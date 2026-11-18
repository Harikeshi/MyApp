import { useState } from "react";
import type { TodoStats } from "../../api/todos";
import ChatStats from "./ChatStats";
import { useStats } from "./useStats";
import "./stats.css";

type Mode = "todos" | "chats";
type PriorityRow = { key: "high" | "normal" | "low"; label: string; count: number };

const priorityRows = (s: TodoStats): PriorityRow[] => [
  { key: "high", label: "Высокий", count: s.highPriorityOpen },
  { key: "normal", label: "Обычный", count: s.normalPriorityOpen },
  { key: "low", label: "Низкий", count: s.lowPriorityOpen },
];

/** Статистика: переключатель между задачами и расходом токенов в чатах. Данные грузятся при каждом открытии вкладки. */
export default function Stats({ active }: { active: boolean }) {
  const [mode, setMode] = useState<Mode>("todos");

  return (
    <div className="st">
      <div className="st-switch" role="group" aria-label="Раздел статистики">
        <button type="button" className={mode === "todos" ? "on" : ""} aria-pressed={mode === "todos"} onClick={() => setMode("todos")}>
          Задачи
        </button>
        <button type="button" className={mode === "chats" ? "on" : ""} aria-pressed={mode === "chats"} onClick={() => setMode("chats")}>
          Чаты (токены)
        </button>
      </div>

      {mode === "todos" ? <TodoStatsView active={active} /> : <ChatStats active={active} />}
    </div>
  );
}

function TodoStatsView({ active }: { active: boolean }) {
  const { stats, error } = useStats(active);

  if (error) return <p className="st-error">{error}</p>;
  if (!stats) return <p className="st-muted">Загрузка…</p>;

  const percent = stats.total ? Math.round((stats.done / stats.total) * 100) : 0;
  const rows = priorityRows(stats);
  const maxCount = Math.max(1, ...rows.map((r) => r.count));

  return (
    <>
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
    </>
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
