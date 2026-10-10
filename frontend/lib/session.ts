import { cookies } from "next/headers";
import { ACCESS_COOKIE } from "./config";

/** Reads the HttpOnly credential on the server. Session verification is implemented with M2. */
export async function getAccessToken(): Promise<string | undefined> {
  return (await cookies()).get(ACCESS_COOKIE)?.value;
}
