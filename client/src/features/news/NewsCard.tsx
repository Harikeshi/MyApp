import { useRef, useState } from "react";
import { errorMessage } from "../../api/http";
import { fetchNewsContent, translateNews } from "../../api/news";
import type { NewsItem, Translation } from "../../types/news";
import Comments from "./Comments";
import { formatTime, topicLabel } from "./format";
import { shorten } from "./shorten";

export default function NewsCard({ item }: { item: NewsItem }) {
  const [translation, setTranslation] = useState<Translation | null>(item.translation);
  const [showTranslation, setShowTranslation] = useState(false);
  const [translating, setTranslating] = useState(false);
  const [translateError, setTranslateError] = useState<string | null>(null);

  // Развёрнутая карточка: показывает полный текст статьи (если сервер смог его получить), иначе полное описание.
  const [expanded, setExpanded] = useState(false);
  const [content, setContent] = useState<string | null | undefined>(undefined); // undefined: ещё не грузили
  const [contentLoading, setContentLoading] = useState(false);
  const [contentError, setContentError] = useState<string | null>(null);

  const [showComments, setShowComments] = useState(false);
  const [commentCount, setCommentCount] = useState(item.commentCount);
  const [imageFailed, setImageFailed] = useState(false);
  const cardRef = useRef<HTMLElement>(null);

  async function toggleTranslation() {
    if (showTranslation) {
      setShowTranslation(false);
      return;
    }
    if (!translation) {
      // Перевода ещё нет: сервер переведёт и сохранит, дальше он общий для всех пользователей.
      setTranslating(true);
      setTranslateError(null);
      try {
        setTranslation(await translateNews(item.id));
      } catch (e) {
        setTranslateError(errorMessage(e));
        setTranslating(false);
        return;
      }
      setTranslating(false);
    }
    setShowTranslation(true);
  }

  async function loadContent() {
    if (contentLoading) return;
    setContentLoading(true);
    setContentError(null);
    try {
      setContent((await fetchNewsContent(item.id)).content);
    } catch (e) {
      setContentError(errorMessage(e));
    } finally {
      setContentLoading(false);
    }
  }

  async function toggleExpanded() {
    if (expanded) {
      setExpanded(false);
      // Длинный текст уходит, и карточка может оказаться выше экрана: возвращаем её на место, иначе кажется, что кнопка не сработала.
      requestAnimationFrame(() => {
        const el = cardRef.current;
        if (el && el.getBoundingClientRect().top < 0) el.scrollIntoView({ block: "start", behavior: "smooth" });
      });
      return;
    }
    setExpanded(true);
    // Текст запрашиваем всегда, а не только при item.hasContent: флаг пришёл вместе со списком,
    // а фоновая загрузка статьи могла закончиться уже после этого.
    if (content === undefined) await loadContent();
  }

  const shown = showTranslation && translation ? translation : { title: item.title, summary: item.summary };
  const preview = shown.summary ? shorten(shown.summary) : null;

  // Что показывать в развёрнутом виде: полный текст, пока он грузится или недоступен — описание.
  const fullText = expanded && content ? content : null;
  const paragraphs = fullText ? fullText.split(/\n{2,}/) : null;

  return (
    <article className="news-card" ref={cardRef}>
      {item.imageUrl && !imageFailed && (
        <img
          className="news-card-image"
          src={item.imageUrl}
          alt=""
          loading="lazy"
          referrerPolicy="no-referrer"
          onError={() => setImageFailed(true)}
        />
      )}

      <div className="news-card-body">
        <div className="news-meta">
          <span className={`news-badge ${item.topic}`}>{topicLabel(item.topic)}</span>
          <span>{item.sourceName}</span>
          <span className="muted">{formatTime(item.publishedAt)}</span>
        </div>

        <h3 className="news-title">
          <a href={item.url} target="_blank" rel="noopener noreferrer">
            {shown.title}
          </a>
        </h3>

        {paragraphs ? (
          <div className="news-fulltext">
            {paragraphs.map((p, i) => (
              <p key={i}>{p}</p>
            ))}
            {showTranslation && <p className="muted">Перевод относится к заголовку и описанию, полный текст на языке оригинала.</p>}
          </div>
        ) : (
          shown.summary &&
          preview && (
            <p className="news-summary">
              {expanded || !preview.truncated ? shown.summary : preview.text}
            </p>
          )
        )}

        {expanded && contentLoading && <p className="muted">Загружаю текст…</p>}
        {expanded && !contentLoading && contentError && (
          <p className="error">
            {contentError}{" "}
            <button type="button" className="news-link" onClick={() => void loadContent()}>
              Повторить
            </button>
          </p>
        )}
        {expanded && !contentLoading && !contentError && content === null && (
          <p className="muted">Полный текст получить не удалось: он доступен на сайте источника.</p>
        )}

        <div className="news-actions">
          <button type="button" onClick={() => void toggleExpanded()} aria-expanded={expanded}>
            {expanded ? "Свернуть" : "Читать полностью"}
          </button>
          <button type="button" onClick={() => void toggleTranslation()} disabled={translating}>
            {translating ? "Перевожу…" : showTranslation ? "Показать оригинал" : "Показать перевод"}
          </button>
          <button type="button" onClick={() => setShowComments((v) => !v)}>
            Комментарии ({commentCount})
          </button>
          {showTranslation && <span className="muted">Машинный перевод</span>}
          {expanded && (
            <a className="news-source-link" href={item.url} target="_blank" rel="noopener noreferrer">
              Открыть на сайте {item.sourceName} ↗
            </a>
          )}
        </div>

        {translateError && <p className="error">{translateError}</p>}

        {showComments && (
          <Comments newsId={item.id} onCountChange={(d) => setCommentCount((n) => Math.max(0, n + d))} />
        )}
      </div>
    </article>
  );
}
