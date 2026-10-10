import { House, Wallet, CalendarDays, Users } from "lucide-react";

export default function Home() {
  return (
    <main className="welcome">
      <section className="welcome-brand" aria-label="Organizali Finanças">
        <div className="brand"><House aria-hidden="true" size={28} /><span>Organizali<span className="brand-subtitle">Finanças da casa</span></span></div>
        <div className="welcome-message">
          <p className="eyebrow">Sua casa, em conjunto</p>
          <h1>Mais clareza para cuidar do que é de todos.</h1>
          <p>Rendas, contas e planos da família em um só lugar.</p>
        </div>
        <p className="brand-footer">Organização para o dia a dia.</p>
      </section>
      <section className="welcome-content" aria-labelledby="pilot-title">
        <p className="eyebrow">Piloto por convite</p>
        <h2 id="pilot-title">Estamos preparando sua nova rotina financeira.</h2>
        <p className="intro">O acesso às primeiras casas será liberado por convite. O cadastro e o painel ainda estão em preparação.</p>
        <ul className="feature-list">
          <li><Wallet aria-hidden="true" /><div><h3>Saiba o que entra e o que sai</h3><p>Acompanhe rendas, despesas e o saldo da casa.</p></div></li>
          <li><CalendarDays aria-hidden="true" /><div><h3>Planeje os próximos dias</h3><p>Reúna vencimentos, parcelas e orçamento.</p></div></li>
          <li><Users aria-hidden="true" /><div><h3>Organize as responsabilidades</h3><p>Compartilhe as informações e combine a contribuição de cada pessoa.</p></div></li>
        </ul>
        <p className="availability">Em preparação · Nenhum cadastro está sendo recebido nesta etapa.</p>
      </section>
    </main>
  );
}
