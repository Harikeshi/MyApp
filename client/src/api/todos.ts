import { deleteJson, getJson, postJson, putJson } from "./http";

// Типы повторяют контракт сервера (MyApp.Application.Todos, MyApp.Application.Common.PagedResult).
export type Priority = "low" | "normal" | "high";

export interface Todo {
  id: number;
  title: string;
  isDone: boolean;
  createdAt: string;
  dueDate: string | null; // YYYY-MM-DD
  priority: Priority;
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

// Локальная дата пользователя YYYY-MM-DD: «просрочено» считается по ней, а не по UTC сервера.
export const localDate = () => new Date().toLocaleDateString("sv-SE");

export const fetchStats = (signal?: AbortSignal) =>
  getJson<TodoStats>(`/todos/stats?today=${localDate()}`, signal);

export const fetchTodos = (page: number, pageSize: number, signal?: AbortSignal) =>
  getJson<TodoPage>(`/todos?page=${page}&pageSize=${pageSize}`, signal);

export const createTodo = (input: { title: string; priority: Priority; dueDate: string | null }) =>
  postJson<Todo>("/todos", input.dueDate ? input : { title: input.title, priority: input.priority });

export const updateTodo = (todo: Todo, changes: Partial<Pick<Todo, "isDone">>) =>
  putJson(`/todos/${todo.id}`, {
    title: todo.title,
    isDone: todo.isDone,
    dueDate: todo.dueDate,
    priority: todo.priority,
    ...changes,
  });

export const removeTodo = (id: number) => deleteJson(`/todos/${id}`);
