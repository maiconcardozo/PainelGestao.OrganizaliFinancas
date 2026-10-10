/** Server-only API origin, following RevendaPRO. */
export const INTERNAL_API_URL = process.env.INTERNAL_API_URL ?? "http://127.0.0.1:5189";
export const ACCESS_COOKIE = "organizali_access";
export const REFRESH_COOKIE = "organizali_refresh";
export const COOKIE_SECURE = process.env.COOKIE_SECURE
  ? process.env.COOKIE_SECURE !== "false"
  : process.env.NODE_ENV === "production";
