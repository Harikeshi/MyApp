import { useCallback, useEffect, useRef, useState } from "react";
import { errorMessage } from "../../api/http";
import {
  createNote,
  fetchNote,
  fetchNotes,
  removeNote,
  updateNote,
  type Note,
  type NoteInput,
  type NoteListItem,
  type NoteTag,
} from "../../api/notes";
import ChatActionsMenu, { type MenuItem } from "../chats/ChatActionsMenu";
import ConfirmDialog from "../chats/ConfirmDialog";
import NoteEditor from "./NoteEditor";
import { sanitizeHtml } from "./sanitize";
import "./notes.css";

/** Переход из поиска: открыть запись. nonce нужен, чтобы тот же переход сработал повторно. */
export interface NoteFocus {
  id: number;
  nonce: number;
}

type Mode = "none" | "view" | "edit" | "create";
type TagFilter = "all" | NoteTag;

const MOBILE_QUERY = "(max-width: 720px)";
const isMobile = () => window.matchMedia(MOBILE_QUERY).matches;

const dateFormat = new Intl.DateTimeFormat("ru-RU", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
const formatDate = (iso: string) => dateFormat.format(new Date(iso));

/**
 * Вкладка Notes: слева «Добавить запись» и список, справа форма. Выбранная запись показывается без правки,
 * «Редактировать» (или «Добавить») открывает форму; в базу запись попадает только по кнопке «Сохранить».
 */
export default function NotesTab({ active, focus }: { active: boolean; focus: NoteFocus | null }) {
  const [items, setItems] = useState<NoteListItem[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [mode, setMode] = useState<Mode>("none");
  const [current, setCurrent] = useState<Note | null>(null);
  const [filter, setFilter] = useState<TagFilter>("all");
  const [sidebarOpen, setSidebarOpen] = useState(() => !isMobile());

  const [menu, setMenu] = useState<{ anchor: DOMRect; items: MenuItem[] } | null>(null);
  const [toDelete, setToDelete] = useState<{ id: number; title: string } | null>(null);
  const [guard, setGuard] = useState<(() => void) | null>(null); // действие, ждущее подтверждения «потерять изменения»

  const dirtyRef = useRef(false);
  const onDirtyChange = useCallback((dirty: boolean) => {
    dirtyRef.current = dirty;
  }, []);
  const loadSeq = useRef(0);

  const loadList = useCallback(async () => {
    try {
      setItems(await fetchNotes());
      setError(null);
    } catch (e) {
      setError(`Не удалось загрузить записи: ${errorMessage(e)}`);
    } finally {
      setLoaded(true);
    }
  }, []);

  useEffect(() => {
    if (active) void loadList();
  }, [active, loadList]);

  /** Выполняет действие, а если в форме есть несохранённый текст, сначала спрашивает. */
  const guarded = (action: () => void) => {
    if (dirtyRef.current) setGuard(() => action);
    else action();
  };

  const open = useCallback(async (id: number) => {
    const seq = ++loadSeq.current;
    try {
      const note = await fetchNote(id);
      if (seq !== loadSeq.current) return; // пока грузилась эта запись, выбрали другую
      dirtyRef.current = false;
      setCurrent(note);
      setMode("view");
      setError(null);
    } catch (e) {
      if (seq === loadSeq.current) setError(errorMessage(e));
      void loadList(); // например, запись уже удалена в другой вкладке
    }
  }, [loadList]);

  useEffect(() => {
    if (!focus) return;
    guarded(() => void open(focus.id));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [focus]);

  const collapseOnMobile = () => {
    if (isMobile()) setSidebarOpen(false);
  };

  const startCreate = () => {
    loadSeq.current++;
    dirtyRef.current = false;
    setCurrent(null);
    setMode("create");
    collapseOnMobile();
  };

  const save = async (input: NoteInput) => {
    const saved = current && mode === "edit" ? await updateNote(current.id, input) : await createNote(input);
    dirtyRef.current = false;
    setCurrent(saved);
    setMode("view");
    void loadList();
  };

  const edit = async (id: number) => {
    if (current?.id === id) {
      setMode("edit");
    } else {
      await open(id);
      setMode("edit");
    }
    collapseOnMobile();
  };

  const remove = async (id: number) => {
    try {
      await removeNote(id);
    } catch (e) {
      setError(errorMessage(e));
    }
    if (current?.id === id) {
      loadSeq.current++;
      dirtyRef.current = false;
      setCurrent(null);
      setMode("none");
    }
    void loadList();
  };

  const shown = filter === "all" ? items : items.filter((n) => n.tag === filter);

  const openMenu = (n: NoteListItem, anchor: DOMRect) =>
    setMenu({
      anchor,
      items: [
        { label: "Редактировать", icon: "✏️", onClick: () => guarded(() => void edit(n.id)) },
        { label: "Удалить", icon: "🗑", danger: true, onClick: () => setToDelete({ id: n.id, title: n.title }) },
      ],
    });

  return (
    <div className="nt" data-side={sidebarOpen ? "open" : "closed"}>
      <aside className="nt-side">
        <div className="nt-side-head">
          <button type="button" className="btn btn-primary nt-add" onClick={() => guarded(startCreate)}>
            + Добавить запись
          </button>
          <div className="nt-filter" role="group" aria-label="Тег">
            {(["all", "task", "faq"] as const).map((f) => (
              <button key={f} type="button" className={filter === f ? "on" : ""} aria-pressed={filter === f} onClick={() => setFilter(f)}>
                {f === "all" ? "Все" : f}
              </button>
            ))}
          </div>
        </div>

        <ul className="nt-list">
          {shown.map((n) => (
            <li key={n.id}>
              <div className={current?.id === n.id && mode !== "create" ? "nt-row active" : "nt-row"}>
                <button
                  type="button"
                  className="nt-item"
                  onClick={() =>
                    guarded(() => {
                      void open(n.id);
                      collapseOnMobile();
                    })
                  }
                >
                  <span className="nt-item-top">
                    <span className="nt-item-title">{n.title}</span>
                    <span className={`nt-tag nt-tag-${n.tag}`}>{n.tag}</span>
                  </span>
                  {n.preview && <span className="nt-item-prev">{n.preview}</span>}
                  <span className="nt-item-date">{formatDate(n.updatedAt)}</span>
                </button>
                <button
                  type="button"
                  className="more-btn"
                  aria-label={`Действия с записью ${n.title}`}
                  aria-haspopup="menu"
                  onClick={(e) => openMenu(n, e.currentTarget.getBoundingClientRect())}
                >
                  ⋯
                </button>
              </div>
            </li>
          ))}
        </ul>
        {loaded && shown.length === 0 && <p className="nt-muted">Записей пока нет</p>}
      </aside>

      <section className="nt-main">
        <div className="nt-bar">
          <button type="button" className="btn nt-toggle" onClick={() => setSidebarOpen((o) => !o)}>
            ☰ Записи
          </button>
        </div>

        {error && (
          <p className="nt-error" role="alert">
            {error}
          </p>
        )}

        {mode === "none" && <p className="nt-muted nt-placeholder">Выберите запись слева или добавьте новую.</p>}

        {mode === "view" && current && (
          <article className="nt-view">
            <header className="nt-view-head">
              <div className="nt-view-info">
                <h2>{current.title}</h2>
                <span className={`nt-tag nt-tag-${current.tag}`}>{current.tag}</span>
                <span className="nt-item-date">изменено {formatDate(current.updatedAt)}</span>
              </div>
              <div className="nt-view-actions">
                <button type="button" className="btn" onClick={() => setMode("edit")}>
                  Редактировать
                </button>
                <button type="button" className="btn" onClick={() => setToDelete({ id: current.id, title: current.title })}>
                  Удалить
                </button>
              </div>
            </header>
            {/* HTML очищен по белому списку: без скриптов и чужих картинок */}
            <div className="nt-content" dangerouslySetInnerHTML={{ __html: sanitizeHtml(current.content) }} />
          </article>
        )}

        {(mode === "create" || mode === "edit") && (
          <NoteEditor
            key={mode === "edit" ? `edit-${current?.id}` : "create"}
            initial={mode === "edit" ? current : null}
            onSave={save}
            onDirtyChange={onDirtyChange}
            onCancel={() => {
              dirtyRef.current = false;
              setMode(mode === "edit" && current ? "view" : "none");
            }}
          />
        )}
      </section>

      {menu && <ChatActionsMenu anchor={menu.anchor} items={menu.items} onClose={() => setMenu(null)} />}

      {toDelete && (
        <ConfirmDialog
          title="Удалить запись?"
          message={`«${toDelete.title}» будет удалена вместе с картинками.`}
          confirmLabel="Удалить"
          onCancel={() => setToDelete(null)}
          onConfirm={() => {
            const target = toDelete;
            setToDelete(null);
            void remove(target.id);
          }}
        />
      )}

      {guard && (
        <ConfirmDialog
          title="Уйти без сохранения?"
          message="В форме есть несохранённые изменения, они будут потеряны."
          confirmLabel="Уйти"
          onCancel={() => setGuard(null)}
          onConfirm={() => {
            const action = guard;
            setGuard(null);
            dirtyRef.current = false;
            action();
          }}
        />
      )}
    </div>
  );
}
