import { test } from "node:test";
import assert from "node:assert/strict";
import { createServer } from "node:http";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";
import { join } from "node:path";

test("cookies, origin protection, token isolation and expired-access logout", { timeout: 40000 }, async () => {
  const calls = [];
  const access = "synthetic-access-token";
  const refresh = "synthetic-refresh-token";
  const backend = createServer((request, response) => {
    calls.push({ path: request.url, authorization: request.headers.authorization });
    request.resume();
    if (request.url === "/api/auth/logout") { response.writeHead(204); response.end(); return; }
    response.setHeader("Content-Type", "application/json");
    if (request.url === "/api/auth/me") {
      if (request.headers.authorization !== `Bearer ${access}`) { response.writeHead(401); response.end("{}"); return; }
      response.end(JSON.stringify({ data: { userCode: "00000000-0000-0000-0000-000000000001", name: "Morador de teste", email: "test@example.test",
        roles: ["reader"], permissions: ["household.read"], menu: [{ key: "household", name: "Minha casa", route: "/household", icon: "House" }, { key: "my-account", name: "Minha conta", route: "/my-account", icon: "UserRound" }] } }));
      return;
    }
    response.end(JSON.stringify({ data: { tokens: {
      accessToken: access, refreshToken: refresh,
      accessTokenExpiresAt: new Date(Date.now() + 900000).toISOString(),
      refreshTokenExpiresAt: new Date(Date.now() + 86400000).toISOString(),
    } } }));
  });
  await new Promise(resolve => backend.listen(0, "127.0.0.1", resolve));
  const portProbe = createServer();
  await new Promise(resolve => portProbe.listen(0, "127.0.0.1", resolve));
  const port = portProbe.address().port;
  await new Promise(resolve => portProbe.close(resolve));
  const origin = `http://127.0.0.1:${port}`;
  const directory = fileURLToPath(new URL("../../frontend/", import.meta.url));
  const web = spawn(process.execPath, [
    join(directory, "node_modules/next/dist/bin/next"), "start", "--hostname", "127.0.0.1", "--port", String(port),
  ], { cwd: directory, windowsHide: true, env: {
    ...process.env, NODE_ENV: "production", COOKIE_SECURE: "true", APP_ORIGIN: origin,
    INTERNAL_API_URL: `http://127.0.0.1:${backend.address().port}`,
  }, stdio: ["ignore", "pipe", "pipe"] });
  let output = "";
  web.stdout.on("data", data => { output += data; });
  web.stderr.on("data", data => { output += data; });
  try {
    let ready = false;
    for (let attempt = 0; attempt < 100; attempt++) {
      try { ready = (await fetch(origin)).ok; } catch {}
      if (ready) break;
      if (web.exitCode !== null) throw new Error(output);
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    assert.ok(ready, output);
    const post = (path, options = {}) => fetch(origin + path, { method: "POST",
      headers: { Origin: origin, "Content-Type": "application/json", ...options.headers },
      body: JSON.stringify(options.body ?? {}), redirect: "manual" });
    const denied = await post("/api/auth/login", { headers: { Origin: "https://foreign.example" } });
    assert.equal(denied.status, 403);
    assert.equal(calls.length, 0);
    for (const path of ["/household", "/my-account"]) {
      const anonymous = await fetch(origin + path, { redirect: "manual" });
      assert.equal(anonymous.status, 307);
      assert.equal(anonymous.headers.get("location"), "/login");
    }
    const expired = await fetch(origin + "/household", { redirect: "manual", headers: { Cookie: `organizali_refresh=${refresh}` } });
    assert.equal(expired.headers.get("location"), "/session-renew");
    const login = await post("/api/auth/login", { body: { email: "test@example.test", password: "synthetic" } });
    assert.equal(login.status, 200);
    assert.deepEqual(await login.json(), { ok: true });
    const cookies = login.headers.getSetCookie();
    assert.equal(cookies.length, 2);
    for (const cookie of cookies) {
      assert.match(cookie, /HttpOnly/i);
      assert.match(cookie, /Secure/i);
      assert.match(cookie, /SameSite=lax/i);
    }
    const account = await fetch(origin + "/my-account", { headers: { Cookie: `organizali_access=${access}` } });
    assert.equal(account.status, 200);
    const accountHtml = await account.text();
    assert.ok(accountHtml.includes("Morador de teste"));
    assert.ok(accountHtml.includes("test@example.test"));
    assert.ok(!accountHtml.includes(access));
    assert.ok(!accountHtml.includes(refresh));
    const forbiddenPage = await fetch(origin + "/members", { redirect: "manual", headers: { Cookie: `organizali_access=${access}` } });
    assert.equal(forbiddenPage.status, 307);
    assert.equal(forbiddenPage.headers.get("location"), "/no-permission");
    assert.ok(!calls.some(call => call.path === "/api/members"));
    const deniedInvite = await post("/api/auth/accept-invitation", { headers: { Origin: "https://foreign.example" }, body: { token: "secret", password: "password-long-enough" } });
    assert.equal(deniedInvite.status, 403);
    const blocked = await post("/api/backend/Auth/refresh", { headers: { Cookie: `organizali_access=${access}` } });
    assert.equal(blocked.status, 404);
    const mutation = await post("/api/backend/people", { headers: { Origin: "https://foreign.example" } });
    assert.equal(mutation.status, 403);
    const logout = await post("/api/auth/logout", { headers: { Cookie: `organizali_refresh=${refresh}` } });
    assert.equal(logout.status, 200);
    assert.ok(calls.some(call => call.path === "/api/auth/refresh"));
    assert.ok(calls.some(call => call.path === "/api/auth/logout" && call.authorization === `Bearer ${access}`));
    for (const cookie of logout.headers.getSetCookie()) assert.match(cookie, /Max-Age=0/i);
  } finally {
    web.kill();
    backend.closeAllConnections();
    await new Promise(resolve => backend.close(resolve));
  }
});
