import type { ReactNode } from "react";

/** Heading id used by the in-page navigation: "2.3 Cadastros financeiros (já carregados)" → "2-3-cadastros-financeiros-ja-carregados". */
export function slugify(text: string): string {
  return text.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "");
}

type Block =
  | { kind: "heading"; level: 1 | 2 | 3; text: string }
  | { kind: "paragraph"; text: string }
  | { kind: "list"; items: string[] }
  | { kind: "table"; header: string[]; rows: string[][] };

/** The subset of Markdown the tutorial uses: headings, paragraphs, bullet lists, pipe tables, bold, links and bare URLs. */
export function parseMarkdown(markdown: string): Block[] {
  const blocks: Block[] = [];
  const lines = markdown.replace(/\r\n/g, "\n").split("\n");
  let index = 0;
  while (index < lines.length) {
    const line = lines[index];
    if (!line.trim()) { index++; continue; }
    const heading = /^(#{1,3})\s+(.*)$/.exec(line);
    if (heading) { blocks.push({ kind: "heading", level: heading[1].length as 1 | 2 | 3, text: heading[2].trim() }); index++; continue; }
    if (line.startsWith("|")) {
      const rows: string[][] = [];
      while (index < lines.length && lines[index].startsWith("|")) {
        const cells = lines[index].trim().replace(/^\||\|$/g, "").split("|").map(cell => cell.trim());
        if (!cells.every(cell => /^:?-{2,}:?$/.test(cell))) rows.push(cells);
        index++;
      }
      const [header, ...body] = rows;
      blocks.push({ kind: "table", header: header ?? [], rows: body });
      continue;
    }
    if (/^- /.test(line)) {
      const items: string[] = [];
      while (index < lines.length && /^- /.test(lines[index])) { items.push(lines[index].slice(2).trim()); index++; }
      blocks.push({ kind: "list", items });
      continue;
    }
    const text: string[] = [];
    while (index < lines.length && lines[index].trim() && !/^(#{1,3})\s|^- |^\|/.test(lines[index])) { text.push(lines[index].trim()); index++; }
    blocks.push({ kind: "paragraph", text: text.join(" ") });
  }
  return blocks;
}

const INLINE = /(\*\*[^*]+\*\*|\[[^\]]+\]\([^)]+\)|https?:\/\/[^\s)]+)/g;

/** Bold, Markdown links and bare URLs; everything else is plain text, so the content can never inject markup. */
export function Inline({ text }: { text: string }) {
  const parts = text.split(INLINE).filter(Boolean);
  return <>{parts.map((part, position) => {
    if (part.startsWith("**") && part.endsWith("**")) return <strong key={position}>{part.slice(2, -2)}</strong>;
    const link = /^\[([^\]]+)\]\(([^)]+)\)$/.exec(part);
    if (link) return <a key={position} href={link[2]} className="text-[var(--primary)] underline-offset-2 hover:underline">{link[1]}</a>;
    if (/^https?:\/\//.test(part)) return <a key={position} href={part} className="break-all text-[var(--primary)] underline-offset-2 hover:underline">{part}</a>;
    return <span key={position}>{part}</span>;
  })}</>;
}

/** Renders the parsed blocks with the panel typography. Level-1 headings are skipped: the page carries its own title. */
export function Markdown({ blocks }: { blocks: Block[] }) {
  const output: ReactNode[] = [];
  blocks.forEach((block, position) => {
    switch (block.kind) {
      case "heading":
        if (block.level === 1) return;
        if (block.level === 2) output.push(<h2 key={position} id={slugify(block.text)} className="scroll-mt-20">{block.text}</h2>);
        else output.push(<h3 key={position} id={slugify(block.text)} className="scroll-mt-20">{block.text}</h3>);
        return;
      case "paragraph":
        output.push(<p key={position} className="text-sm leading-relaxed text-[var(--text-secondary)]"><Inline text={block.text} /></p>);
        return;
      case "list":
        output.push(<ul key={position} className="list-disc space-y-1.5 pl-5 text-sm leading-relaxed text-[var(--text-secondary)]">
          {block.items.map((item, line) => <li key={line}><Inline text={item} /></li>)}
        </ul>);
        return;
      case "table":
        output.push(<div key={position} className="table-scroll"><table className="finance-table help-table">
          <thead><tr>{block.header.map((cell, column) => <th key={column}><Inline text={cell} /></th>)}</tr></thead>
          <tbody>{block.rows.map((row, line) => <tr key={line}>{row.map((cell, column) => <td key={column}><Inline text={cell} /></td>)}</tr>)}</tbody>
        </table></div>);
        return;
    }
  });
  return <div className="help-content space-y-4">{output}</div>;
}
