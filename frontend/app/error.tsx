"use client";

export default function ErrorPage({ reset }: { reset: () => void }) {
  return <main className="session-message"><h1>Não foi possível carregar esta página</h1>
    <p>Confira sua conexão e tente novamente.</p><button className="primary-button" onClick={reset}>Tentar novamente</button>
  </main>;
}
