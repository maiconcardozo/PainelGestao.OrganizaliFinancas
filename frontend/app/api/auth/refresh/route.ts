import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { REFRESH_COOKIE } from "@/lib/config";
import { exchangeTokens, rejectForeignOrigin } from "@/lib/auth-http";
export async function POST(request: Request) {
  const rejected = rejectForeignOrigin(request);
  if (rejected) return rejected;
  const refreshToken = (await cookies()).get(REFRESH_COOKIE)?.value;
  if (!refreshToken) return NextResponse.json({ detail: "Entre novamente." }, { status: 401 });
  return exchangeTokens("refresh", { refreshToken });
}
