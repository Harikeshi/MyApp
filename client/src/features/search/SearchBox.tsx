import { useEffect, useRef, useState, type ReactNode } from "react";
import { errorMessage } from "../../api/http";
import { MIN_QUERY, resetChatIndex, searchAll, type SearchResults } from "../../api/search";
import "./search.css";

/** Куда перейти по выбранному результату. */
export type NavTarget =
  | { kind: "todo"; id: number; projectId: number | null }
  | { kind: "project"; id: number }
  | { kind: "note"; id: number }
  | { kind: "chat"; id: string; modelId: string };

interface Props {
  onNavigate: (target: NavTarget) => void;
}

/** Строка поиска в шапке: по своим задачам, проектам, записям и названиям чатов. Результат ведёт в нужную вкладку. */
export default function SearchBox({ onNavigate }: Props) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<SearchResults | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [open, setOpen] = useState(false); // выпадающий список
  const [expanded, setExpanded] = useState(false); // на телефоне поле раскрывается по кнопке
  const rootRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const q = query.trim();

  useEffect(() => {
    if (q.length < MIN_QUERY) {
      setResults(null);
      setLoading(false);
      setError(null);
      return;
    }
    setLoading(true);
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      searchAll(q, controller.signal)
        .then((r) => {
          setResults(r);
          setError(null);
          setLoading(false);
        })
        .catch((e: unknown) => {
          if (controller.signal.aborted) return;
          setError(errorMessage(e));
          setLoading(false);
        });
    }, 300);
    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [q]);

  // кэш названий чатов принадлежит пользователю: при выходе его не оставляем
  useEffect(() => resetChatIndex, []);

  // клик мимо и Esc закрывают список
  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) setOpen(false);
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        setOpen(false);
        setExpanded(false);
      }
    };
    document.addEventListener("mousedown", onDown);
    window.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDown);
      window.removeEventListener("keydown", onKey);
    };
  }, [open]);

  const go = (target: NavTarget) => {
    setOpen(false);
    setExpanded(false);
    onNavigate(target);
  };

  const total = results ? results.tasks.length + results.notes.length + results.projects.length + results.chats.length : 0;
  const showPanel = open && q.length >= MIN_QUERY;

  return (
    <div className={expanded ? "sr sr-expanded" : "sr"} ref={rootRef}>
      <button
        type="button"
        className="sr-toggle"
        aria-label="Поиск"
        aria-expanded={expanded}
        onClick={() => {
          setExpanded((v) => !v);
          setOpen(true);
          window.setTimeout(() => inputRef.current?.focus(), 0);
        }}
      >
        🔍
      </button>

      <div className="sr-field">
        <input
          ref={inputRef}
          type="search"
          value={query}
          maxLength={100}
          placeholder="Поиск: задачи, записи, чаты"
          aria-label="Поиск по задачам, записям и чатам"
          onFocus={() => setOpen(true)}
          onChange={(e) => {
            setQuery(e.target.value);
            setOpen(true);
          }}
        />

        {showPanel && (
          <div className="sr-panel" role="listbox">
            {loading && !results && <p className="sr-muted">Ищу…</p>}
            {error && <p className="sr-error">{error}</p>}
            {results && !error && total === 0 && !loading && <p className="sr-muted">Ничего не найдено</p>}

            {results && results.tasks.length > 0 && (
              <Group title="Задачи">
                {results.tasks.map((t) => (
                  <Item key={t.id} onClick={() => go({ kind: "todo", id: t.id, projectId: t.projectId })} done={t.isDone}>
                    {t.title}
                  </Item>
                ))}
              </Group>
            )}
            {results && results.projects.length > 0 && (
              <Group title="Проекты">
                {results.projects.map((p) => (
                  <Item key={p.id} onClick={() => go({ kind: "project", id: p.id })}>
                    {p.name}
                  </Item>
                ))}
              </Group>
            )}
            {results && results.notes.length > 0 && (
              <Group title="Notes">
                {results.notes.map((n) => (
                  <Item key={n.id} onClick={() => go({ kind: "note", id: n.id })} hint={n.preview}>
                    {n.title} <span className={`nt-tag nt-tag-${n.tag}`}>{n.tag}</span>
                  </Item>
                ))}
              </Group>
            )}
            {results && results.chats.length > 0 && (
              <Group title="Чаты (по названию)">
                {results.chats.map((c) => (
                  <Item key={c.id} onClick={() => go({ kind: "chat", id: c.id, modelId: c.modelId })} hint={c.modelName}>
                    {c.title}
                  </Item>
                ))}
              </Group>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function Group({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="sr-group">
      <div className="sr-group-title">{title}</div>
      {children}
    </div>
  );
}

function Item({ children, onClick, hint, done }: { children: ReactNode; onClick: () => void; hint?: string; done?: boolean }) {
  return (
    <button type="button" role="option" aria-selected={false} className={done ? "sr-item done" : "sr-item"} onClick={onClick}>
      <span className="sr-item-title">{children}</span>
      {hint && <span className="sr-item-hint">{hint}</span>}
    </button>
  );
}
