import { useState, type FormEvent } from "react";
import Modal from "../../components/Modal";
import { errorMessage } from "../../api/http";
import type { Project, UserRef } from "../../api/projects";
import type { Priority, Todo, TodoInput } from "../../api/todos";
import { fromLocalInput, toLocalInput } from "./dates";

/** Кого можно назначить исполнителем: себя и участников проекта (владелец тоже участник). */
export function assigneeOptions(me: UserRef, project: Project | undefined, current?: UserRef | null): UserRef[] {
  const list: UserRef[] = [{ id: me.id, name: "Я" }];
  if (project) list.push({ id: project.ownerId, name: project.ownerName }, ...project.members);
  if (current) list.push(current);
  const seen = new Set<string>();
  return list.filter((u) => u.id && !seen.has(u.id) && seen.add(u.id));
}

interface Props {
  todo: Todo;
  project: Project | undefined;
  me: UserRef;
  onSave: (input: TodoInput) => Promise<void>;
  onClose: () => void;
}

/** Правка задачи: текст, исполнитель, срок (дата и время) и приоритет. */
export default function TodoDialog({ todo, project, me, onSave, onClose }: Props) {
  const [title, setTitle] = useState(todo.title);
  const [due, setDue] = useState(toLocalInput(todo.dueAt));
  const [priority, setPriority] = useState<Priority>(todo.priority);
  const [assignee, setAssignee] = useState(todo.assigneeId ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const current = todo.assigneeId ? { id: todo.assigneeId, name: todo.assigneeName ?? "Исполнитель" } : null;
  const options = assigneeOptions(me, project, current);
  const canSave = title.trim() !== "" && due !== "" && !busy;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!canSave) return;
    setBusy(true);
    setError(null);
    try {
      await onSave({ title: title.trim(), dueAt: fromLocalInput(due), priority, assigneeId: assignee || null });
      onClose();
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  };

  return (
    <Modal
      title="Изменить задачу"
      onClose={onClose}
      footer={
        <>
          <button type="button" className="btn" onClick={onClose}>
            Отмена
          </button>
          <button type="submit" form="todo-edit-form" className="btn btn-primary" disabled={!canSave}>
            {busy ? "Сохраняю…" : "Сохранить"}
          </button>
        </>
      }
    >
      <form id="todo-edit-form" onSubmit={submit}>
        <label className="field">
          Текст
          <input type="text" value={title} maxLength={200} autoFocus onChange={(e) => setTitle(e.target.value)} />
        </label>
        <label className="field">
          Исполнитель
          <select value={assignee} onChange={(e) => setAssignee(e.target.value)}>
            <option value="">Без исполнителя</option>
            {options.map((u) => (
              <option key={u.id} value={u.id}>
                {u.name}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          Срок
          <input type="datetime-local" value={due} onChange={(e) => setDue(e.target.value)} />
        </label>
        <label className="field">
          Приоритет
          <select value={priority} onChange={(e) => setPriority(e.target.value as Priority)}>
            <option value="low">Низкий</option>
            <option value="normal">Обычный</option>
            <option value="high">Высокий</option>
          </select>
        </label>
        {error && (
          <p className="modal-error" role="alert">
            {error}
          </p>
        )}
      </form>
    </Modal>
  );
}
