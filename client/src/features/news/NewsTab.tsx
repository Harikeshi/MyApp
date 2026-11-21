import type { TopicFilter } from "../../types/news";
import NewsCard from "./NewsCard";
import { useNews } from "./useNews";
import "./news.css";

const FILTERS: { value: TopicFilter; label: string }[] = [
  { value: "all", label: "Все" },
  { value: "markets", label: "Рынки" },
  { value: "crypto", label: "Крипта" },
];

const time = (d: Date) => d.toLocaleTimeString("ru-RU", { hour: "2-digit", minute: "2-digit" });

/** Новости по рынкам и крипте: лента, перевод по кнопке, комментарии. */
export default function NewsTab({ active }: { active: boolean }) {
  const n = useNews(active);
  const first = n.items.length === 0;

  return (
    <div className="news-tab">
      <div className="news-toolbar">
        <div className="news-chips" role="group" aria-label="Тема новостей">
          {FILTERS.map((f) => (
            <button
              key={f.value}
              type="button"
              className={n.topic === f.value ? "news-chip on" : "news-chip"}
              aria-pressed={n.topic === f.value}
              onClick={() => n.setTopic(f.value)}
            >
              {f.label}
            </button>
          ))}
        </div>
        <button type="button" onClick={() => void n.refresh()} disabled={n.loading}>
          {n.loading ? "Обновляю…" : "Обновить"}
        </button>
      </div>

      {/* Что сейчас показано: по этой строке видно, что кнопки сработали. */}
      <p className="muted news-status" aria-live="polite">
        {n.loading
          ? "Загружаю…"
          : n.updatedAt
            ? `Обновлено в ${time(n.updatedAt)} · показано новостей: ${n.items.length}${n.hasMore ? " и есть ещё" : ""}`
            : ""}
      </p>

      {n.error && (
        <p className="error">
          {n.error}{" "}
          <button type="button" className="news-link" onClick={() => void n.refresh()}>
            Повторить
          </button>
        </p>
      )}

      <div className={n.loading && !first ? "news-list is-loading" : "news-list"}>
        {n.items.map((item) => (
          <NewsCard key={item.id} item={item} />
        ))}
      </div>

      {!n.loading && !n.error && first && (
        <p className="muted">
          {n.topic === "all"
            ? "Новостей пока нет. Источники опрашиваются раз в несколько минут, лента обновляется из базы."
            : "В этой теме пока нет новостей."}
        </p>
      )}

      {n.hasMore && !n.loading && (
        <button type="button" className="news-more" onClick={() => void n.loadMore()}>
          Загрузить ещё
        </button>
      )}

      <p className="muted news-disclaimer">Новости не являются инвестиционной рекомендацией.</p>
    </div>
  );
}
