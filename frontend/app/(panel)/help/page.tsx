import { requireSession } from "@/lib/session";
import { tutorial } from "@/content/tutorial";
import { Markdown, parseMarkdown, slugify } from "@/components/help/Markdown";

/** Ajuda: the family tutorial, rendered inside the panel so nobody depends on an external link. */
export default async function HelpPage() {
  await requireSession();
  const blocks = parseMarkdown(tutorial);
  const sections = blocks.filter(block => block.kind === "heading" && block.level === 2).map(block => block.kind === "heading" ? block.text : "");
  return <section className="page-content">
    <div className="mb-6">
      <h1>Ajuda</h1>
      <p className="mt-1 max-w-2xl text-sm text-[var(--text-secondary)]">Como usar o Organizali no dia a dia da casa: preparar os cadastros, lançar, pagar e acompanhar. Os nomes abaixo são os mesmos das telas.</p>
    </div>
    <div className="grid gap-6 lg:grid-cols-[240px_minmax(0,1fr)]">
      <nav aria-label="Seções da ajuda" className="lg:sticky lg:top-20 lg:self-start">
        <ol className="space-y-1 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-3 text-sm">
          {sections.map(section => <li key={section}><a href={`#${slugify(section)}`} className="block rounded-md px-2 py-1.5 text-[var(--text-secondary)] hover:bg-[var(--surface-2)] hover:text-[var(--primary)]">{section}</a></li>)}
        </ol>
      </nav>
      <article className="rounded-xl border border-[var(--border)] bg-[var(--surface)] p-5 shadow-[var(--shadow)] sm:p-7">
        <Markdown blocks={blocks} />
      </article>
    </div>
  </section>;
}
