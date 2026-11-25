import { useEffect, useState, type FormEvent } from "react";
import { errorMessage } from "../../api/http";
import type { Project, UserRef } from "../../api/projects";
import type { Priority, Todo } from "../../api/todos";
import { useAuth } from "../auth/AuthContext";
import ChatActionsMenu, { type MenuItem } from "../chats/ChatActionsMenu";
import ConfirmDialog from "../chats/ConfirmDialog";
import { formatDue, fromLocalInput, startOfTodayMs } from "./dates";
import ProjectDialog from "./ProjectDialog";
import TodoDialog, { assigneeOptions } from "./TodoDialog";
import { useProjects } from "./useProjects";
import { useTodos } from "./useTodos";
import "./todos.css";

const PRIORITY_LABEL: Record<Priority, string> = { low: "низкий", normal: "", high: "высокий" };

/** Переход из поиска: открыть проект (null — все задачи) и показать задачу. nonce нужен, чтобы тот же переход сработал повторно. */
export interface TodoFocus {
  projectId: number | null;
  todoId?: number;
  nonce: number;
}

type ProjectDialogState = { project?: Project } | null;

export default function Todos({ focus }: { focus: TodoFocus | null }) {
  const { user } = useAuth();
  const me: UserRef = { id: user?.id ?? "", name: user?.name ?? "" };

  const pr = useProjects();
  const [selected, setSelected] = useState<number | null>(null); // null — «Все задачи»
  const t = useTodos(selected);

  // форма новой задачи
  const [title, setTitle] = useState("");
  const [due, setDue] = useState("");
  const [priority, setPriority] = useState<Priority>("normal");
  const [assignee, setAssignee] = useState("");
  const [formProject, setFormProject] = useState(""); // проект для новой задачи, когда слева выбрано «Все задачи»
  const [formError, setFormError] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);

  // окна и меню
  const [projectDialog, setProjectDialog] = useState<ProjectDialogState>(null);
  const [editTodo, setEditTodo] = useState<Todo | null>(null);
  const [todoToDelete, setTodoToDelete] = useState<Todo | null>(null);
  const [projectToDelete, setProjectToDelete] = useState<Project | null>(null);
  const [menu, setMenu] = useState<{ anchor: DOMRect; items: MenuItem[] } | null>(null);

  // переход из поиска
  const [pendingTodo, setPendingTodo] = useState<number | null>(null);
  const [highlight, setHighlight] = useState<number | null>(null);

  useEffect(() => {
    if (!focus) return;
    setSelected(focus.projectId);
    setPendingTodo(focus.todoId ?? null);
    setHighlight(null);
    void pr.reload(); // проект мог появиться после последней загрузки
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [focus]);

  useEffect(() => {
    if (pendingTodo === null || !t.loaded) return;
    const id = pendingTodo;
    setPendingTodo(null);
    if (t.items.some((x) => x.id === id)) {
      setHighlight(id);
    } else {
      void t.locate(id).then((found) => setHighlight(found ? id : null));
    }
  }, [pendingTodo, t.loaded, t.items, t]);

  useEffect(() => {
    if (highlight === null) return;
    document.querySelector(`[data-todo="${highlight}"]`)?.scrollIntoView({ block: "center", behavior: "smooth" });
    const timer = window.setTimeout(() => setHighlight(null), 3000);
    return () => window.clearTimeout(timer);
  }, [highlight, t.items]);

  // выбранный проект удалили или потеряли доступ: показываем все задачи
  useEffect(() => {
    if (selected !== null && pr.projects.length > 0 && !pr.projects.some((p) => p.id === selected)) setSelected(null);
  }, [selected, pr.projects]);

  const projectOf = (id: number | null) => (id === null ? undefined : pr.projects.find((p) => p.id === id));
  const newTaskProjectId = selected ?? (formProject ? Number(formProject) : null);
  const newTaskProject = projectOf(newTaskProjectId);
  const assignees = assigneeOptions(me, newTaskProject);

  // исполнитель должен быть участником выбранного проекта
  useEffect(() => {
    if (assignee && !assignees.some((u) => u.id === assignee)) setAssignee("");
  }, [assignee, assignees]);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const text = title.trim();
    if (!text || adding) return;

    setAdding(true);
    setFormError(null);
    try {
      await t.add({
        title: text,
        priority,
        dueAt: fromLocalInput(due), // не выбрано — сервер поставит текущее время
        assigneeId: assignee || null,
        projectId: newTaskProjectId,
      });
      setTitle("");
      setDue("");
      setPriority("normal");
      setAssignee("");
    } catch (err) {
      setFormError(errorMessage(err));
    } finally {
      setAdding(false);
    }
  };

  const openTodoMenu = (todo: Todo, anchor: DOMRect) =>
    setMenu({
      anchor,
      items: [
        { label: "Изменить", icon: "✏️", onClick: () => setEditTodo(todo) },
        { label: "Удалить", icon: "🗑", danger: true, onClick: () => setTodoToDelete(todo) },
      ],
    });

  const openProjectMenu = (project: Project, anchor: DOMRect) =>
    setMenu({
      anchor,
      items: [
        { label: "Изменить", icon: "✏️", onClick: () => setProjectDialog({ project }) },
        { label: "Удалить", icon: "🗑", danger: true, onClick: () => setProjectToDelete(project) },
      ],
    });

  const selectedProject = projectOf(selected);
  const startOfToday = startOfTodayMs();
  const error = t.error ?? pr.error;

  return (
    <div className="td pj">
      <aside className="pj-side">
        <button type="button" className="pj-new" onClick={() => setProjectDialog({})}>
          + Новый проект
        </button>
        <ul className="pj-list">
          <li>
            <div className={selected === null ? "pj-row active" : "pj-row"}>
              <button type="button" className="pj-item" onClick={() => setSelected(null)}>
                <span className="pj-name">Все задачи</span>
              </button>
            </div>
          </li>
          {pr.projects.map((p) => (
            <li key={p.id}>
              <div className={selected === p.id ? "pj-row active" : "pj-row"}>
                <button type="button" className="pj-item" onClick={() => setSelected(p.id)}>
                  <span className="pj-name">{p.name}</span>
                  {!p.isOwner && <span className="pj-meta">владелец: {p.ownerName}</span>}
                </button>
                {p.isOwner && (
                  <button
                    type="button"
                    className="more-btn"
                    aria-label={`Действия с проектом ${p.name}`}
                    aria-haspopup="menu"
                    onClick={(e) => openProjectMenu(p, e.currentTarget.getBoundingClientRect())}
                  >
                    ⋯
                  </button>
                )}
              </div>
            </li>
          ))}
        </ul>
      </aside>

      <section className="pj-main">
        <h2 className="pj-title">{selectedProject ? selectedProject.name : "Все задачи"}</h2>

        <form className="td-form" onSubmit={submit}>
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
            <div className="td-date">
              <input
                type="datetime-local"
                value={due}
                aria-label="Срок"
                title="Срок (если не выбран, будет текущее время)"
                onChange={(e) => setDue(e.target.value)}
              />
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
          </div>
          <div className="td-row">
            {selected === null && (
              <select value={formProject} title="Проект" onChange={(e) => setFormProject(e.target.value)}>
                <option value="">Без проекта</option>
                {pr.projects.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>
            )}
            <select value={assignee} title="Исполнитель" onChange={(e) => setAssignee(e.target.value)}>
              <option value="">Без исполнителя</option>
              {assignees.map((u) => (
                <option key={u.id} value={u.id}>
                  {u.name}
                </option>
              ))}
            </select>
            <button disabled={!title.trim() || adding}>Добавить</button>
          </div>
          {formError && (
            <p className="td-error" role="alert">
              {formError}
            </p>
          )}
        </form>

        {error && (
          <p className="td-error" role="alert">
            {error}
          </p>
        )}
        {!t.loaded && !t.error && <p className="td-muted">Загрузка…</p>}
        {t.loaded && t.items.length === 0 && !t.error && <p className="td-muted">Задач пока нет</p>}

        <ul className="td-list">
          {t.items.map((todo) => {
            const overdue = !todo.isDone && new Date(todo.dueAt).getTime() < startOfToday;
            const className = ["td-item", todo.isDone && "done", todo.priority, highlight === todo.id && "flash"]
              .filter(Boolean)
              .join(" ");
            const projectName = selected === null ? projectOf(todo.projectId)?.name : undefined;

            return (
              <li key={todo.id} className={className} data-todo={todo.id}>
                <input
                  type="checkbox"
                  checked={todo.isDone}
                  aria-label={`Выполнено: ${todo.title}`}
                  onChange={(e) => void t.toggle(todo, e.target.checked)}
                />
                <div className="td-body">
                  <span className="td-title">{todo.title}</span>
                  <span className="td-tags">
                    {PRIORITY_LABEL[todo.priority] && <span className="td-prio">{PRIORITY_LABEL[todo.priority]}</span>}
                    <span className={overdue ? "td-due overdue" : "td-due"}>{formatDue(todo.dueAt)}</span>
                    {todo.assigneeName && <span className="td-who">→ {todo.assigneeName}</span>}
                    {projectName && <span className="td-proj">{projectName}</span>}
                  </span>
                </div>
                {todo.canManage && (
                  <button
                    type="button"
                    className="more-btn"
                    aria-label={`Действия с задачей ${todo.title}`}
                    aria-haspopup="menu"
                    onClick={(e) => openTodoMenu(todo, e.currentTarget.getBoundingClientRect())}
                  >
                    ⋯
                  </button>
                )}
              </li>
            );
          })}
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
      </section>

      {menu && <ChatActionsMenu anchor={menu.anchor} items={menu.items} onClose={() => setMenu(null)} />}

      {projectDialog && (
        <ProjectDialog
          project={projectDialog.project}
          onClose={() => setProjectDialog(null)}
          onSave={async (input) => {
            if (projectDialog.project) await pr.edit(projectDialog.project.id, input);
            else setSelected((await pr.add(input)).id);
          }}
        />
      )}

      {editTodo && (
        <TodoDialog
          todo={editTodo}
          me={me}
          project={projectOf(editTodo.projectId)}
          onClose={() => setEditTodo(null)}
          onSave={(input) => t.edit(editTodo, input)}
        />
      )}

      {todoToDelete && (
        <ConfirmDialog
          title="Удалить задачу?"
          message={`«${todoToDelete.title}» будет удалена.`}
          confirmLabel="Удалить"
          onCancel={() => setTodoToDelete(null)}
          onConfirm={() => {
            const todo = todoToDelete;
            setTodoToDelete(null);
            void t.remove(todo);
          }}
        />
      )}

      {projectToDelete && (
        <ConfirmDialog
          title="Удалить проект?"
          message={`«${projectToDelete.name}»: все задачи этого проекта тоже будут удалены, у всех участников.`}
          confirmLabel="Удалить"
          onCancel={() => setProjectToDelete(null)}
          onConfirm={() => {
            const project = projectToDelete;
            setProjectToDelete(null);
            void pr.remove(project.id).then(() => t.reload());
          }}
        />
      )}
    </div>
  );
}
