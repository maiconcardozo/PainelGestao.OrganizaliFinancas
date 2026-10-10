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

test("pilot journey through real browser, Next, API and disposable MariaDB", { timeout: 180000, skip: process.env.ORGANIZALI_DATABASE_TESTS !== "1" }, async () => {
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
    async function screenshot(session, name) {
      const shot = await session.send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
      await writeFile(join(output, name), Buffer.from(shot.data, "base64"));
    }
    const owner = await page(webUrl + "/login"); await login(owner, "owner@pilot.example.test");
    assert.ok(!(await owner.evaluate("document.cookie")).includes("organizali_"), "Session cookies must be HttpOnly");
    await navigate(owner, "/people"); await waitFor(owner, "!!document.querySelector('input[name=name]')");
    await input(owner, "input[name=name]", "Morador sem login"); await owner.evaluate("document.querySelector('form').requestSubmit()");
    await waitFor(owner, "document.querySelectorAll('.member-row').length === 1");
    await owner.send("Emulation.setDeviceMetricsOverride", { width: 360, height: 800, deviceScaleFactor: 1, mobile: false });
    assert.ok(await owner.evaluate("document.documentElement.scrollWidth <= innerWidth"), "Mobile viewport overflow");
    await screenshot(owner, "moradores-mobile.png");
    await owner.send("Emulation.setDeviceMetricsOverride", { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
    await navigate(owner, "/members"); await waitFor(owner, "!!document.querySelector('input[name=email]')");
    await input(owner, "input[name=name]", "Editor de teste"); await input(owner, "input[name=email]", "editor@pilot.example.test"); await input(owner, "select[name=roleKey]", "editor");
    await owner.evaluate("document.querySelector('form').requestSubmit()"); await waitFor(owner, "!!document.querySelector('textarea')?.value.includes('/join#token=')");
    const editorLink = await owner.evaluate("document.querySelector('textarea').value");
    const editor = await page("about:blank"); await accept(editor, editorLink, "editor@pilot.example.test");
    await navigate(editor, "/catalogs/categories"); await waitFor(editor, "!!document.querySelector('input[name=name]')");
    await input(editor, "input[name=name]", "Moradia"); await editor.evaluate("document.querySelector('form').requestSubmit()"); await waitFor(editor, "document.querySelectorAll('input[name=name]').length === 2");
    await navigate(editor, "/expense-types"); await waitFor(editor, "!!document.querySelector('input[name=name]')");
    await input(editor, "input[name=name]", "Água"); await input(editor, "select[name=behavior]", "recurring");
    await editor.evaluate("[...document.querySelectorAll('button')].find(b=>b.textContent==='Adicionar campo').click()"); await waitFor(editor, "!!document.querySelector('.field-definition input')");
    await input(editor, ".field-definition input", "Instalação"); await editor.evaluate("document.querySelector('.field-definition input[type=checkbox]').click()");
    await editor.evaluate("document.querySelector('form').requestSubmit()"); await waitFor(editor, "document.querySelectorAll('.type-list article').length === 1");
    await editor.evaluate("[...document.querySelectorAll('button')].find(b=>b.textContent==='Experimentar campos').click()"); await waitFor(editor, "document.querySelectorAll('form').length === 2");
    assert.equal(await editor.evaluate("document.querySelector('form input').checkValidity()"), false);
    await input(editor, "form input", "12345"); await editor.evaluate("document.querySelector('form').requestSubmit()"); await waitFor(editor, "document.body.innerText.includes('Campos válidos')");
    await screenshot(editor, "tipos-desktop.png");
    const readerResult = await owner.evaluate(`fetch('/api/backend/members/invitations',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({name:'Leitor de teste',email:'reader@pilot.example.test',roleKey:'reader'})}).then(async r=>({status:r.status,body:await r.json()}))`);
    assert.equal(readerResult.status, 201);
    const reader = await page("about:blank"); await accept(reader, `${webUrl}/join#token=${encodeURIComponent(readerResult.body.data.token)}`, "reader@pilot.example.test");
    await navigate(reader, "/members"); await waitFor(reader, "location.pathname === '/no-permission'");
    await navigate(reader, "/catalogs/categories"); await waitFor(reader, "document.body.innerText.includes('Moradia') || !!document.querySelector('input[value=Moradia]')");
    assert.equal(await reader.evaluate("[...document.querySelectorAll('button')].some(b=>b.textContent==='Salvar cadastro')"), false);
    assert.equal(await reader.evaluate("fetch('/api/backend/catalogs/categories',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({name:'Denied',isActive:true,version:0,parentCode:null})}).then(r=>r.status)"), 403);
    const swagger = await page(`${apiUrl}/swagger/index.html`);
    await waitFor(swagger, "!!document.querySelector('.swagger-ui .opblock')");
    await writeFile(join(output, "swagger-dom.html"), await swagger.evaluate("document.documentElement.outerHTML"));
    await waitFor(swagger, "!!document.querySelector('.swagger-ui .opblock') && !!document.querySelector('.global-docs-hero')");
    await screenshot(swagger, "swagger-desktop.png");
    await owner.evaluate("[...document.querySelectorAll('button')].find(b=>b.textContent.includes('Sair de todas')).click()");
    await waitFor(owner, "location.pathname === '/login'");
    await navigate(owner, "/people"); await waitFor(owner, "location.pathname === '/login'");
    await writeFile(join(output, "result.json"), JSON.stringify({ passed: true, household: "synthetic", browser: "Chrome headless isolated profile", database: "disposable MariaDB", checks: ["login", "HttpOnly", "resident without login", "mobile width", "manual invitation", "editor catalogs", "required custom field", "reader route and API denial", "Swagger", "logout and private route denial"], screenshots: ["moradores-mobile.png", "tipos-desktop.png", "swagger-desktop.png"] }, null, 2));
  } finally {
    for (const socket of sockets) socket.socket.close();
    if (browser) { await browser.send("Browser.close").catch(() => {}); browser.socket.close(); }
    for (const child of children.reverse()) if (child.exitCode === null) child.kill();
    await execute("docker", ["rm", "--force", "--volumes", databaseName], { windowsHide: true }).catch(() => {});
  }
});
