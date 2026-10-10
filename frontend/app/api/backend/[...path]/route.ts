import { NextResponse } from "next/server";
import { INTERNAL_API_URL } from "@/lib/config";
import { getAccessToken } from "@/lib/session";
import { rejectForeignOrigin } from "@/lib/auth-http";

async function forward(request: Request, path: string[]) {
  const readOnly = request.method === "GET";
  if (!readOnly) {
    const rejected = rejectForeignOrigin(request);
    if (rejected) return rejected;
  }
  // Token endpoints must never be forwarded to browser JavaScript.
  if (path[0]?.toLowerCase() === "auth" || path.some(segment => !/^[a-zA-Z0-9_-]+$/.test(segment)))
    return NextResponse.json({ detail: "Rota indisponível." }, { status: 404 });
  const token = await getAccessToken();
  if (!token) return NextResponse.json({ detail: "Entre novamente." }, { status: 401 });
  const url = new URL(request.url);
  const headers: Record<string, string> = { Authorization: `Bearer ${token}`, "Content-Type": "application/json" };
  // Financial writes are deduplicated by the API; only a well-formed UUID is forwarded.
  const idempotencyKey = request.headers.get("idempotency-key");
  if (idempotencyKey && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(idempotencyKey))
    headers["Idempotency-Key"] = idempotencyKey;
  try {
    const response = await fetch(`${INTERNAL_API_URL}/api/${path.join("/")}${url.search}`, {
      method: request.method,
      headers,
      body: readOnly ? undefined : await request.text(),
      cache: "no-store", redirect: "error", signal: AbortSignal.timeout(10000),
    });
    return new NextResponse(response.status === 204 ? null : await response.arrayBuffer(), {
      status: response.status,
      headers: { "Content-Type": response.headers.get("content-type") ?? "application/json", "Cache-Control": "no-store" },
    });
  } catch {
    return NextResponse.json({ detail: "Servidor indisponível." }, { status: 503 });
  }
}
type Context = { params: Promise<{ path: string[] }> };
export async function GET(request: Request, { params }: Context) { return forward(request, (await params).path); }
export async function POST(request: Request, { params }: Context) { return forward(request, (await params).path); }
export async function PUT(request: Request, { params }: Context) { return forward(request, (await params).path); }
export async function PATCH(request: Request, { params }: Context) { return forward(request, (await params).path); }
export async function DELETE(request: Request, { params }: Context) { return forward(request, (await params).path); }
