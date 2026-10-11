import { test } from "node:test";
import assert from "node:assert/strict";
import { spawn, execFile } from "node:child_process";
import { promisify } from "node:util";
import { createServer } from "node:http";
import { randomUUID } from "node:crypto";
import { mkdir, mkdtemp, writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { join } from "node:path";

const execute = promisify(execFile);
const root = fileURLToPath(new URL("../../", import.meta.url));
const delay = ms => new Promise(resolve => setTimeout(resolve, ms));
async function freePort() {
  const server = createServer(); await new Promise(resolve => server.listen(0, "127.0.0.1", resolve));
  const port = server.address().port; await new Promise(resolve => server.close(resolve)); return port;
}
async function until(check, message, attempts = 150) {
  for (let attempt = 0; attempt < attempts; attempt++) { if (await check()) return; await delay(200); }
  throw new Error(message);
}
class Cdp {
  constructor(socket) {
    this.socket = socket; this.next = 0; this.pending = new Map();
    socket.addEventListener("message", event => {
      const message = JSON.parse(event.data); const pending = this.pending.get(message.id);
      if (!pending) return;
      this.pending.delete(message.id);
      if (message.error) pending.reject(new Error(message.error.message)); else pending.resolve(message.result);
    });
  }
  static async connect(url) {
    const socket = new WebSocket(url);
    await new Promise((resolve, reject) => { socket.addEventListener("open", resolve, { once: true }); socket.addEventListener("error", reject, { once: true }); });
    return new Cdp(socket);
  }
  async send(method, params = {}) {
    const id = ++this.next;
    return new Promise((resolve, reject) => { this.pending.set(id, { resolve, reject }); this.socket.send(JSON.stringify({ id, method, params })); });
  }
  async evaluate(expression) {
    const result = await this.send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true });
    if (result.exceptionDetails) throw new Error(result.exceptionDetails.text);
    return result.result.value;
  }
}

test("pilot journey through real browser, Next, API and disposable MariaDB", { timeout: 300000, skip: process.env.ORGANIZALI_DATABASE_TESTS !== "1" }, async () => {
  const databaseName = `organizali-browser-${randomUUID().slice(0, 8)}`;
  const output = join(root, "TestResults", "browser-m3"); await mkdir(output, { recursive: true });
  const profile = await mkdtemp(join(output, "chrome-"));
  const password = `Synthetic-${randomUUID()}!`;
  const dbPassword = randomUUID();
  const [apiPort, webPort, debugPort] = await Promise.all([freePort(), freePort(), freePort()]);
  const apiUrl = `http://127.0.0.1:${apiPort}`, webUrl = `http://127.0.0.1:${webPort}`;
  const apiDirectory = join(root, "src/PainelGestao.OrganizaliFinancas.Api");
  const apiDll = join(apiDirectory, "bin/Release/net10.0/PainelGestao.OrganizaliFinancas.Api.dll");
  const children = []; const sockets = []; let browser;
  function launch(file, args, cwd, env) {
    const child = spawn(file, args, { cwd, env, windowsHide: true, stdio: "ignore" }); children.push(child); return child;
  }
  try {
    await execute("docker", ["run", "--detach", "--name", databaseName, "-p", "127.0.0.1::3306", "-e", "MARIADB_ROOT_PASSWORD", "-e", "MARIADB_DATABASE=organizali_browser",
      "--health-cmd=healthcheck.sh --connect --innodb_initialized", "--health-interval=1s", "--health-retries=60", "mariadb:11.8"],
      { env: { ...process.env, MARIADB_ROOT_PASSWORD: dbPassword }, windowsHide: true });
    await until(async () => (await execute("docker", ["inspect", "--format={{.State.Health.Status}}", databaseName], { windowsHide: true })).stdout.trim() === "healthy", "MariaDB did not become healthy", 300);
    const databasePort = (await execute("docker", ["port", databaseName, "3306/tcp"], { windowsHide: true })).stdout.trim().split(":").at(-1);
    const environment = { ...process.env, ASPNETCORE_ENVIRONMENT: "Development", ASPNETCORE_URLS: apiUrl,
      Jwt__Key: `Synthetic-signing-${randomUUID()}-${randomUUID()}`,
      OrganizaliFinancas__ConnectionString: `Server=127.0.0.1;Port=${databasePort};Database=organizali_browser;User=root;Password=${dbPassword};Connection Timeout=5`,
      Pilot__Key: randomUUID(), Pilot__HouseholdName: "Casa de demonstração", Pilot__OwnerName: "Titular de teste", Pilot__Email: "owner@pilot.example.test", Pilot__Password: password, Pilot__Actor: "BrowserTest" };
    const admin = operation => execute("dotnet", [apiDll, `--Operation=${operation}`], { cwd: apiDirectory, env: environment, windowsHide: true });
    await admin("Migrate");
    const provisioning = (await admin("ProvisionHousehold")).stdout.split(/\r?\n/).find(line => line.startsWith('{"HouseholdCode"'));
    assert.ok(provisioning, "Provisioning response missing");
    environment.Pilot__HouseholdCode = JSON.parse(provisioning).HouseholdCode;
    environment.Pilot__Status = "Active"; environment.Pilot__Reason = "Teste integrado descartável";
    await admin("ChangeSubscriptionStatus");
    launch("dotnet", [apiDll], apiDirectory, environment);
    launch(process.execPath, [join(root, "frontend/node_modules/next/dist/bin/next"), "start", "--hostname", "127.0.0.1", "--port", String(webPort)], join(root, "frontend"),
      { ...process.env, NODE_ENV: "production", APP_ORIGIN: webUrl, COOKIE_SECURE: "false", INTERNAL_API_URL: apiUrl });
    await until(async () => { try { return (await fetch(`${apiUrl}/health/ready`)).ok && (await fetch(webUrl)).ok; } catch { return false; } }, "Servers unavailable");
    launch("C:/Program Files/Google/Chrome/Application/chrome.exe", ["--headless=new", `--remote-debugging-port=${debugPort}`, "--remote-debugging-address=127.0.0.1", `--user-data-dir=${profile}`,
      "--no-first-run", "--no-default-browser-check", "--disable-extensions", "--disable-background-networking", "--disable-sync", "about:blank"], root, process.env);
    const debugUrl = `http://127.0.0.1:${debugPort}`;
    await until(async () => { try { return (await fetch(`${debugUrl}/json/version`)).ok; } catch { return false; } }, "Headless Chrome unavailable");
    browser = await Cdp.connect((await (await fetch(`${debugUrl}/json/version`)).json()).webSocketDebuggerUrl);
    async function page(url) {
      const context = await browser.send("Target.createBrowserContext");
      const target = await browser.send("Target.createTarget", { url, browserContextId: context.browserContextId });
      const targets = await (await fetch(`${debugUrl}/json/list`)).json();
      const session = await Cdp.connect(targets.find(item => item.id === target.targetId).webSocketDebuggerUrl); sockets.push(session);
      await session.send("Page.enable"); await session.send("Runtime.enable");
      await session.send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
      return session;
    }
    const waitFor = (session, expression) => until(async () => { try { return await session.evaluate(expression); } catch { return false; } }, `Page condition not reached: ${expression}`);
    async function navigate(session, path) { await session.send("Page.navigate", { url: webUrl + path }); await waitFor(session, "document.readyState === 'complete'"); }
    async function input(session, selector, value) {
      await session.evaluate(`(() => { const element=document.querySelector(${JSON.stringify(selector)}); const prototype=element.tagName==='SELECT'?HTMLSelectElement.prototype:HTMLInputElement.prototype; Object.getOwnPropertyDescriptor(prototype,'value').set.call(element,${JSON.stringify(value)}); element.dispatchEvent(new Event('input',{bubbles:true})); element.dispatchEvent(new Event('change',{bubbles:true})); })()`);
    }
    async function login(session, email) {
      await navigate(session, "/login"); await waitFor(session, "!!document.querySelector('input[name=email]')");
      await input(session, "input[name=email]", email); await input(session, "input[name=password]", password);
      await session.evaluate("document.querySelector('form').requestSubmit()"); await waitFor(session, "location.pathname === '/household' && document.body.innerText.includes('Seu acesso')");
    }
    async function accept(session, url, email) {
      await session.send("Page.navigate", { url }); await waitFor(session, "!!document.querySelector('input[name=confirmation]') && location.hash === ''");
      await input(session, "input[name=password]", password); await input(session, "input[name=confirmation]", password);
      await session.evaluate("document.querySelector('form').requestSubmit()"); await waitFor(session, "document.body.innerText.includes('Acesso criado')"); await login(session, email);
    }
    const press = (session, text) => session.evaluate(`[...document.querySelectorAll('button')].find(b => b.textContent.trim() === ${JSON.stringify(text)}).click()`);
    const closedModal = "!document.querySelector('[role=dialog]')";
    async function screenshot(session, name) {
      const shot = await session.send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
      await writeFile(join(output, name), Buffer.from(shot.data, "base64"));
    }
    const owner = await page(webUrl + "/login"); await login(owner, "owner@pilot.example.test");
    assert.ok(!(await owner.evaluate("document.cookie")).includes("organizali_"), "Session cookies must be HttpOnly");
    await navigate(owner, "/people"); await waitFor(owner, "[...document.querySelectorAll('button')].some(b => b.textContent.trim() === 'Novo morador')");
    await press(owner, "Novo morador"); await waitFor(owner, "!!document.querySelector('[role=dialog] input[name=name]')");
    await input(owner, "input[name=name]", "Morador sem login"); await owner.evaluate("document.querySelector('form').requestSubmit()");
    await waitFor(owner, `${closedModal} && document.querySelectorAll('.member-row-item').length === 1`); await screenshot(owner, "moradores-desktop.png");
    await owner.send("Emulation.setDeviceMetricsOverride", { width: 360, height: 800, deviceScaleFactor: 1, mobile: false });
    assert.ok(await owner.evaluate("document.documentElement.scrollWidth <= innerWidth"), "Mobile viewport overflow");
    await screenshot(owner, "moradores-mobile.png");
    await owner.send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
    await navigate(owner, "/members"); await waitFor(owner, "!!document.querySelector('.member-row-item')");
    await press(owner, "Convidar pessoa"); await waitFor(owner, "!!document.querySelector('input[name=email]')");
    await input(owner, "input[name=name]", "Editor de teste"); await input(owner, "input[name=email]", "editor@pilot.example.test"); await input(owner, "select[name=roleKey]", "editor");
    await owner.evaluate("document.querySelector('form').requestSubmit()"); await waitFor(owner, "!!document.querySelector('textarea')?.value.includes('/join#token=')");
    const editorLink = await owner.evaluate("document.querySelector('textarea').value");
    const editor = await page("about:blank"); await accept(editor, editorLink, "editor@pilot.example.test");
    await navigate(editor, "/catalogs/categories"); await waitFor(editor, "[...document.querySelectorAll('button')].some(b => b.textContent.trim() === 'Nova categoria')");
    await press(editor, "Nova categoria"); await waitFor(editor, "!!document.querySelector('input[name=name]')");
    await input(editor, "input[name=name]", "Moradia"); await editor.evaluate("document.querySelector('form').requestSubmit()");
    await waitFor(editor, `${closedModal} && document.querySelector('main').innerText.includes('Moradia')`); await screenshot(editor, "categorias-desktop.png");
    await navigate(editor, "/expense-types"); await waitFor(editor, "[...document.querySelectorAll('button')].some(b => b.textContent.trim() === 'Novo tipo')");
    await press(editor, "Novo tipo"); await waitFor(editor, "!!document.querySelector('input[name=name]')");
    await input(editor, "input[name=name]", "Água"); await input(editor, "select[name=behavior]", "recurring");
    await editor.evaluate("[...document.querySelectorAll('button')].find(b=>b.textContent==='Adicionar campo').click()"); await waitFor(editor, "!!document.querySelector('.field-definition input')");
    await input(editor, ".field-definition input", "Instalação"); await editor.evaluate("document.querySelector('.field-definition input[type=checkbox]').click()");
    await editor.evaluate("document.querySelector('form').requestSubmit()"); await waitFor(editor, `${closedModal} && document.querySelectorAll('.type-item').length === 1`);
    await editor.evaluate("document.querySelector('[aria-label^=\"Experimentar campos\"]').click()"); await waitFor(editor, "!!document.querySelector('[role=dialog] form input')");
    assert.equal(await editor.evaluate("document.querySelector('form input').checkValidity()"), false);
    await input(editor, "form input", "12345"); await editor.evaluate("document.querySelector('form').requestSubmit()"); await waitFor(editor, "document.body.innerText.includes('Campos válidos')");
    await screenshot(editor, "tipos-desktop.png");
    const readerResult = await owner.evaluate(`fetch('/api/backend/members/invitations',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({name:'Leitor de teste',email:'reader@pilot.example.test',roleKey:'reader'})}).then(async r=>({status:r.status,body:await r.json()}))`);
    assert.equal(readerResult.status, 201);
    const reader = await page("about:blank"); await accept(reader, `${webUrl}/join#token=${encodeURIComponent(readerResult.body.data.token)}`, "reader@pilot.example.test");
    await navigate(reader, "/members"); await waitFor(reader, "location.pathname === '/no-permission'");
    await navigate(reader, "/catalogs/categories"); await waitFor(reader, "document.body.innerText.includes('Moradia') || !!document.querySelector('input[value=Moradia]')");
    assert.equal(await reader.evaluate("[...document.querySelectorAll('button')].some(b => b.textContent.trim() === 'Nova categoria' || (b.getAttribute('aria-label') ?? '').startsWith('Editar'))"), false);
    assert.equal(await reader.evaluate("fetch('/api/backend/catalogs/categories',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({name:'Denied',isActive:true,version:0,parentCode:null})}).then(r=>r.status)"), 403);
    // M4: contas, transferência, fonte fixa, recebimentos parciais e estorno pelo titular.
    const submit = (session, selector) => session.evaluate(`document.querySelector(${JSON.stringify(selector)}).form.requestSubmit()`);
    await navigate(editor, "/accounts"); await waitFor(editor, "!!document.querySelector('.money-summary')");
    for (const [name, balance] of [["Conta corrente", "1.000,00"], ["Poupança", "0,00"]]) {
      const rows = await editor.evaluate("document.querySelectorAll('.account-item').length");
      await press(editor, "Nova conta"); await waitFor(editor, "!!document.querySelector('input[name=openingBalance]')");
      await input(editor, "input[name=name]", name); await input(editor, "input[name=openingBalance]", balance); if (rows === 0) await screenshot(editor, "modal-nova-conta.png"); await submit(editor, "input[name=openingBalance]");
      await waitFor(editor, `${closedModal} && document.querySelectorAll('.account-item').length === ${rows + 1}`);
    }
    await waitFor(editor, "[...document.querySelectorAll('button')].some(b => b.textContent.trim() === 'Transferir')");
    await press(editor, "Transferir"); await waitFor(editor, "!!document.querySelector('select[name=source]')");
    await input(editor, "input[name=amount]", "500,00"); await submit(editor, "input[name=amount]");
    await waitFor(editor, "document.body.innerText.includes('Transferência registrada')");
    assert.ok((await editor.evaluate("document.querySelector('.money-summary strong').textContent")).includes("1.000,00"), "Transfer must keep the household total");
    await navigate(editor, "/income"); await waitFor(editor, "[...document.querySelectorAll('button')].some(b => b.textContent.trim() === 'Nova fonte de renda')");
    await press(editor, "Nova fonte de renda"); await waitFor(editor, "!!document.querySelector('input[name=expected]')");
    await input(editor, "input[name=name]", "Salário"); await input(editor, "input[name=expected]", "4.000,00"); await submit(editor, "input[name=expected]");
    await waitFor(editor, "document.querySelectorAll('.forecast-list li').length === 3");
    for (const amount of ["2.000,00", "1.800,00"]) {
      await waitFor(editor, closedModal); await press(editor, "Registrar recebimento"); await waitFor(editor, "!!document.querySelector('select[name=receiptSource]')");
      const source = await editor.evaluate("[...document.querySelector('select[name=receiptSource]').options].find(o => o.value).value");
      await input(editor, "select[name=receiptSource]", source); await waitFor(editor, "!!document.querySelector('select[name=receiptForecast]')?.value");
      await input(editor, "input[name=amount]", amount); await submit(editor, "input[name=amount]");
      await waitFor(editor, `document.body.innerText.includes('Recebimento registrado') && document.querySelector('.forecast-list li').innerText.includes(${JSON.stringify(amount === "2.000,00" ? "2.000,00" : "3.800,00")})`);
    }
    assert.ok((await editor.evaluate("document.querySelector('.forecast-list li').innerText")).includes("Falta receber R$ 200,00"), "Residual of 200 expected");
    assert.equal(await editor.evaluate("[...document.querySelectorAll('button')].some(b => b.textContent === 'Estornar')"), false, "Editor cannot reverse");
    await navigate(owner, "/income"); await waitFor(owner, "[...document.querySelectorAll('button')].some(b => b.textContent === 'Estornar')");
    await owner.evaluate("[...document.querySelectorAll('button')].find(b => b.textContent === 'Estornar').click()"); await waitFor(owner, "!!document.querySelector('input[name=reason]')");
    await input(owner, "input[name=reason]", "Depósito devolvido"); await submit(owner, "input[name=reason]");
    await waitFor(owner, "document.body.innerText.includes('Estornado: Depósito devolvido') && document.querySelector('.forecast-list li').innerText.includes('Falta receber R$ 2.000,00')");
    await screenshot(owner, "rendas-desktop.png");
    await owner.send("Emulation.setDeviceMetricsOverride", { width: 360, height: 800, deviceScaleFactor: 1, mobile: false });
    for (const [path, shot] of [["/income", "rendas-mobile.png"], ["/accounts", "contas-mobile.png"]]) {
      await navigate(owner, path); await waitFor(owner, "!!document.querySelector('.money-summary')");
      assert.ok(await owner.evaluate("document.documentElement.scrollWidth <= innerWidth"), `Mobile viewport overflow on ${path}`);
      await screenshot(owner, shot);
    }
    await owner.send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
    await navigate(owner, "/accounts"); await waitFor(owner, "!!document.querySelector('.account-item')");
    await owner.evaluate("[...document.querySelectorAll('button')].find(b => b.textContent === 'Ver extrato').click()"); await waitFor(owner, "!!document.querySelector('.statement-totals')");
    await screenshot(owner, "contas-desktop.png");
    // M5: conta avulsa, pagamento parcial, prévia de parcelas, estorno pelo titular e largura mobile.
    await navigate(editor, "/expenses"); await waitFor(editor, "[...document.querySelectorAll('button')].some(b => b.textContent.trim() === 'Nova despesa')");
    await press(editor, "Nova despesa"); await waitFor(editor, "!!document.querySelector('input[name=description]')");
    await input(editor, "input[name=description]", "Conta de luz"); await input(editor, "input[name=amount]", "250,00"); await submit(editor, "input[name=amount]");
    await waitFor(editor, "[...document.querySelectorAll('.expense-row strong')].some(e => e.textContent === 'Conta de luz')");
    const luz = "[...document.querySelectorAll('.expense-row')].find(r => r.innerText.includes('Conta de luz'))";
    await editor.evaluate(`${luz}.querySelector('button').click()`); await waitFor(editor, "!!document.querySelector('.pay-form input[name=amount]')");
    await input(editor, ".pay-form input[name=amount]", "100,00"); await submit(editor, ".pay-form input[name=amount]");
    await waitFor(editor, `${luz}?.innerText.includes('Paga em parte') && ${luz}.innerText.includes('150,00')`);
    await waitFor(editor, closedModal); await press(editor, "Nova despesa"); await waitFor(editor, "!!document.querySelector('.segmented')");
    await editor.evaluate("[...document.querySelectorAll('.segmented button')].find(b => b.textContent === 'Compra parcelada').click()"); await waitFor(editor, "!!document.querySelector('input[name=total]')");
    await input(editor, "input[name=description]", "Cadeira"); await input(editor, "input[name=total]", "100,00");
    await editor.evaluate("[...document.querySelectorAll('button')].find(b => b.textContent === 'Ver parcelas').click()");
    await waitFor(editor, "document.querySelector('.data-form caption')?.textContent.includes('3 parcelas') && document.querySelector('.data-form table').innerText.includes('33,34')");
    await submit(editor, "input[name=total]"); await waitFor(editor, "document.body.innerText.includes('Compra parcelada lançada')");
    await waitFor(editor, "[...document.querySelectorAll('.expense-row')].some(r => r.innerText.includes('Parcela 1/3'))");
    assert.equal(await editor.evaluate("[...document.querySelectorAll('button')].some(b => b.textContent === 'Estornar')"), false, "Editor cannot reverse payments");
    await navigate(owner, "/expenses"); await waitFor(owner, "[...document.querySelectorAll('button')].some(b => b.textContent === 'Estornar')");
    await owner.evaluate("[...document.querySelectorAll('button')].find(b => b.textContent === 'Estornar').click()"); await waitFor(owner, "!!document.querySelector('input[name=reason]')");
    await input(owner, "input[name=reason]", "Débito em duplicidade"); await submit(owner, "input[name=reason]");
    await waitFor(owner, `document.body.innerText.includes('Estornado: Débito em duplicidade') && ${luz}.innerText.includes('Em aberto')`);
    await screenshot(owner, "despesas-desktop.png");
    await owner.send("Emulation.setDeviceMetricsOverride", { width: 360, height: 800, deviceScaleFactor: 1, mobile: false });
    await navigate(owner, "/expenses"); await waitFor(owner, "!!document.querySelector('.money-grid')");
    assert.ok(await owner.evaluate("document.documentElement.scrollWidth <= innerWidth"), "Mobile viewport overflow on /expenses");
    await screenshot(owner, "despesas-mobile.png");
    await owner.send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
    // M6: cartão sem número, compra de 300 sem mexer na conta e pagamento da fatura que reduz o caixa uma única vez.
    const householdTotal = async session => { await navigate(session, "/accounts"); await waitFor(session, "!!document.querySelector('.money-summary strong')");
      return Number((await session.evaluate("document.querySelector('.money-summary strong').textContent")).replace(/[^\d,-]/g, "").replace(",", ".")); };
    const before = await householdTotal(editor);
    await navigate(editor, "/cards"); await waitFor(editor, "[...document.querySelectorAll('button')].some(b => b.textContent.trim() === 'Novo cartão')");
    await press(editor, "Novo cartão"); await waitFor(editor, "!!document.querySelector('input[name=closingDay]')");
    await input(editor, "input[name=name]", "Visa da casa"); await input(editor, "input[name=creditLimit]", "1.000,00"); await submit(editor, "input[name=closingDay]");
    await waitFor(editor, `${closedModal} && document.body.innerText.includes('Cartão cadastrado')`);
    await press(editor, "Lançar compra"); await waitFor(editor, "!!document.querySelector('input[name=total]')");
    await input(editor, "input[name=description]", "Mercado no cartão"); await input(editor, "input[name=total]", "300,00");
    await editor.evaluate("[...document.querySelectorAll('button')].find(b => b.textContent === 'Ver em quais faturas cai').click()");
    await waitFor(editor, "document.querySelector('.data-form caption')?.textContent.includes('1 parcela(s) somando R$')");
    await submit(editor, "input[name=total]"); await waitFor(editor, "document.body.innerText.includes('Compra lançada') && [...document.querySelectorAll('.expense-row')].some(r => r.innerText.includes('300,00'))");
    assert.equal(await householdTotal(editor), before, "A card purchase must not change the household balance");
    await navigate(editor, "/cards"); await waitFor(editor, "[...document.querySelectorAll('button')].some(b => b.textContent === 'Pagar fatura')");
    await editor.evaluate("[...document.querySelectorAll('button')].find(b => b.textContent === 'Pagar fatura').click()"); await waitFor(editor, "!!document.querySelector('.pay-form input[name=amount]')");
    await submit(editor, ".pay-form input[name=amount]"); await waitFor(editor, "[...document.querySelectorAll('.status-tag')].some(t => t.textContent === 'Paga')");
    await screenshot(editor, "cartoes-desktop.png");
    assert.equal(await householdTotal(editor), before - 300, "Paying the invoice reduces cash once");
    await editor.send("Emulation.setDeviceMetricsOverride", { width: 360, height: 800, deviceScaleFactor: 1, mobile: false });
    await navigate(editor, "/cards"); await waitFor(editor, "!!document.querySelector('.money-grid')");
    assert.ok(await editor.evaluate("document.documentElement.scrollWidth <= innerWidth"), "Mobile viewport overflow on /cards");
    await screenshot(editor, "cartoes-mobile.png");
    await editor.send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
    // M7: orçamento salvo pelo editor, painel com projeção, fluxo, alertas e análise; leitor vê sem editar.
    await navigate(editor, "/budget"); await waitFor(editor, "!!document.querySelector('.limit-input input')");
    await input(editor, ".limit-input input", "100,00"); await editor.evaluate("document.querySelector('.limit-input input').form.requestSubmit()");
    await waitFor(editor, "document.body.innerText.includes('Orçamento salvo') && document.body.innerText.includes('Dentro do limite')");
    await navigate(reader, "/budget"); await waitFor(reader, "!!document.querySelector('.budget-list')");
    assert.equal(await reader.evaluate("!!document.querySelector('.limit-input input')"), false, "Reader cannot edit the budget");
    await navigate(owner, "/dashboard"); await waitFor(owner, "!!document.querySelector('.projection-chart svg path.balance-line')");
    assert.ok((await owner.evaluate("document.querySelector('.money-grid').textContent")).includes("Saldo hoje"), "Dashboard KPIs expected");
    await screenshot(owner, "painel-desktop.png");
    for (const [path, ready] of [["/cash-flow", ".month-bars"], ["/alerts", "h1"], ["/analysis", ".filter-form"]]) {
      await navigate(owner, path); await waitFor(owner, `!!document.querySelector(${JSON.stringify(ready)})`);
    }
    await navigate(owner, "/cash-flow"); await waitFor(owner, "!!document.querySelector('.month-bars')"); await screenshot(owner, "fluxo-desktop.png");
    await owner.send("Emulation.setDeviceMetricsOverride", { width: 360, height: 800, deviceScaleFactor: 1, mobile: false });
    for (const [path, shot] of [["/dashboard", "painel-mobile.png"], ["/cash-flow", "fluxo-mobile.png"], ["/budget", null], ["/alerts", null], ["/analysis", null]]) {
      await navigate(owner, path); await waitFor(owner, "!!document.querySelector('h1')");
      assert.ok(await owner.evaluate("document.documentElement.scrollWidth <= innerWidth"), `Mobile viewport overflow on ${path}`);
      if (shot) await screenshot(owner, shot);
    }
    await owner.send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
    const swagger = await page(`${apiUrl}/swagger/index.html`);
    await waitFor(swagger, "!!document.querySelector('.swagger-ui .opblock')");
    await writeFile(join(output, "swagger-dom.html"), await swagger.evaluate("document.documentElement.outerHTML"));
    await waitFor(swagger, "!!document.querySelector('.swagger-ui .opblock') && !!document.querySelector('.global-docs-hero')");
    await screenshot(swagger, "swagger-desktop.png");
    await owner.evaluate("document.querySelector('aside button[aria-label=Sair]').click()"); await waitFor(owner, "!!document.querySelector('[role=dialog]')");
    await owner.evaluate("[...document.querySelectorAll('button')].find(b=>b.textContent.includes('Sair de todas')).click()");
    await waitFor(owner, "location.pathname === '/login'");
    await navigate(owner, "/people"); await waitFor(owner, "location.pathname === '/login'");
    await writeFile(join(output, "result.json"), JSON.stringify({ passed: true, household: "synthetic", browser: "Chrome headless isolated profile", database: "disposable MariaDB", checks: ["login", "HttpOnly", "resident without login", "mobile width", "manual invitation", "editor catalogs", "required custom field", "reader route and API denial", "accounts and transfer total", "income forecast 4000 with 2000+1800 receipts", "owner reversal", "mobile accounts/income", "expense partial payment", "installments 100 in 3", "owner payment reversal", "mobile expenses", "card purchase keeps balance", "invoice payment once", "mobile cards", "budget by editor", "reader budget read-only", "dashboard projection", "cash flow, alerts and analysis", "mobile planning screens", "Swagger", "logout and private route denial"], screenshots: ["moradores-mobile.png", "tipos-desktop.png", "rendas-desktop.png", "rendas-mobile.png", "contas-mobile.png", "contas-desktop.png", "despesas-desktop.png", "despesas-mobile.png", "cartoes-desktop.png", "cartoes-mobile.png", "painel-desktop.png", "painel-mobile.png", "fluxo-desktop.png", "fluxo-mobile.png", "swagger-desktop.png"] }, null, 2));
  } finally {
    for (const socket of sockets) socket.socket.close();
    if (browser) { await browser.send("Browser.close").catch(() => {}); browser.socket.close(); }
    for (const child of children.reverse()) if (child.exitCode === null) child.kill();
    await execute("docker", ["rm", "--force", "--volumes", databaseName], { windowsHide: true }).catch(() => {});
  }
});
