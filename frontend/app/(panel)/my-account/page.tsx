import { requireSession } from "@/lib/session";

export default async function MyAccount() {
  const session = await requireSession();
  return <section className="page-content"><h1>Minha conta</h1>
    <p className="intro">Dados do seu acesso ao Organizali.</p>
    <dl className="account-details"><dt>Nome</dt><dd>{session.name}</dd><dt>E-mail</dt><dd>{session.email}</dd></dl>
  </section>;
}
