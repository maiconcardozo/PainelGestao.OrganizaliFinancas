import { test } from "node:test";
import assert from "node:assert/strict";
import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { createServer } from "node:http";
import { randomBytes, randomUUID } from "node:crypto";
import { mkdtemp, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { fileURLToPath } from "node:url";
import { join } from "node:path";

// The disposable stack uses Caddy's internal CA on localhost; certificate checks are relaxed only inside this test process.
process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
const execute = promisify(execFile);
const root = fileURLToPath(new URL("../../", import.meta.url));
const deploy = join(root, "deploy");
const delay = ms => new Promise(resolve => setTimeout(resolve, ms));
const secret = () => randomBytes(24).toString("base64url");
async function freePort() {
  const server = createServer(); await new Promise(resolve => server.listen(0, "127.0.0.1", resolve));
  const port = server.address().port; await new Promise(resolve => server.close(resolve)); return port;
}

test("production stack: images, HTTPS proxy, isolation, provisioning, encrypted backup and restore", { timeout: 1500000, skip: process.env.ORGANIZALI_DOCKER_TESTS !== "1" }, async () => {
  const project = `organizali-verify-${randomUUID().slice(0, 8)}`;
  // Forward slashes keep the paths valid both for docker and for the bash scripts.
  const work = (await mkdtemp(join(tmpdir(), "organizali-deploy-"))).replaceAll("\\", "/");
  const [httpPort, httpsPort] = [await freePort(), await freePort()];
  const origin = `https://localhost:${httpsPort}`;
  const values = {
    ORGANIZALI_VERSION: process.env.ORGANIZALI_VERSION ?? "verify", ORGANIZALI_PUBLIC_ORIGIN: origin, ORGANIZALI_SITE_ADDRESS: "localhost", ORGANIZALI_TLS: "internal",
    ORGANIZALI_HTTP_PORT: String(httpPort), ORGANIZALI_HTTPS_PORT: String(httpsPort), ORGANIZALI_DB_PASSWORD: secret(), ORGANIZALI_DB_ROOT_PASSWORD: secret(),
    ORGANIZALI_JWT_KEY: `${secret()}${secret()}`, ORGANIZALI_BACKUP_PASSPHRASE: secret(),
  };
  const envFile = `${work}/.env.production`;
  await writeFile(envFile, Object.entries(values).map(([key, value]) => `${key}=${value}`).join("\n") + "\n");
  const env = { ...process.env, COMPOSE_PROJECT_NAME: project, ENV_FILE: envFile, BACKUP_DIR: `${work}/backups` };
  const compose = (...args) => execute("docker", ["compose", "-f", join(deploy, "compose.production.yml"), "--env-file", envFile, ...args],
    { cwd: deploy, env, windowsHide: true, maxBuffer: 64 * 1024 * 1024 });
  const until = async (check, message, attempts = 180) => {
    for (let attempt = 0; attempt < attempts; attempt++) { try { if (await check()) return; } catch {} await delay(1000); }
    throw new Error(message);
  };
  const password = `Synthetic-${randomUUID()}!`;
  const admin = async (operation, extra = {}) => {
    const flags = Object.entries({ Pilot__Actor: "DeployTest", ...extra }).flatMap(([key, value]) => ["-e", `${key}=${value}`]);
    return (await compose("run", "--rm", "--no-deps", ...flags, "migrate", `--Operation=${operation}`)).stdout;
  };
  const request = (path, options = {}) => fetch(origin + path, { redirect: "manual", ...options,
    headers: { Origin: origin, "Content-Type": "application/json", ...(options.headers ?? {}) } });
  async function login() {
    const response = await request("/api/auth/login", { method: "POST", body: JSON.stringify({ email: "owner@deploy.example.test", password }) });
    assert.equal(response.status, 200, await response.text());
    return response.headers.getSetCookie().map(cookie => cookie.split(";")[0]).join("; ");
  }
  try {
    if (!process.env.ORGANIZALI_VERSION) await compose("build");
    await compose("up", "-d");
    await until(async () => (await fetch(`${origin}/api/health`)).ok, "Stack unavailable over HTTPS");

    // Only the proxy publishes ports; the API, Swagger and the database stay private.
    const services = (await compose("ps", "--format", "json")).stdout.trim().split(/\r?\n/).map(line => JSON.parse(line));
    for (const name of ["api", "database", "frontend"])
      assert.ok(!(services.find(service => service.Service === name)?.Publishers ?? []).some(port => port.PublishedPort), `${name} must not publish ports`);
    const page = await fetch(`${origin}/login`);
    assert.equal(page.status, 200);
    assert.match(page.headers.get("strict-transport-security") ?? "", /max-age=31536000/);
    assert.match(page.headers.get("content-security-policy") ?? "", /frame-ancestors 'none'/);
    assert.equal(page.headers.get("server"), null);
    assert.equal((await fetch(`${origin}/swagger/index.html`)).status, 404);
    assert.equal((await fetch(`http://localhost:${httpPort}/login`, { redirect: "manual" })).status, 308, "HTTP redirects to HTTPS");

    const provisioning = (await admin("ProvisionHousehold", { Pilot__Key: randomUUID(), Pilot__HouseholdName: "Casa da verificação", Pilot__OwnerName: "Titular",
      Pilot__Email: "owner@deploy.example.test", Pilot__Password: password })).split(/\r?\n/).find(line => line.startsWith('{"HouseholdCode"'));
    assert.ok(provisioning, "Provisioning output missing");
    await admin("ChangeSubscriptionStatus", { Pilot__HouseholdCode: JSON.parse(provisioning).HouseholdCode, Pilot__Status: "Active", Pilot__Reason: "Verificação descartável" });
    let cookie = await login();
    const today = new Date().toISOString().slice(0, 10);
    const create = name => request("/api/backend/accounts", { method: "POST", headers: { Cookie: cookie },
      body: JSON.stringify({ name, isActive: true, version: 0, openingDate: today, openingBalance: "1234.56" }) });
    assert.equal((await create("Conta restaurada")).status, 201);
    assert.equal((await fetch(`${origin}/api/backend/accounts`)).status, 401, "no session, no data");
    const roll = JSON.parse((await admin("RollHorizon")).split(/\r?\n/).find(line => line.startsWith('{"Skipped"')));
    assert.deepEqual([roll.Skipped, roll.Households], [false, 1]);

    // Encrypted backup, a change after it, then restore: the change disappears and the balance comes back exactly.
    const backup = (await execute("bash", [join(deploy, "backup.sh")], { env, windowsHide: true })).stdout.trim().split(/\r?\n/).at(-1);
    assert.match(backup, /\.sql\.gz\.enc$/);
    assert.equal((await create("Depois do backup")).status, 201);
    await execute("bash", [join(deploy, "restore.sh"), "--yes", backup], { env, windowsHide: true, maxBuffer: 64 * 1024 * 1024 });
    await until(async () => (await fetch(`${origin}/api/health`)).ok, "Stack unavailable after restore");
    cookie = await login();
    const accounts = (await (await request("/api/backend/accounts", { headers: { Cookie: cookie } })).json()).data;
    assert.deepEqual(accounts.accounts.map(account => [account.name, account.balance]), [["Conta restaurada", "1234.56"]]);

    const logs = (await compose("logs", "api", "--no-color")).stdout;
    assert.ok(logs.includes('"Organizali.Http"'), "API logs are structured JSON");
    for (const sensitive of [values.ORGANIZALI_DB_PASSWORD, values.ORGANIZALI_JWT_KEY, password, "1234.56"])
      assert.ok(!logs.includes(sensitive), "logs never carry secrets or financial values");
  } finally {
    await compose("down", "--volumes", "--remove-orphans").catch(() => {});
    await rm(work, { recursive: true, force: true });
  }
});
