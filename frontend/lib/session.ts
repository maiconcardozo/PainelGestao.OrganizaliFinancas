import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { ACCESS_COOKIE, REFRESH_COOKIE, INTERNAL_API_URL } from "./config";

export type Session = {
  userCode: string; name: string; email: string; roles: string[]; permissions: string[];
  menu: { key: string; name: string; route: string; icon: string }[];
};

/** Reads the HttpOnly credential only on the server. */
export async function getAccessToken(): Promise<string | undefined> {
  return (await cookies()).get(ACCESS_COOKIE)?.value;
}

export async function requireSession(): Promise<Session> {
  const token = await getAccessToken();
  const signIn = async () => {
    if ((await cookies()).has(REFRESH_COOKIE)) redirect("/session-renew");
    redirect("/login");
  };
  if (!token) return signIn();
  const response = await fetch(`${INTERNAL_API_URL}/api/auth/me`, {
    headers: { Authorization: `Bearer ${token}` }, cache: "no-store",
    redirect: "error", signal: AbortSignal.timeout(10000),
  });
  if (response.status === 401) return signIn();
  if (!response.ok) throw new Error("Não foi possível consultar sua sessão.");
  return (await response.json()).data as Session;
}

export async function requirePermission(permission: string): Promise<Session> {
  const session = await requireSession();
  if (!session.permissions.includes(permission)) redirect("/no-permission");
  return session;
}
