import { NextResponse, type NextRequest } from "next/server";
import { ACCESS_COOKIE, REFRESH_COOKIE, COOKIE_SECURE, INTERNAL_API_URL } from "@/lib/config";

type Tokens = { accessToken: string; accessTokenExpiresAt: string; refreshToken: string; refreshTokenExpiresAt: string };

/**
 * Silent session renewal. The access cookie lives for a short time; when it is gone but the refresh cookie is still there,
 * the request is served with a fresh pair of tokens instead of bouncing the person to a "continue" or login screen.
 * Every request also learns its own path (x-pathname) so a server component can send the person back after renewing.
 */
export async function proxy(request: NextRequest) {
  const headers = new Headers(request.headers);
  headers.set("x-pathname", request.nextUrl.pathname + request.nextUrl.search);
  const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value;
  // Prefetches are speculative: a renewal for them would only spend the refresh token's rate budget.
  const prefetch = request.headers.get("next-router-prefetch") === "1" || request.headers.get("purpose") === "prefetch";
  if (request.cookies.has(ACCESS_COOKIE) || !refreshToken || prefetch) return NextResponse.next({ request: { headers } });
  try {
    const forwarded = request.headers.get("x-forwarded-for");
    const result = await fetch(`${INTERNAL_API_URL}/api/auth/refresh`, {
      method: "POST", headers: { "Content-Type": "application/json", ...(forwarded && /^[0-9a-fA-F:., ]{1,200}$/.test(forwarded) ? { "X-Forwarded-For": forwarded } : {}) },
      body: JSON.stringify({ refreshToken }), cache: "no-store", redirect: "error", signal: AbortSignal.timeout(10000),
    });
    if (result.status === 401) {
      // The refresh token is gone for good (expired, signed out elsewhere, password changed): clear it so the next stop is the login page.
      headers.set("cookie", keptCookies(request).join("; "));
      const response = NextResponse.next({ request: { headers } });
      for (const name of [ACCESS_COOKIE, REFRESH_COOKIE])
        response.cookies.set(name, "", { httpOnly: true, sameSite: "lax", secure: COOKIE_SECURE, path: "/", maxAge: 0 });
      return response;
    }
    if (!result.ok) return NextResponse.next({ request: { headers } });
    const tokens: Tokens = (await result.json()).data.tokens;
    // The page being rendered right now must already see the new access cookie.
    headers.set("cookie", [...keptCookies(request), `${ACCESS_COOKIE}=${tokens.accessToken}`, `${REFRESH_COOKIE}=${tokens.refreshToken}`].join("; "));
    const response = NextResponse.next({ request: { headers } });
    const options = { httpOnly: true, sameSite: "lax" as const, secure: COOKIE_SECURE, path: "/" };
    response.cookies.set(ACCESS_COOKIE, tokens.accessToken, { ...options, expires: new Date(tokens.accessTokenExpiresAt) });
    response.cookies.set(REFRESH_COOKIE, tokens.refreshToken, { ...options, expires: new Date(tokens.refreshTokenExpiresAt) });
    return response;
  } catch {
    return NextResponse.next({ request: { headers } });
  }
}

/** The request's cookies without the two session cookies, so the forwarded request carries only what the proxy decided. */
function keptCookies(request: NextRequest): string[] {
  return (request.headers.get("cookie") ?? "").split(";").map(part => part.trim())
    .filter(part => part && !part.startsWith(`${ACCESS_COOKIE}=`) && !part.startsWith(`${REFRESH_COOKIE}=`));
}

export const config = {
  // Pages and the authenticated proxy; never the token routes, static assets or the public screens.
  matcher: ["/((?!api/auth|_next/static|_next/image|favicon.ico|icon|manifest|login|forgot-password|reset-password|accept-invitation|raiz-organizali).*)"],
};
