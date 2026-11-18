import { useState, type FormEvent } from "react";
import { localDate, type Priority, type Todo } from "../../api/todos";
import { useTodos } from "./useTodos";
import "./todos.css";

const PRIORITY_LABEL: Record<Priority, string> = { low: "низкий", normal: "", high: "высокий" };

// "2027-01-15" -> "15.01.2027", без сдвига часовых поясов
function formatDate(iso: string): string {
  const [y, m, d] = iso.split("-");
  return `${d}.${m}.${y}`;
}

export default function Todos() {
  const t = useTodos();
  const [title, setTitle] = useState("");
  const [due, setDue] = useState("");
  const [priority, setPriority] = useState<Priority>("normal");

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const text = title.trim();
    if (!text) return;

    const added = await t.add({ title: text, priority, dueDate: due || null });
    if (added) {
      setTitle("");
      setDue("");
      setPriority("normal");
    }
  };

  const today = localDate();

  return (
    <div className="td">
      <form onSubmit={submit}>
        <div className="td-row">
          <input
            type="text"
            value={title}
            maxLength={200}
            placeholder="Новая задача"
            onChange={(e) => setTitle(e.target.value)}
          />
        </div>
        <div className="td-row">
          <div className={due ? "td-date" : "td-date empty"}>
            {/* на iPhone пустое поле даты вообще ничего не рисует: показываем свою подпись поверх */}
            <span className="td-date-ph" aria-hidden="true">
              Срок (необязательно)
            </span>
            <input type="date" value={due} aria-label="Срок" onChange={(e) => setDue(e.target.value)} />
            {due && (
              <button type="button" className="td-date-clear" aria-label="Убрать срок" onClick={() => setDue("")}>
                ✕
              </button>
            )}
          </div>
          <select value={priority} title="Приоритет" onChange={(e) => setPriority(e.target.value as Priority)}>
            <option value="low">Низкий</option>
            <option value="normal">Обычный</option>
            <option value="high">Высокий</option>
          </select>
          <button disabled={!title.trim()}>Добавить</button>
        </div>
      </form>

      {t.error && (
        <p className="td-error" role="alert">
          {t.error}
        </p>
      )}
      {!t.loaded && !t.error && <p className="td-muted">Загрузка…</p>}

      <ul className="td-list">
        {t.items.map((todo) => (
          <TodoRow key={todo.id} todo={todo} today={today} onToggle={t.toggle} onRemove={t.remove} />
        ))}
      </ul>

      {t.totalPages > 1 && (
        <div className="td-pager">
          <button type="button" disabled={t.page <= 1} onClick={() => void t.goTo(t.page - 1)}>
            ← Назад
          </button>
          <span>
            Страница {t.page} из {t.totalPages}
          </span>
          <button type="button" disabled={t.page >= t.totalPages} onClick={() => void t.goTo(t.page + 1)}>
            Вперёд →
          </button>
        </div>
      )}
    </div>
  );
}

interface RowProps {
  todo: Todo;
  today: string;
  onToggle: (todo: Todo, isDone: boolean) => void;
  onRemove: (todo: Todo) => void;
}

function TodoRow({ todo, today, onToggle, onRemove }: RowProps) {
  const className = ["td-item", todo.isDone && "done", todo.priority].filter(Boolean).join(" ");
  const overdue = !todo.isDone && todo.dueDate !== null && todo.dueDate < today;

  return (
    <li className={className}>
      <input
        type="checkbox"
        checked={todo.isDone}
        aria-label={`Выполнено: ${todo.title}`}
        onChange={(e) => onToggle(todo, e.target.checked)}
      />
      <span className="td-title">{todo.title}</span>
      {PRIORITY_LABEL[todo.priority] && <span className="td-prio">{PRIORITY_LABEL[todo.priority]}</span>}
      {todo.dueDate && <span className={overdue ? "td-due overdue" : "td-due"}>{formatDate(todo.dueDate)}</span>}
      <button type="button" onClick={() => onRemove(todo)}>
        Удалить
      </button>
    </li>
  );
}
