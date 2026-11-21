import { useState } from 'react';
import { newsApi } from './api';
import { errorMessage } from './http';
import { formatTime, topicLabel } from './format';
import { shorten } from './shorten';
import Comments from './Comments';
import type { NewsItem, Translation } from './types';

export default function NewsCard({ item }: { item: NewsItem }) {
  const [translation, setTranslation] = useState<Translation | null>(item.translation);
  const [showTranslation, setShowTranslation] = useState(false);
  const [translating, setTranslating] = useState(false);
  const [translateError, setTranslateError] = useState<string | null>(null);

  const [expanded, setExpanded] = useState(false);
  const [showComments, setShowComments] = useState(false);
  const [commentCount, setCommentCount] = useState(item.commentCount);
  const [imageFailed, setImageFailed] = useState(false);

  async function toggleTranslation() {
    if (showTranslation) {
      setShowTranslation(false);
      return;
    }
    if (!translation) {
      // Перевода ещё нет: сервер переведёт и сохранит, дальше он общий для всех.
      setTranslating(true);
      setTranslateError(null);
      try {
        setTranslation(await newsApi.translate(item.id));
      } catch (e) {
        setTranslateError(errorMessage(e));
        setTranslating(false);
        return;
      }
      setTranslating(false);
    }
    setShowTranslation(true);
  }

  const shown = showTranslation && translation ? translation : { title: item.title, summary: item.summary };
  const summary = shown.summary ? shorten(shown.summary) : null;

  return (
    <article className="news-card">
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
          <span className={`news-badge news-badge-${item.topic}`}>{topicLabel(item.topic)}</span>
          <span>{item.sourceName}</span>
          <span className="news-muted">{formatTime(item.publishedAt)}</span>
        </div>

        <h3 className="news-title">
          <a href={item.url} target="_blank" rel="noopener noreferrer">
            {shown.title}
          </a>
        </h3>

        {shown.summary && summary && (
          <p className="news-summary">
            {expanded || !summary.truncated ? shown.summary : summary.text}
            {summary.truncated && (
              <>
                {' '}
                <button type="button" className="news-link" onClick={() => setExpanded((v) => !v)}>
                  {expanded ? 'Свернуть' : 'Показать полностью'}
                </button>
              </>
            )}
          </p>
        )}

        <div className="news-actions">
          <button type="button" onClick={toggleTranslation} disabled={translating}>
            {translating ? 'Перевожу…' : showTranslation ? 'Показать оригинал' : 'Показать перевод'}
          </button>
          <button type="button" onClick={() => setShowComments((v) => !v)}>
            Комментарии ({commentCount})
          </button>
          {showTranslation && <span className="news-muted">Машинный перевод</span>}
        </div>

        {translateError && <p className="news-error">{translateError}</p>}

        {showComments && (
          <Comments newsId={item.id} onCountChange={(d) => setCommentCount((n) => Math.max(0, n + d))} />
        )}
      </div>
    </article>
  );
}
