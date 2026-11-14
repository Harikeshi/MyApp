import type { TodoStats } from "../../api/todos";
import { useStats } from "./useStats";
import "./stats.css";

type PriorityRow = { key: "high" | "normal" | "low"; label: string; count: number };

const priorityRows = (s: TodoStats): PriorityRow[] => [
  { key: "high", label: "Высокий", count: s.highPriorityOpen },
  { key: "normal", label: "Обычный", count: s.normalPriorityOpen },
  { key: "low", label: "Низкий", count: s.lowPriorityOpen },
];

export default function Stats() {
  const { stats, error } = useStats();

  if (error) return <p className="st-error">{error}</p>;
  if (!stats) return <p className="st-muted">Загрузка…</p>;

  const percent = stats.total ? Math.round((stats.done / stats.total) * 100) : 0;
  const rows = priorityRows(stats);
  const maxCount = Math.max(1, ...rows.map((r) => r.count));

  return (
    <div className="st">
      <div className="st-cards">
        <Card value={stats.total} label="всего" />
        <Card value={stats.done} label="выполнено" />
        <Card value={stats.open} label="в работе" />
        <Card value={stats.overdue} label="просрочено" warn={stats.overdue > 0} />
      </div>

      <h3>Выполнено: {percent}%</h3>
      <div className="st-bar"><div style={{ width: `${percent}%` }} /></div>

      <h3>Активные по приоритету</h3>
      {rows.map(({ key, label, count }) => (
        <div className="st-row" key={key}>
          <span>{label}</span>
          <div className="st-bar"><div className={`p-${key}`} style={{ width: `${(count / maxCount) * 100}%` }} /></div>
          <span>{count}</span>
        </div>
      ))}
    </div>
  );
}

function Card({ value, label, warn = false }: { value: number; label: string; warn?: boolean }) {
  return (
    <div className={warn ? "st-card warn" : "st-card"}>
      <b>{value}</b>
      <span>{label}</span>
    </div>
  );
}
