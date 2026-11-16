import { useCallback, useEffect, useRef, useState } from "react";
import { PAGE_SIZE } from "../../config";
import { errorMessage } from "../../api/http";
import { createTodo, fetchTodos, removeTodo, updateTodo, type Priority, type Todo, type TodoPage } from "../../api/todos";

/** Состояние вкладки «Задачи»: текущая страница списка и действия над задачами. */
export function useTodos() {
  const [data, setData] = useState<TodoPage | null>(null);
  const [error, setError] = useState<string | null>(null);

  const pageRef = useRef(1);                       // актуальная страница для действий (замыкания видят старый state)
  const abortRef = useRef<AbortController | null>(null);

  const load = useCallback(async function run(page: number): Promise<void> {
    abortRef.current?.abort();                     // старый ответ не должен перезаписать новый
    const controller = new AbortController();
    abortRef.current = controller;

    try {
      const result = await fetchTodos(page, PAGE_SIZE, controller.signal);

      // Страница опустела (например, удалили последнюю задачу на последней странице): переходим назад.
      const lastPage = Math.max(1, result.totalPages);
      if (result.items.length === 0 && page > lastPage) return run(lastPage);

      pageRef.current = page;
      setData(result);
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
  }, [load]);

  /** true, если задача добавлена. Новая задача может оказаться на любой странице (порядок зависит от приоритета и срока), поэтому показываем с начала. */
  const add = useCallback(
    async (input: { title: string; priority: Priority; dueDate: string | null }): Promise<boolean> => {
      try {
        await createTodo(input);
        await load(1);
        return true;
      } catch (e) {
        setError(errorMessage(e));
        return false;
      }
    },
    [load],
  );

  const toggle = useCallback(
    async (todo: Todo, isDone: boolean) => {
      // галочка меняется сразу, не дожидаясь сервера
      setData((d) => d && { ...d, items: d.items.map((t) => (t.id === todo.id ? { ...t, isDone } : t)) });
      try {
        await updateTodo(todo, { isDone });
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

  return {
    items: data?.items ?? [],
    page: data?.page ?? 1,
    totalPages: data?.totalPages ?? 1,
    loaded: data !== null,
    error,
    add,
    toggle,
    remove,
    goTo,
  };
}
