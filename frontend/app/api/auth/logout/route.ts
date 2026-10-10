import { getAccessToken } from "@/lib/session";
import { INTERNAL_API_URL, REFRESH_COOKIE } from "@/lib/config";
import { clearSession, rejectForeignOrigin } from "@/lib/auth-http";
import { NextResponse } from "next/server";
import { cookies } from "next/headers";
export async function POST(request: Request) {
  const rejected = rejectForeignOrigin(request);
  if (rejected) return rejected;
  let token = await getAccessToken();
  const refreshToken = (await cookies()).get(REFRESH_COOKIE)?.value;
  if (refreshToken) {
    try {
      const renewed = await fetch(`${INTERNAL_API_URL}/api/auth/refresh`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken }), cache: "no-store", redirect: "error",
        signal: AbortSignal.timeout(10000),
      });
      if (renewed.status === 401) {
        if (!token) return clearSession();
      } else {
        if (!renewed.ok) return NextResponse.json({ detail: "Tente encerrar a sessão novamente." }, { status: 503 });
        token = (await renewed.json()).data.tokens.accessToken;
      }
    } catch {
      return NextResponse.json({ detail: "Tente encerrar a sessão novamente." }, { status: 503 });
    }
  }
  if (token) {
    try {
      const response = await fetch(`${INTERNAL_API_URL}/api/auth/logout`, {
        method: "POST", headers: { Authorization: `Bearer ${token}` },
        cache: "no-store", redirect: "error", signal: AbortSignal.timeout(10000),
      });
      if (!response.ok && response.status !== 401)
        return NextResponse.json({ detail: "Não foi possível encerrar a sessão. Tente novamente." }, { status: 503 });
    } catch {
      return NextResponse.json({ detail: "Não foi possível encerrar a sessão. Tente novamente." }, { status: 503 });
    }
  }
  return clearSession();
}
