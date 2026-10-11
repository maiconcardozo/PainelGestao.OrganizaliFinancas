import { NextResponse } from "next/server";
import { exchangeTokens, forwardedFor, rejectForeignOrigin } from "@/lib/auth-http";
export async function POST(request: Request) {
  const rejected = rejectForeignOrigin(request);
  if (rejected) return rejected;
  try {
    const { email, password } = await request.json();
    if (typeof email !== "string" || typeof password !== "string" || email.length > 180 || password.length > 128)
      return NextResponse.json({ detail: "Dados inválidos." }, { status: 400 });
    return exchangeTokens("login", { email, password }, forwardedFor(request));
  } catch {
    return NextResponse.json({ detail: "Dados inválidos." }, { status: 400 });
  }
}
