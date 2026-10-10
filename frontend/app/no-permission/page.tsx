import Link from "next/link";
export default function NoPermission() {
  return <main className="session-message"><h1>Acesso não permitido</h1>
    <p>Seu perfil não permite abrir esta tela. Converse com um titular da casa.</p><Link href="/household">Voltar à minha casa</Link></main>;
}
