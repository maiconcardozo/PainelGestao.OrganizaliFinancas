import { INTERNAL_API_URL } from "@/lib/config";

/** Public technical probe. The authenticated proxy is completed with the M2 session. */
export async function GET() {
  try {
    const response = await fetch(`${INTERNAL_API_URL}/health/live`, {
      cache: "no-store",
      redirect: "error",
      signal: AbortSignal.timeout(5000),
    });
    return Response.json({ status: response.ok ? "Healthy" : "Unavailable" },
      { status: response.ok ? 200 : 503 });
  } catch {
    return Response.json({ status: "Unavailable" }, { status: 503 });
  }
}
