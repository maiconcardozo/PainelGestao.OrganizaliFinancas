import { NextResponse } from "next/server";
import { rejectForeignOrigin } from "@/lib/auth-http";
import { INTERNAL_API_URL } from "@/lib/config";

export async function POST(request: Request) {
  const rejected = rejectForeignOrigin(request);
  if (rejected) return rejected;
  const body = await request.json().catch(() => null);
  if (typeof body?.token !== "string" || body.token.length > 256 || typeof body?.password !== "string" || body.password.length > 128)
    return NextResponse.json({ detail: "Dados inválidos." }, { status: 400 });
  try {
    const response = await fetch(`${INTERNAL_API_URL}/api/auth/accept-invitation`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token: body.token, password: body.password }),
      cache: "no-store", redirect: "error", signal: AbortSignal.timeout(10000),
    });
    if (response.ok) return new NextResponse(null, { status: 204 });
    const error = await response.json().catch(() => null);
    return NextResponse.json({ detail: error?.detail ?? "Não foi possível aceitar o convite." }, { status: response.status });
  } catch { return NextResponse.json({ detail: "Servidor indisponível. Tente novamente." }, { status: 503 }); }
}
