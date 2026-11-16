import { useCallback, useEffect, useRef, useState } from "react";
import { fetchStats, type TodoStats } from "../../api/todos";

/** Загружает статистику каждый раз, когда пользователь открывает вкладку «Статистика» (active становится true). */
export function useStats(active: boolean) {
  const [stats, setStats] = useState<TodoStats | null>(null);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  const load = useCallback(async () => {
    abortRef.current?.abort();                 // не даём старому ответу перезаписать новый
    const controller = new AbortController();
    abortRef.current = controller;

    try {
      const data = await fetchStats(controller.signal);
      setStats(data);
      setError(null);
    } catch (e) {
      if (controller.signal.aborted) return;
      setError(e instanceof Error ? e.message : "Нет связи с сервером");
    }
  }, []);

  useEffect(() => {
    if (active) void load();
  }, [active, load]);

  useEffect(() => () => abortRef.current?.abort(), []);

  return { stats, error };
}
