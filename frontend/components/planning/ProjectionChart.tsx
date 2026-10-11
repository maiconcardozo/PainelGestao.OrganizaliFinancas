import { civilDate, money } from "@/lib/finance";
import type { Projection } from "@/lib/planning";

const W = 800, H = 230, PAD = { top: 26, right: 16, bottom: 10, left: 16 };
const dayNumber = (date: string) => Date.parse(`${date}T00:00:00Z`) / 86400000;

/**
 * Step chart of the projected balance: it only changes on days with events, as real balances do.
 * The zero line and the first deficit are the two things the household must see at a glance.
 */
export function ProjectionChart({ projection }: { projection: Projection }) {
  const start = Number(projection.start);
  const first = dayNumber(projection.from), last = Math.max(dayNumber(projection.to), first + 1);
  const steps: [number, number][] = [[first, start], ...projection.days.map(day => [dayNumber(day.date), Number(day.balance)] as [number, number])];
  const values = steps.map(([, value]) => value);
  const high = Math.max(...values, 0), low = Math.min(...values, 0);
  const span = high - low || 1;
  const x = (day: number) => PAD.left + ((day - first) / (last - first)) * (W - PAD.left - PAD.right);
  const y = (value: number) => PAD.top + ((high - value) / span) * (H - PAD.top - PAD.bottom);
  const path = steps.slice(1).reduce((d, [day, value]) => `${d} H ${x(day)} V ${y(value)}`, `M ${x(first)} ${y(start)}`) + ` H ${x(last)}`;
  const area = `${path} V ${y(0)} H ${x(first)} Z`;
  // Labels are HTML placed in percentages, so they keep their size while the drawing stretches to the width.
  const at = (px: number, py: number) => ({ left: `${(px / W) * 100}%`, top: `${(py / H) * 100}%` });
  const deficit = projection.firstDeficit ? x(dayNumber(projection.firstDeficit)) : null;
  const summary = projection.firstDeficit
    ? `Saldo projetado começa em ${money(projection.start)} e fica negativo em ${civilDate(projection.firstDeficit)}; o menor valor é ${money(projection.lowest)} em ${civilDate(projection.lowestDate)}.`
    : `Saldo projetado começa em ${money(projection.start)} e termina em ${money(projection.end)} em ${civilDate(projection.to)}, sem ficar negativo.`;
  return <figure className="projection-chart"><div className="chart-canvas">
    <svg viewBox={`0 0 ${W} ${H}`} role="img" aria-labelledby="projection-title projection-desc" preserveAspectRatio="none">
      <title id="projection-title">Saldo projetado</title><desc id="projection-desc">{summary}</desc>
      <clipPath id="below-zero"><rect x="0" y={y(0)} width={W} height={H} /></clipPath>
      <clipPath id="above-zero"><rect x="0" y="0" width={W} height={y(0)} /></clipPath>
      <path d={area} className="area-positive" clipPath="url(#above-zero)" />
      <path d={area} className="area-negative" clipPath="url(#below-zero)" />
      <line x1={PAD.left} x2={W - PAD.right} y1={y(0)} y2={y(0)} className="zero-line" />
      <path d={path} className="balance-line" />
      {deficit !== null && <line x1={deficit} x2={deficit} y1={PAD.top - 8} y2={H - PAD.bottom} className="deficit-line" />}
    </svg>
    <span aria-hidden="true" className="chart-label start" style={at(PAD.left, y(start))}>{money(projection.start)}</span>
    <span aria-hidden="true" className="chart-label end" style={at(W - PAD.right, y(Number(projection.end)))}>{money(projection.end)}</span>
    {deficit !== null && <span aria-hidden="true" className="chart-label deficit" style={at(deficit, PAD.top)}>Déficit em {civilDate(projection.firstDeficit!)}</span>}
    </div>
    <div className="chart-axis" aria-hidden="true"><span>{civilDate(projection.from)}</span><span>{civilDate(projection.to)}</span></div>
    <figcaption>{summary}</figcaption>
  </figure>;
}
