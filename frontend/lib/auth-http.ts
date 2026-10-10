import { NextResponse } from "next/server";
import { ACCESS_COOKIE, REFRESH_COOKIE, COOKIE_SECURE, INTERNAL_API_URL } from "./config";

type Tokens = {
  accessToken: string; accessTokenExpiresAt: string;
  refreshToken: string; refreshTokenExpiresAt: string;
};

export function rejectForeignOrigin(request: Request): NextResponse | null {
  const origin = request.headers.get("origin");
  // The public origin can differ from Next's internal URL behind a reverse proxy.
  const expectedOrigin = process.env.APP_ORIGIN ?? new URL(request.url).origin;
  if (origin !== expectedOrigin)
    return NextResponse.json({ detail: "Origem da solicitação inválida." }, { status: 403 });
  return null;
}

export function clearSession(): NextResponse {
  const response = NextResponse.json({ ok: true });
  for (const name of [ACCESS_COOKIE, REFRESH_COOKIE])
    response.cookies.set(name, "", { httpOnly: true, sameSite: "lax", secure: COOKIE_SECURE, path: "/", maxAge: 0 });
  return response;
}

export async function exchangeTokens(operation: "login" | "refresh", payload: unknown): Promise<NextResponse> {
  try {
    const result = await fetch(`${INTERNAL_API_URL}/api/auth/${operation}`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload), cache: "no-store", redirect: "error", signal: AbortSignal.timeout(10000),
    });
    if (!result.ok) {
      const error = await result.json().catch(() => null);
      return NextResponse.json({ detail: error?.detail ?? "Não foi possível iniciar a sessão." }, { status: result.status });
    }
    const tokens: Tokens = (await result.json()).data.tokens;
    const output = NextResponse.json({ ok: true });
    const options = { httpOnly: true, sameSite: "lax" as const, secure: COOKIE_SECURE, path: "/" };
    output.cookies.set(ACCESS_COOKIE, tokens.accessToken, { ...options, expires: new Date(tokens.accessTokenExpiresAt) });
    output.cookies.set(REFRESH_COOKIE, tokens.refreshToken, { ...options, expires: new Date(tokens.refreshTokenExpiresAt) });
    return output;
  } catch {
    return NextResponse.json({ detail: "Servidor indisponível. Tente novamente." }, { status: 503 });
  }
}
