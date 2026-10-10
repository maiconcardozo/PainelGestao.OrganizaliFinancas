import { INTERNAL_API_URL } from "./config";
import { getAccessToken } from "./session";

/** Authenticated server-side requests, following RevendaPRO/lib/server.ts. */
export async function fetchFromApi<T>(path: string, query?: Record<string, string>): Promise<T> {
  const token = await getAccessToken();
  if (!token) throw new Error("Sessão necessária.");
  if (!/^[a-zA-Z0-9/_-]+$/.test(path) || path.includes(".."))
    throw new Error("Caminho de API inválido.");
  const search = query ? `?${new URLSearchParams(query)}` : "";

  const response = await fetch(`${INTERNAL_API_URL}/api/${path}${search}`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
    redirect: "error",
    signal: AbortSignal.timeout(5000),
  });
  if (!response.ok) throw new Error("Não foi possível consultar os dados.");
  const body = await response.json();
  return body.data as T;
}
