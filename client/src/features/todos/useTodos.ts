import { useCallback, useEffect, useRef, useState } from "react";
import { PAGE_SIZE } from "../../config";
import { errorMessage } from "../../api/http";
import {
  createTodo,
  fetchTodos,
  removeTodo,
  setTodoDone,
  updateTodo,
  type Todo,
  type TodoInput,
  type TodoPage,
} from "../../api/todos";

const MAX_LOCATE_PAGES = 10;

/** Состояние вкладки «Задачи»: текущая страница списка (всех задач или одного проекта) и действия над задачами. */
export function useTodos(projectId: number | null) {
  // projectId хранится вместе с данными: после смены проекта чужой список не показывается ни на миг
  const [data, setData] = useState<{ projectId: number | null; page: TodoPage } | null>(null);
  const [error, setError] = useState<string | null>(null);

  const projectRef = useRef(projectId);
  projectRef.current = projectId;
  const pageRef = useRef(1); // актуальная страница для действий (замыкания видят старый state)
  const abortRef = useRef<AbortController | null>(null);

  const load = useCallback(async function run(page: number): Promise<void> {
    abortRef.current?.abort(); // старый ответ не должен перезаписать новый
    const controller = new AbortController();
    abortRef.current = controller;
    const pid = projectRef.current;

    try {
      const result = await fetchTodos(pid, page, PAGE_SIZE, controller.signal);

      // Страница опустела (например, удалили последнюю задачу на последней странице): переходим назад.
      const lastPage = Math.max(1, result.totalPages);
      if (result.items.length === 0 && page > lastPage) return run(lastPage);

      pageRef.current = page;
      setData({ projectId: pid, page: result });
      setError(null);
    } catch (e) {
      if (controller.signal.aborted) return;
      setError(`Не удалось загрузить задачи: ${errorMessage(e)}`);
    }
  }, []);

  const reload = useCallback(() => load(pageRef.current), [load]);

  useEffect(() => {
    void load(1);
    return () => abortRef.current?.abort();
  }, [projectId, load]);

  /** Ищет страницу, на которой лежит задача (переход из поиска), и показывает её. false — не нашли. */
  const locate = useCallback(async (id: number): Promise<boolean> => {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;
    const pid = projectRef.current;

    try {
      for (let p = 1; p <= MAX_LOCATE_PAGES; p++) {
        const result = await fetchTodos(pid, p, PAGE_SIZE, controller.signal);
        if (result.items.some((t) => t.id === id)) {
          pageRef.current = p;
          setData({ projectId: pid, page: result });
          setError(null);
          return true;
        }
        if (p >= result.totalPages) break;
      }
    } catch (e) {
      if (!controller.signal.aborted) setError(errorMessage(e));
    }
    return false;
  }, []);

  /** Новая задача может оказаться на любой странице (порядок зависит от приоритета и срока), поэтому показываем с начала. Ошибку сервера бросает. */
  const add = useCallback(
    async (input: TodoInput & { projectId: number | null }): Promise<void> => {
      await createTodo(input);
      await load(1);
    },
    [load],
  );

  /** Сохраняет правку; ошибку сервера бросает (покажет окно правки). */
  const edit = useCallback(
    async (todo: Todo, input: TodoInput): Promise<void> => {
      await updateTodo(todo.id, input);
      await reload();
    },
    [reload],
  );

  const toggle = useCallback(
    async (todo: Todo, isDone: boolean) => {
      // галочка меняется сразу, не дожидаясь сервера
      setData((d) => d && { ...d, page: { ...d.page, items: d.page.items.map((t) => (t.id === todo.id ? { ...t, isDone } : t)) } });
      try {
        await setTodoDone(todo.id, isDone);
      } catch (e) {
        setError(errorMessage(e));
      }
      await reload(); // порядок зависит от «выполнено»: берём актуальный список
    },
    [reload],
  );

  const remove = useCallback(
    async (todo: Todo) => {
      try {
        await removeTodo(todo.id);
      } catch (e) {
        setError(errorMessage(e));
      }
      await reload();
    },
    [reload],
  );

  const goTo = useCallback((page: number) => load(page), [load]);

  const current = data && data.projectId === projectId ? data.page : null;

  return {
    items: current?.items ?? [],
    page: current?.page ?? 1,
    totalPages: current?.totalPages ?? 1,
    loaded: current !== null,
    error,
    add,
    edit,
    toggle,
    remove,
    goTo,
    locate,
    reload,
  };
}
