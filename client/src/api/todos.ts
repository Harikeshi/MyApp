import { deleteJson, getJson, patchJson, postJson, putJson } from "./http";

// Типы повторяют контракт сервера (MyApp.Application.Todos, MyApp.Application.Common.PagedResult).
export type Priority = "low" | "normal" | "high";

export interface Todo {
  id: number;
  title: string;
  isDone: boolean;
  createdAt: string;
  dueAt: string; // ISO 8601, UTC
  priority: Priority;
  projectId: number | null;
  ownerId: string;
  ownerName: string;
  assigneeId: string | null;
  assigneeName: string | null;
  /** Может ли текущий пользователь менять и удалять задачу (автор или владелец проекта). */
  canManage: boolean;
}

export interface TodoPage {
  items: Todo[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface TodoStats {
  total: number;
  done: number;
  open: number;
  overdue: number;
  highPriorityOpen: number;
  normalPriorityOpen: number;
  lowPriorityOpen: number;
}

/** Поля, которые пользователь задаёт при создании и правке задачи. */
export interface TodoInput {
  title: string;
  priority: Priority;
  /** Срок как ISO-строка; null при создании — сервер поставит текущее время. */
  dueAt: string | null;
  assigneeId: string | null;
}

/** Начало сегодняшнего дня пользователя (ISO, UTC): «просрочено» считается по местному дню, а не по UTC сервера. */
export const todayStart = () => {
  const d = new Date();
  d.setHours(0, 0, 0, 0);
  return d.toISOString();
};

export const fetchStats = (signal?: AbortSignal) =>
  getJson<TodoStats>(`/todos/stats?todayStart=${encodeURIComponent(todayStart())}`, signal);

/** projectId = null — все задачи, которые видит пользователь. */
export const fetchTodos = (projectId: number | null, page: number, pageSize: number, signal?: AbortSignal) =>
  getJson<TodoPage>(
    `/todos?page=${page}&pageSize=${pageSize}${projectId === null ? "" : `&projectId=${projectId}`}`,
    signal,
  );

export const createTodo = (input: TodoInput & { projectId: number | null }) =>
  postJson<Todo>("/todos", {
    title: input.title,
    priority: input.priority,
    dueAt: input.dueAt ?? undefined,
    assigneeId: input.assigneeId ?? undefined,
    projectId: input.projectId ?? undefined,
  });

export const updateTodo = (id: number, input: TodoInput) =>
  putJson<Todo>(`/todos/${id}`, {
    title: input.title,
    priority: input.priority,
    dueAt: input.dueAt,
    assigneeId: input.assigneeId,
  });

export const setTodoDone = (id: number, isDone: boolean) => patchJson<Todo>(`/todos/${id}/done`, { isDone });

export const removeTodo = (id: number) => deleteJson(`/todos/${id}`);
