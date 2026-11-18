import { useEffect, useRef, useState } from "react";
import { chatsApi } from "../../api/chats";
import { errorMessage } from "../../api/http";
import type { TokenStats } from "../../types/chats";

/** Расход токенов за последние `days` дней. Грузится при каждом открытии вкладки и при смене периода. */
export function useTokenStats(active: boolean, days: number) {
  const [data, setData] = useState<TokenStats | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    if (!active) return;

    abortRef.current?.abort(); // старый ответ не должен перезаписать новый
    const controller = new AbortController();
    abortRef.current = controller;

    setLoading(true);
    chatsApi
      .getTokenStats(days, controller.signal)
      .then((result) => {
        setData(result);
        setError(null);
      })
      .catch((e: unknown) => {
        if (!controller.signal.aborted) setError(errorMessage(e));
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => controller.abort();
  }, [active, days]);

  return { data, error, loading };
}
