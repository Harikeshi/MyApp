import { useCallback, useEffect, useRef, useState } from 'react';
import { newsApi } from './api';
import { errorMessage } from './http';
import NewsCard from './NewsCard';
import type { NewsItem, TopicFilter } from './types';
import './news.css';

const FILTERS: { value: TopicFilter; label: string }[] = [
  { value: 'all', label: 'Все' },
  { value: 'markets', label: 'Рынки' },
  { value: 'crypto', label: 'Крипта' },
];

export default function NewsTab() {
  const [topic, setTopic] = useState<TopicFilter>('all');
  const [items, setItems] = useState<NewsItem[]>([]);
  const [cursor, setCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const requestId = useRef(0); // отбрасываем устаревшие ответы при быстром переключении чипов

  const load = useCallback(
    async (reset: boolean, from: string | null) => {
      const id = ++requestId.current;
      setLoading(true);
      setError(null);
      try {
        const page = await newsApi.list({ topic: topic === 'all' ? undefined : topic, cursor: reset ? null : from });
        if (id !== requestId.current) return;
        setItems((prev) => (reset ? page.items : [...prev, ...page.items]));
        setCursor(page.nextCursor);
      } catch (e) {
        if (id === requestId.current) setError(errorMessage(e));
      } finally {
        if (id === requestId.current) setLoading(false);
      }
    },
    [topic],
  );

  useEffect(() => {
    setItems([]);
    setCursor(null);
    void load(true, null);
  }, [load]);

  return (
    <section className="news-tab">
      <div className="news-toolbar">
        <div className="news-chips" role="tablist" aria-label="Тема новостей">
          {FILTERS.map((f) => (
            <button
              key={f.value}
              type="button"
              role="tab"
              aria-selected={topic === f.value}
              className={`news-chip${topic === f.value ? ' is-active' : ''}`}
              onClick={() => setTopic(f.value)}
            >
              {f.label}
            </button>
          ))}
        </div>
        <button type="button" onClick={() => load(true, null)} disabled={loading}>
          Обновить
        </button>
      </div>

      {error && <p className="news-error">{error}</p>}

      {items.map((item) => (
        <NewsCard key={item.id} item={item} />
      ))}

      {!loading && !error && items.length === 0 && (
        <p className="news-muted">Новостей пока нет. Источники опрашиваются раз в несколько минут.</p>
      )}
      {loading && <p className="news-muted">Загружаю…</p>}

      {cursor && !loading && (
        <button type="button" className="news-more" onClick={() => load(false, cursor)}>
          Загрузить ещё
        </button>
      )}

      <p className="news-muted news-disclaimer">Новости не являются инвестиционной рекомендацией.</p>
    </section>
  );
}
