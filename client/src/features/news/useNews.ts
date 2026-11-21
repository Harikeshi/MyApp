import { useCallback, useEffect, useRef, useState } from "react";
import { errorMessage } from "../../api/http";
import { fetchNews } from "../../api/news";
import type { NewsItem, TopicFilter } from "../../types/news";

/** При повторном открытии вкладки лента обновляется, если загружена давно. */
const REFRESH_AFTER_MS = 5 * 60 * 1000;

/** Лента новостей: загрузка при открытии вкладки, смена темы, «Загрузить ещё» и ручное обновление. */
export function useNews(active: boolean) {
  const [topic, setTopic] = useState<TopicFilter>("all");
  const [items, setItems] = useState<NewsItem[]>([]);
  const [cursor, setCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [updatedAt, setUpdatedAt] = useState<Date | null>(null); // когда лента успешно загружена в последний раз

  const requestId = useRef(0);                              // отбрасываем устаревшие ответы (быстрая смена темы)
  const loadedAt = useRef(0);                               // когда начинали последнюю загрузку (0: ещё не грузили)
  const loadedTopic = useRef<TopicFilter | null>(null);

  const load = useCallback(
    async (reset: boolean, from: string | null) => {
      const id = ++requestId.current;
      loadedAt.current = Date.now();
      loadedTopic.current = topic;
      setLoading(true);
      setError(null);
      try {
        const page = await fetchNews({ topic: topic === "all" ? undefined : topic, cursor: reset ? null : from });
        if (id !== requestId.current) return;
        setItems((prev) => (reset ? page.items : [...prev, ...page.items]));
        setCursor(page.nextCursor);
        setUpdatedAt(new Date());
      } catch (e) {
        if (id === requestId.current) setError(errorMessage(e));
      } finally {
        if (id === requestId.current) setLoading(false);
      }
    },
    [topic],
  );

  // Вкладки в Layout смонтированы всегда, поэтому грузим только когда вкладку открыли.
  useEffect(() => {
    if (!active) return;
    const topicChanged = loadedTopic.current !== topic;
    if (!topicChanged && Date.now() - loadedAt.current < REFRESH_AFTER_MS) return;

    if (topicChanged) {
      setItems([]);
      setCursor(null);
    }
    void load(true, null);
  }, [active, topic, load]);

  const refresh = useCallback(() => load(true, null), [load]);
  const loadMore = useCallback(() => (cursor ? load(false, cursor) : Promise.resolve()), [cursor, load]);

  return { topic, setTopic, items, hasMore: cursor !== null, loading, error, updatedAt, refresh, loadMore };
}
