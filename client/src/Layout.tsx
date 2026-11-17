import { useState } from "react";
import { useAuth } from "./features/auth/AuthContext";
import Chats from "./features/chats/Chats";
import Settings from "./features/settings/Settings";
import Stats from "./features/stats/Stats";
import Todos from "./features/todos/Todos";

type Tab = "todos" | "stats" | "chats" | "settings";

const TABS: { id: Exclude<Tab, "settings">; label: string }[] = [
  { id: "todos", label: "Задачи" },
  { id: "stats", label: "Статистика" },
  { id: "chats", label: "Чаты" },
];

/**
 * Шапка с вкладками и сами вкладки. Все вкладки смонтированы сразу и просто скрываются:
 * так в «Чатах» не пропадают открытый чат и неотправленный текст при переключении.
 * «Настройки» открываются кликом по имени пользователя.
 */
export default function Layout() {
  const { user, logout } = useAuth();
  const [tab, setTab] = useState<Tab>("todos");

  return (
    <div className="app">
      <header className="header">
        <span className="brand">Мои задачи</span>
        <nav className="tabs">
          {TABS.map((t) => (
            <button key={t.id} type="button" className={tab === t.id ? "tab active" : "tab"} onClick={() => setTab(t.id)}>
              {t.label}
            </button>
          ))}
        </nav>
        <div className="user">
          <button
            type="button"
            className={tab === "settings" ? "user-name active" : "user-name"}
            title={user?.email}
            onClick={() => setTab("settings")}
          >
            {user?.name}
          </button>
          <button type="button" onClick={() => void logout()}>
            Выйти
          </button>
        </div>
      </header>

      {/* вкладке «Чаты» нужна ширина: список чатов слева, переписка справа */}
      <main className={tab === "chats" ? "content wide chats" : "content"}>
        <section className="panel" hidden={tab !== "todos"}>
          <Todos />
        </section>
        <section className="panel" hidden={tab !== "stats"}>
          <Stats active={tab === "stats"} />
        </section>
        <section hidden={tab !== "chats"}>
          <Chats active={tab === "chats"} />
        </section>
        <section className="panel" hidden={tab !== "settings"}>
          <Settings />
        </section>
      </main>
    </div>
  );
}
