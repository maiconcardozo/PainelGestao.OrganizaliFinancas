import { NextResponse } from "next/server";
import { clearSession, rejectForeignOrigin } from "@/lib/auth-http";
import { INTERNAL_API_URL } from "@/lib/config";
import { getAccessToken } from "@/lib/session";

/** Own password change. The API closes every session of the user, so the cookies are cleared on success. */
export async function POST(request: Request) {
  const rejected = rejectForeignOrigin(request);
  if (rejected) return rejected;
  const body = await request.json().catch(() => null);
  if (typeof body?.currentPassword !== "string" || body.currentPassword.length > 128 || typeof body?.newPassword !== "string" || body.newPassword.length > 128)
    return NextResponse.json({ detail: "Dados inválidos." }, { status: 400 });
  const token = await getAccessToken();
  if (!token) return NextResponse.json({ detail: "Entre novamente." }, { status: 401 });
  try {
    const response = await fetch(`${INTERNAL_API_URL}/api/auth/change-password`, {
      method: "POST", headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
      body: JSON.stringify({ currentPassword: body.currentPassword, newPassword: body.newPassword }), cache: "no-store", redirect: "error", signal: AbortSignal.timeout(10000),
    });
    if (response.ok) return clearSession();
    const error = await response.json().catch(() => null);
    return NextResponse.json({ detail: Object.values(error?.errors ?? {}).flat().join(" ") || error?.detail || "Não foi possível trocar a senha." }, { status: response.status });
  } catch { return NextResponse.json({ detail: "Servidor indisponível. Tente novamente." }, { status: 503 }); }
}
