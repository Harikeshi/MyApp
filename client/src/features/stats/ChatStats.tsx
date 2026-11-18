import { useMemo, useState } from "react";
import type { TokenStatRow } from "../../types/chats";
import { formatCompact, formatNumber } from "../chats/format";
import { useTokenStats } from "./useTokenStats";

const PERIODS = [7, 30, 90] as const;

type Metric = "total" | "input" | "output";
const METRICS: { id: Metric; label: string }[] = [
  { id: "total", label: "Всего" },
  { id: "input", label: "Вход" },
  { id: "output", label: "Выход" },
];

// цвета моделей: по убыванию расхода
const COLORS = ["#2457d6", "#e07b00", "#1b7f3b", "#8e44ad", "#c62828", "#0e8a8a", "#98a2b3"];

const valueOf = (r: Pick<TokenStatRow, "inputTokens" | "outputTokens">, metric: Metric) =>
  metric === "input" ? r.inputTokens : metric === "output" ? r.outputTokens : r.inputTokens + r.outputTokens;

const shortDate = (iso: string) => `${iso.slice(8, 10)}.${iso.slice(5, 7)}`; // 2026-11-17 -> 17.11
const longDate = (iso: string) => `${iso.slice(8, 10)}.${iso.slice(5, 7)}.${iso.slice(0, 4)}`;

/** Все дни от from до to включительно. Через UTC, чтобы переход на летнее время не сдвигал дни. */
function daysBetween(from: string, to: string): string[] {
  const parse = (iso: string) => Date.UTC(Number(iso.slice(0, 4)), Number(iso.slice(5, 7)) - 1, Number(iso.slice(8, 10)));
  const days: string[] = [];
  for (let t = parse(from); t <= parse(to); t += 86_400_000) days.push(new Date(t).toISOString().slice(0, 10));
  return days;
}

interface ModelTotal {
  id: string;
  name: string;
  input: number;
  output: number;
  requests: number;
}

export default function ChatStats({ active }: { active: boolean }) {
  const [days, setDays] = useState<number>(30);
  const [metric, setMetric] = useState<Metric>("total");
  const [selected, setSelected] = useState<string | null>(null);
  const { data, error, loading } = useTokenStats(active, days);

  const view = useMemo(() => {
    if (!data) return null;

    const totals = new Map<string, ModelTotal>();
    const byDay = new Map<string, TokenStatRow[]>();
    for (const r of data.rows) {
      const t = totals.get(r.modelId) ?? { id: r.modelId, name: r.modelName, input: 0, output: 0, requests: 0 };
      t.input += r.inputTokens;
      t.output += r.outputTokens;
      t.requests += r.requests;
      totals.set(r.modelId, t);

      const list = byDay.get(r.date);
      if (list) list.push(r);
      else byDay.set(r.date, [r]);
    }

    const models = [...totals.values()].sort((a, b) => b.input + b.output - (a.input + a.output));
    const colorOf = new Map(models.map((m, i) => [m.id, COLORS[i % COLORS.length]]));
    const axis = daysBetween(data.from, data.to);
    const lastDataDay = data.rows.length > 0 ? data.rows[data.rows.length - 1].date : null;
    return { models, colorOf, axis, byDay, lastDataDay };
  }, [data]);

  const switcher = (
    <div className="tk-controls">
      <div className="st-switch small" role="group" aria-label="Период">
        {PERIODS.map((p) => (
          <button
            key={p}
            type="button"
            className={p === days ? "on" : ""}
            aria-pressed={p === days}
            onClick={() => {
              setDays(p);
              setSelected(null);
            }}
          >
            {p} дн.
          </button>
        ))}
      </div>
      <div className="st-switch small" role="group" aria-label="Что показывать на графике">
        {METRICS.map((m) => (
          <button key={m.id} type="button" className={m.id === metric ? "on" : ""} aria-pressed={m.id === metric} onClick={() => setMetric(m.id)}>
            {m.label}
          </button>
        ))}
      </div>
    </div>
  );

  if (error) return <>{switcher}<p className="st-error">{error}</p></>;
  if (!data || !view) return <>{switcher}<p className="st-muted">Загрузка…</p></>;

  if (data.rows.length === 0) {
    return (
      <>
        {switcher}
        <p className="st-muted">
          За выбранный период данных по токенам нет. Подсчёт ведётся с момента обновления сервера: более старые ответы не учитываются.
        </p>
      </>
    );
  }

  const { models, colorOf, axis, byDay, lastDataDay } = view;
  const sum = (pick: (m: ModelTotal) => number) => models.reduce((s, m) => s + pick(m), 0);
  const totalInput = sum((m) => m.input);
  const totalOutput = sum((m) => m.output);
  const totalRequests = sum((m) => m.requests);

  const dayValue = (date: string) => (byDay.get(date) ?? []).reduce((s, r) => s + valueOf(r, metric), 0);
  const max = Math.max(0, ...axis.map(dayValue));
  const step = axis.length <= 8 ? 1 : axis.length <= 31 ? 5 : 15;

  const shown = selected ?? lastDataDay;
  const shownRows = shown ? (byDay.get(shown) ?? []) : [];

  return (
    <div className={loading ? "tk tk-loading" : "tk"}>
      {switcher}

      <div className="st-cards">
        <Card value={formatNumber(totalRequests)} label="запросов" />
        <Card value={formatCompact(totalInput)} label="токенов на входе" title={formatNumber(totalInput)} />
        <Card value={formatCompact(totalOutput)} label="токенов на выходе" title={formatNumber(totalOutput)} />
        <Card value={formatCompact(totalInput + totalOutput)} label="всего" title={formatNumber(totalInput + totalOutput)} />
      </div>

      <h3>По дням: {METRICS.find((m) => m.id === metric)!.label.toLowerCase()}</h3>
      <div className="tk-chart-wrap">
        <span className="tk-max">{formatCompact(max)}</span>
        <div className="tk-chart">
          {axis.map((date) => {
            const rows = byDay.get(date) ?? [];
            return (
              <button
                key={date}
                type="button"
                className={date === shown ? "tk-day on" : "tk-day"}
                title={`${shortDate(date)}: ${formatNumber(dayValue(date))}`}
                aria-label={`${longDate(date)}: ${formatNumber(dayValue(date))} токенов`}
                onClick={() => setSelected(date === selected ? null : date)}
              >
                {rows.map((r) => (
                  <span
                    key={r.modelId}
                    className="tk-seg"
                    style={{ height: `${max > 0 ? (valueOf(r, metric) / max) * 100 : 0}%`, background: colorOf.get(r.modelId) }}
                  />
                ))}
              </button>
            );
          })}
        </div>
        <div className="tk-axis">
          {axis.map((date, i) => (
            <span key={date}>{i % step === 0 || i === axis.length - 1 ? shortDate(date) : ""}</span>
          ))}
        </div>
      </div>
      <p className="st-muted tk-hint">Нажмите на столбец, чтобы увидеть детали за день.</p>

      {shown && (
        <div className="tk-detail">
          <b>{longDate(shown)}</b>
          {shownRows.length === 0 ? (
            <span className="st-muted">запросов не было</span>
          ) : (
            shownRows.map((r) => (
              <div className="tk-detail-row" key={r.modelId}>
                <i style={{ background: colorOf.get(r.modelId) }} />
                <span className="tk-detail-name">{r.modelName}</span>
                <span>
                  ↑ {formatNumber(r.inputTokens)} · ↓ {formatNumber(r.outputTokens)} · запросов: {r.requests}
                </span>
              </div>
            ))
          )}
        </div>
      )}

      <h3>По моделям за период</h3>
      <div className="tk-scroll">
        <table className="tk-table">
          <thead>
            <tr>
              <th>Модель</th>
              <th>Запросов</th>
              <th>Вход</th>
              <th>Выход</th>
              <th>Всего</th>
            </tr>
          </thead>
          <tbody>
            {models.map((m) => (
              <tr key={m.id}>
                <td>
                  <i className="tk-dot" style={{ background: colorOf.get(m.id) }} />
                  {m.name}
                </td>
                <td>{formatNumber(m.requests)}</td>
                <td>{formatNumber(m.input)}</td>
                <td>{formatNumber(m.output)}</td>
                <td>{formatNumber(m.input + m.output)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <details className="tk-details">
        <summary>Таблица по дням</summary>
        <div className="tk-scroll">
          <table className="tk-table">
            <thead>
              <tr>
                <th>День</th>
                <th>Модель</th>
                <th>Запросов</th>
                <th>Вход</th>
                <th>Выход</th>
              </tr>
            </thead>
            <tbody>
              {[...data.rows].reverse().map((r) => (
                <tr key={`${r.date}-${r.modelId}`}>
                  <td>{longDate(r.date)}</td>
                  <td>{r.modelName}</td>
                  <td>{formatNumber(r.requests)}</td>
                  <td>{formatNumber(r.inputTokens)}</td>
                  <td>{formatNumber(r.outputTokens)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </details>

      <p className="st-muted tk-hint">
        «Вход» — токены запроса: при каждом ответе модель получает всю историю чата, поэтому он растёт с длиной диалога. «Выход» — токены ответа.
      </p>
    </div>
  );
}

function Card({ value, label, title }: { value: string; label: string; title?: string }) {
  return (
    <div className="st-card" title={title}>
      <b>{value}</b>
      <span>{label}</span>
    </div>
  );
}
