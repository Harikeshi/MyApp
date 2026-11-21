import { useEffect, useState } from 'react';
import { newsApi } from './api';
import { errorMessage } from './http';
import { formatTime } from './format';
import type { NewsComment } from './types';

const MAX_LENGTH = 2000;

interface Props {
  newsId: number;
  onCountChange: (delta: number) => void;
}

export default function Comments({ newsId, onCountChange }: Props) {
  const [comments, setComments] = useState<NewsComment[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [text, setText] = useState('');
  const [sending, setSending] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    newsApi
      .comments(newsId)
      .then((list) => {
        if (!cancelled) setComments(list);
      })
      .catch((e) => {
        if (!cancelled) setLoadError(errorMessage(e));
      });
    return () => {
      cancelled = true;
    };
  }, [newsId]);

  async function submit() {
    if (!text.trim() || sending) return;
    setSending(true);
    setFormError(null);
    try {
      const created = await newsApi.addComment(newsId, text);
      setComments((prev) => [...(prev ?? []), created]);
      setText('');
      onCountChange(1);
    } catch (e) {
      setFormError(errorMessage(e));
    } finally {
      setSending(false);
    }
  }

  async function remove(c: NewsComment) {
    try {
      await newsApi.deleteComment(c.id);
      setComments((prev) => (prev ?? []).filter((x) => x.id !== c.id));
      onCountChange(-1);
    } catch (e) {
      setNotice(errorMessage(e));
    }
  }

  async function report(c: NewsComment) {
    try {
      await newsApi.reportComment(c.id);
      setNotice('Жалоба отправлена.');
    } catch (e) {
      setNotice(errorMessage(e));
    }
  }

  return (
    <div className="news-comments">
      {loadError && <p className="news-error">{loadError}</p>}
      {!comments && !loadError && <p className="news-muted">Загружаю комментарии…</p>}
      {comments && comments.length === 0 && <p className="news-muted">Комментариев пока нет.</p>}

      <ul className="news-comment-list">
        {comments?.map((c) => (
          <li key={c.id} className="news-comment">
            <div className="news-comment-head">
              <strong>{c.userName}</strong>
              <span className="news-muted">{formatTime(c.createdAt)}</span>
            </div>
            {/* Только текст: никакого HTML из комментариев. */}
            <p className="news-comment-text">{c.text}</p>
            <div className="news-comment-actions">
              {c.isMine ? (
                <button type="button" className="news-link" onClick={() => remove(c)}>
                  Удалить
                </button>
              ) : (
                <button type="button" className="news-link" onClick={() => report(c)}>
                  Пожаловаться
                </button>
              )}
            </div>
          </li>
        ))}
      </ul>

      {notice && <p className="news-muted">{notice}</p>}

      <div className="news-comment-form">
        <textarea
          value={text}
          maxLength={MAX_LENGTH}
          rows={3}
          placeholder="Ваш комментарий. Не больше одной ссылки, только https."
          onChange={(e) => setText(e.target.value)}
        />
        <div className="news-comment-form-row">
          <span className="news-muted">
            {text.length}/{MAX_LENGTH}
          </span>
          <button type="button" onClick={submit} disabled={sending || !text.trim()}>
            {sending ? 'Отправляю…' : 'Отправить'}
          </button>
        </div>
        {formError && <p className="news-error">{formError}</p>}
      </div>
    </div>
  );
}
