import { requireSession } from "@/lib/session";

export default async function Household() {
  const session = await requireSession();
  return <section className="page-content">
    <h1>Olá, {session.name}.</h1>
    <p className="intro">Seu acesso ao Organizali está ativo.</p>
    <div className="empty-state"><h2>Vamos organizar a casa</h2>
      <p>Os cadastros e as ferramentas financeiras estão sendo preparados para o piloto. Eles aparecerão aqui quando estiverem disponíveis.</p>
    </div>
  </section>;
}
