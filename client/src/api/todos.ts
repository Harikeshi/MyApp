import { getJson } from "./http";

// Типы повторяют контракт сервера (MyApp.Application.Todos.TodoStatsResponse).
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
const localDate = () => new Date().toLocaleDateString("sv-SE");

export const fetchStats = (signal?: AbortSignal) =>
  getJson<TodoStats>(`/todos/stats?today=${localDate()}`, signal);
