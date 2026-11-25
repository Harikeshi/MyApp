import { useEffect, useState } from "react";
import { useAuth } from "./features/auth/AuthContext";
import Chats from "./features/chats/Chats";
import Settings from "./features/settings/Settings";
import Stats from "./features/stats/Stats";
import Todos from "./features/todos/Todos";
import News from "./features/news/NewsTab";
import Notes, { type NoteFocus } from "./features/notes/NotesTab";
import SearchBox, { type NavTarget } from "./features/search/SearchBox";
import { type TodoFocus } from "./features/todos/Todos";
import type { ChatFocus } from "./features/chats/Chats";

type Tab = "todos" | "notes" | "stats" | "chats" | "news" | "settings";

const TABS: { id: Exclude<Tab, "settings">; label: string }[] = [
  { id: "todos", label: "Задачи" },
  { id: "notes", label: "Заметки" },
  { id: "stats", label: "Статистика" },
  { id: "chats", label: "Чаты" },
  { id: "news", label: "Новости" },
];

const LABELS: Record<Tab, string> = { todos: "Задачи", notes: "Заметки", stats: "Статистика", chats: "Чаты", news: "Новости", settings: "Настройки" };

/** Две первые буквы имени (если имени нет — почты): «Иван» -> «ИВ». */
function initials(name?: string, email?: string): string {
  const letters = (text?: string) =>
    Array.from(text ?? "")
      .filter((ch) => /[\p{L}\p{N}]/u.test(ch))
      .slice(0, 2)
      .join("");
  return (letters(name) || letters(email) || "?").toUpperCase();
}

/** Круглый аватар (кнопка, открывает настройки) и имя рядом: имя — просто текст. */
function UserChip({ name, email, active, onOpen }: { name?: string; email?: string; active: boolean; onOpen: () => void }) {
  return (
    <div className={active ? "user-chip active" : "user-chip"}>
      <button type="button" className="avatar" aria-label="Настройки" title="Настройки" onClick={onOpen}>
        {initials(name, email)}
      </button>
      <span className="user-chip-name" title={email}>
        {name}
      </span>
    </div>
  );
}

/**
 * Шапка с вкладками и сами вкладки. Все вкладки смонтированы сразу и просто скрываются:
 * так в «Чатах» не пропадают открытый чат и неотправленный текст при переключении.
 * «Настройки» открываются кликом по имени пользователя.
 *
 * На узком экране вкладки, имя и «Выйти» не помещаются в шапку, поэтому шапка такая: «☰» (разворачивает меню с вкладками
 * и «Выйти»), название текущей вкладки и аватар с именем, по аватару открываются настройки (CSS переключает вид по ширине).
 */
export default function Layout() {
  const { user, logout } = useAuth();
  const [tab, setTab] = useState<Tab>("todos");
  const [menuOpen, setMenuOpen] = useState(false);

  // переход из поиска: вкладка получает «фокус» (nonce нужен, чтобы тот же результат сработал снова)
  const [todoFocus, setTodoFocus] = useState<TodoFocus | null>(null);
  const [noteFocus, setNoteFocus] = useState<NoteFocus | null>(null);
  const [chatFocus, setChatFocus] = useState<ChatFocus | null>(null);

  const navigate = (target: NavTarget) => {
    const nonce = Date.now();
    switch (target.kind) {
      case "todo":
        setTodoFocus({ projectId: target.projectId, todoId: target.id, nonce });
        go("todos");
        break;
      case "project":
        setTodoFocus({ projectId: target.id, nonce });
        go("todos");
        break;
      case "note":
        setNoteFocus({ id: target.id, nonce });
        go("notes");
        break;
      case "chat":
        setChatFocus({ modelId: target.modelId, chatId: target.id, nonce });
        go("chats");
        break;
    }
  };

  const go = (next: Tab) => {
    setTab(next);
    setMenuOpen(false);
  };

  // Escape закрывает меню
  useEffect(() => {
    if (!menuOpen) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setMenuOpen(false);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [menuOpen]);

  return (
    <div className="app">
      <header className="header">
        <span className="brand">Мои задачи</span>

        {/* широкий экран */}
        <nav className="tabs">
          {TABS.map((t) => (
            <button key={t.id} type="button" className={tab === t.id ? "tab active" : "tab"} onClick={() => go(t.id)}>
              {t.label}
            </button>
          ))}
        </nav>
        <SearchBox onNavigate={navigate} />
        <div className="user">
          <UserChip name={user?.name} email={user?.email} active={tab === "settings"} onOpen={() => go("settings")} />
          <button type="button" onClick={() => void logout()}>
            Выйти
          </button>
        </div>

        {/* узкий экран: «☰» (меню), название текущей вкладки (не кнопка), аватар (кнопка, ведёт в настройки) и имя (просто текст) */}
        <button
          type="button"
          className="menu-toggle"
          aria-label="Меню"
          aria-expanded={menuOpen}
          aria-controls="app-menu"
          onClick={() => setMenuOpen((open) => !open)}
        >
          {menuOpen ? "✕" : "☰"}
        </button>
        <span className="mobile-title">{LABELS[tab]}</span>
        <div className="mobile-user">
          <UserChip name={user?.name} email={user?.email} active={tab === "settings"} onOpen={() => go("settings")} />
        </div>

        <nav id="app-menu" className="menu" hidden={!menuOpen}>
          {TABS.map((t) => (
            <button key={t.id} type="button" className={tab === t.id ? "menu-item active" : "menu-item"} onClick={() => go(t.id)}>
              {t.label}
            </button>
          ))}
          <div className="menu-sep" role="separator" />
          <button type="button" className="menu-item" onClick={() => void logout()}>
            Выйти
          </button>
        </nav>

        {menuOpen && <div className="menu-backdrop" onClick={() => setMenuOpen(false)} />}
      </header>

      {/* вкладке «Чаты» нужна ширина: список чатов слева, переписка справа */}
      <main className={tab === "chats" ? "content wide chats" : tab === "todos" || tab === "notes" ? "content wide" : "content"}>
        <section className="panel flush" hidden={tab !== "todos"}>
          <Todos focus={todoFocus} />
        </section>
        <section className="panel flush" hidden={tab !== "notes"}>
          <Notes active={tab === "notes"} focus={noteFocus} />
        </section>
        <section className="panel" hidden={tab !== "stats"}>
          <Stats active={tab === "stats"} />
        </section>
        <section hidden={tab !== "chats"}>
          <Chats active={tab === "chats"} focus={chatFocus} />
        </section>
        <section className="panel" hidden={tab !== "news"}>
          <News active={tab === "news"} />
        </section>
        <section className="panel" hidden={tab !== "settings"}>
          <Settings />
        </section>
      </main>
    </div>
  );
}
