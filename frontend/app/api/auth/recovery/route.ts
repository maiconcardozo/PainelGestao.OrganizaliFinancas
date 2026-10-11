import { NextResponse } from "next/server";
import { forwardedFor, rejectForeignOrigin } from "@/lib/auth-http";
import { INTERNAL_API_URL } from "@/lib/config";

/** Same answer whether the e-mail exists or not; only "unavailable" (no provider configured) is reported. */
export async function POST(request: Request) {
  const rejected = rejectForeignOrigin(request);
  if (rejected) return rejected;
  const body = await request.json().catch(() => null);
  if (typeof body?.email !== "string" || body.email.length > 180) return NextResponse.json({ detail: "Informe um e-mail válido." }, { status: 400 });
  try {
    const response = await fetch(`${INTERNAL_API_URL}/api/auth/recovery`, {
      method: "POST", headers: { "Content-Type": "application/json", ...forwardedFor(request) }, body: JSON.stringify({ email: body.email }),
      cache: "no-store", redirect: "error", signal: AbortSignal.timeout(20000),
    });
    if (response.ok) return new NextResponse(null, { status: 202 });
    const error = await response.json().catch(() => null);
    return NextResponse.json({ detail: error?.detail ?? "Não foi possível solicitar a redefinição." }, { status: response.status });
  } catch { return NextResponse.json({ detail: "Servidor indisponível. Tente novamente." }, { status: 503 }); }
}
