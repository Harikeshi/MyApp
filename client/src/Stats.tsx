import { useCallback, useEffect, useState } from "react";

type Priority = "low" | "normal" | "high";
type Todo = { id: number; title: string; isDone: boolean; dueDate?: string | null; priority: Priority };

const PRIORITY_LABEL: Record<Priority, string> = { low: "Низкий", normal: "Обычный", high: "Высокий" };

export default function Stats() {
  const [todos, setTodos] = useState<Todo[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    const token = sessionStorage.getItem("token");   // тот же токен, что хранит index.html
    if (!token) return;
    try {
      const r = await fetch("/todos", { headers: { Authorization: "Bearer " + token } });
      if (!r.ok) throw new Error(r.status === 401 ? "Сессия истекла, войдите снова" : "Ошибка " + r.status);
      setTodos(await r.json());
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Нет связи с сервером");
    }
  }, []);

  // index.html сообщает о смене вкладки: обновляем данные, когда открыта «Статистика»
  useEffect(() => {
    const onTab = (e: Event) => {
      if ((e as CustomEvent<string>).detail === "stats") load();
    };
    window.addEventListener("tabchange", onTab);
    return () => window.removeEventListener("tabchange", onTab);
  }, [load]);

  if (error) return <p className="st-error">{error}</p>;
  if (!todos) return <p className="st-muted">Загрузка…</p>;

  const today = new Date().toLocaleDateString("sv-SE");   // локальная дата YYYY-MM-DD
  const total = todos.length;
  const done = todos.filter((t) => t.isDone).length;
  const overdue = todos.filter((t) => !t.isDone && t.dueDate && t.dueDate < today).length;
  const percent = total ? Math.round((done / total) * 100) : 0;
  const byPriority = (["high", "normal", "low"] as Priority[]).map((p) => ({
    p,
    count: todos.filter((t) => t.priority === p && !t.isDone).length,
  }));
  const maxCount = Math.max(1, ...byPriority.map((x) => x.count));

  return (
    <div className="st">
      <div className="st-cards">
        <div className="st-card"><b>{total}</b><span>всего</span></div>
        <div className="st-card"><b>{done}</b><span>выполнено</span></div>
        <div className="st-card"><b>{total - done}</b><span>в работе</span></div>
        <div className={"st-card" + (overdue ? " warn" : "")}><b>{overdue}</b><span>просрочено</span></div>
      </div>

      <h3>Выполнено: {percent}%</h3>
      <div className="st-bar"><div style={{ width: percent + "%" }} /></div>

      <h3>Активные по приоритету</h3>
      {byPriority.map(({ p, count }) => (
        <div className="st-row" key={p}>
          <span>{PRIORITY_LABEL[p]}</span>
          <div className="st-bar"><div className={"p-" + p} style={{ width: (count / maxCount) * 100 + "%" }} /></div>
          <span>{count}</span>
        </div>
      ))}
    </div>
  );
}
