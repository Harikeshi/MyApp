import { useEffect, useState, type FormEvent } from "react";
import Modal from "../../components/Modal";
import { errorMessage } from "../../api/http";
import { searchUsers, type Project, type ProjectInput, type UserRef } from "../../api/projects";

interface Props {
  /** Не задан — создание нового проекта. */
  project?: Project;
  onSave: (input: ProjectInput) => Promise<void>;
  onClose: () => void;
}

/** Новый проект или правка существующего: название и ответственные (ищутся по имени или точной почте). */
export default function ProjectDialog({ project, onSave, onClose }: Props) {
  const [name, setName] = useState(project?.name ?? "");
  const [members, setMembers] = useState<UserRef[]>(project?.members ?? []);
  const [query, setQuery] = useState("");
  const [found, setFound] = useState<UserRef[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // поиск пользователей с небольшой задержкой, чтобы не слать запрос на каждую букву
  useEffect(() => {
    const q = query.trim();
    if (q.length < 2) {
      setFound([]);
      return;
    }
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      searchUsers(q, controller.signal)
        .then((list) => setFound(list))
        .catch(() => {
          if (!controller.signal.aborted) setFound([]);
        });
    }, 300);
    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [query]);

  const available = found.filter((u) => !members.some((m) => m.id === u.id));
  const canSave = name.trim() !== "" && !busy;

  const add = (u: UserRef) => {
    setMembers((prev) => [...prev, u]);
    setQuery("");
    setFound([]);
  };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!canSave) return;
    setBusy(true);
    setError(null);
    try {
      await onSave({ name: name.trim(), memberIds: members.map((m) => m.id) });
      onClose();
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  };

  return (
    <Modal
      title={project ? "Изменить проект" : "Новый проект"}
      onClose={onClose}
      footer={
        <>
          <button type="button" className="btn" onClick={onClose}>
            Отмена
          </button>
          <button type="submit" form="project-form" className="btn btn-primary" disabled={!canSave}>
            {busy ? "Сохраняю…" : project ? "Сохранить" : "Создать"}
          </button>
        </>
      }
    >
      <form id="project-form" onSubmit={submit}>
        <label className="field">
          Название
          <input type="text" value={name} maxLength={100} autoFocus onChange={(e) => setName(e.target.value)} />
        </label>

        <div className="field">
          Ответственные
          {members.length > 0 && (
            <ul className="pj-chips">
              {members.map((m) => (
                <li key={m.id} className="pj-chip">
                  {m.name}
                  <button
                    type="button"
                    aria-label={`Убрать ${m.name}`}
                    onClick={() => setMembers((prev) => prev.filter((x) => x.id !== m.id))}
                  >
                    ✕
                  </button>
                </li>
              ))}
            </ul>
          )}
          <input
            type="text"
            value={query}
            placeholder="Имя или точная почта пользователя"
            onChange={(e) => setQuery(e.target.value)}
          />
          {available.length > 0 && (
            <ul className="pj-found">
              {available.map((u) => (
                <li key={u.id}>
                  <button type="button" onClick={() => add(u)}>
                    + {u.name}
                  </button>
                </li>
              ))}
            </ul>
          )}
          {query.trim().length >= 2 && found.length === 0 && <span className="pj-hint">Никого не нашли</span>}
        </div>

        {error && (
          <p className="modal-error" role="alert">
            {error}
          </p>
        )}
      </form>
    </Modal>
  );
}
