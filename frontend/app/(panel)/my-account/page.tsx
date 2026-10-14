import { requireSession } from "@/lib/session";
import { ChangePassword } from "@/components/households/ChangePassword";

export default async function MyAccount() {
  const session = await requireSession();
  return <section className="page-content"><h1>Minha conta</h1>
    <p className="intro">Dados do seu acesso ao Organizali.</p>
    <dl className="account-details"><dt>Nome</dt><dd>{session.name}</dd><dt>E-mail</dt><dd>{session.email}</dd></dl>
    <div className="mt-6"><ChangePassword /></div>
    <p className="form-hint mt-3">Esqueceu a senha atual? Um titular da casa pode gerar um link de nova senha em Integrantes.</p>
  </section>;
}
