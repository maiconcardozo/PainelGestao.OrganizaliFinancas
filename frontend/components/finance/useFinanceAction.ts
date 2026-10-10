"use client";
import { useRouter } from "next/navigation";
import { useState } from "react";

/** Mutations through the authenticated proxy. Financial writes carry one Idempotency-Key per attempt, reused on retry. */
export function useFinanceAction() {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [key, setKey] = useState(() => crypto.randomUUID());
  async function send<T>(path: string, method: string, payload: unknown, success: string, idempotent = false): Promise<T | null> {
    setBusy(true); setError(""); setMessage("");
    try {
      const headers: Record<string, string> = { "Content-Type": "application/json" };
      if (idempotent) headers["Idempotency-Key"] = key;
      const response = await fetch(`/api/backend/${path}`, { method, headers, body: payload === undefined ? undefined : JSON.stringify(payload) });
      if (response.status === 401) { router.replace("/session-renew"); return null; }
      const body = await response.json().catch(() => null);
      if (!response.ok) {
        setError(Object.values(body?.errors ?? {}).flat().join(" ") || body?.detail || "Não foi possível salvar. Confira os dados.");
        // A rejected request did not create anything; a new attempt with changed data needs a new key.
        if (response.status !== 503) setKey(crypto.randomUUID());
        return null;
      }
      setKey(crypto.randomUUID()); setMessage(success); router.refresh();
      return body?.data as T;
    } catch {
      // Network failure: keep the key so a retry cannot register the same entry twice.
      setError("Servidor indisponível. Tente novamente; o envio repetido não duplica o lançamento.");
      return null;
    } finally { setBusy(false); }
  }
  /** Reads that depend on user input (period, account) and therefore cannot be server-rendered with the page. */
  async function read<T>(path: string): Promise<T | null> {
    setBusy(true); setError("");
    try {
      const response = await fetch(`/api/backend/${path}`);
      if (response.status === 401) { router.replace("/session-renew"); return null; }
      const body = await response.json().catch(() => null);
      if (!response.ok) { setError(Object.values(body?.errors ?? {}).flat().join(" ") || body?.detail || "Não foi possível carregar os dados."); return null; }
      return body?.data as T;
    } catch { setError("Servidor indisponível. Tente novamente."); return null; }
    finally { setBusy(false); }
  }
  return { busy, message, error, send, read };
}
