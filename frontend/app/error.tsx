"use client";
import { AuthShell } from "@/components/layout/AuthShell";

export default function ErrorPage({ reset }: { reset: () => void }) {
  return <AuthShell><h1>Não foi possível carregar esta página</h1>
    <p>Confira sua conexão e tente novamente.</p><button className="primary-button" onClick={reset}>Tentar novamente</button>
  </AuthShell>;
}
