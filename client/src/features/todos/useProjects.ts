import { useCallback, useEffect, useState } from "react";
import { errorMessage } from "../../api/http";
import { createProject, fetchProjects, removeProject, updateProject, type Project, type ProjectInput } from "../../api/projects";

const byName = (a: Project, b: Project) => a.name.localeCompare(b.name, "ru");

/** Проекты, в которых пользователь владелец или ответственный. Ошибки создания и правки летят в окно формы. */
export function useProjects() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setProjects((await fetchProjects()).sort(byName));
      setError(null);
    } catch (e) {
      setError(`Не удалось загрузить проекты: ${errorMessage(e)}`);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  /** Бросает ApiError, если сервер отказал (окно формы покажет причину). */
  const add = useCallback(async (input: ProjectInput): Promise<Project> => {
    const created = await createProject(input);
    setProjects((prev) => [...prev, created].sort(byName));
    return created;
  }, []);

  const edit = useCallback(async (id: number, input: ProjectInput): Promise<void> => {
    const updated = await updateProject(id, input);
    setProjects((prev) => prev.map((p) => (p.id === id ? updated : p)).sort(byName));
  }, []);

  const remove = useCallback(async (id: number): Promise<boolean> => {
    try {
      await removeProject(id);
      setProjects((prev) => prev.filter((p) => p.id !== id));
      return true;
    } catch (e) {
      setError(errorMessage(e));
      void load();
      return false;
    }
  }, [load]);

  return { projects, error, reload: load, add, edit, remove };
}
