import { useEffect, useState } from "react";
import { errorMessage } from "../../api/http";
import { addComment, fetchComments, removeComment, reportComment } from "../../api/news";
import type { NewsComment } from "../../types/news";
import { formatTime } from "./format";

const MAX_LENGTH = 2000; // как на сервере (CommentRules.MaxLength)

interface Props {
  newsId: number;
  onCountChange: (delta: number) => void;
}

export default function Comments({ newsId, onCountChange }: Props) {
  const [comments, setComments] = useState<NewsComment[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [text, setText] = useState("");
  const [sending, setSending] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    fetchComments(newsId, ctrl.signal)
      .then(setComments)
      .catch((e) => {
        if (!ctrl.signal.aborted) setLoadError(errorMessage(e));
      });
    return () => ctrl.abort();
  }, [newsId]);

  async function submit() {
    if (!text.trim() || sending) return;
    setSending(true);
    setFormError(null);
    try {
      const created = await addComment(newsId, text);
      setComments((prev) => [...(prev ?? []), created]);
      setText("");
      onCountChange(1);
    } catch (e) {
      setFormError(errorMessage(e));
    } finally {
      setSending(false);
    }
  }

  async function remove(c: NewsComment) {
    try {
      await removeComment(c.id);
      setComments((prev) => (prev ?? []).filter((x) => x.id !== c.id));
      onCountChange(-1);
    } catch (e) {
      setNotice(errorMessage(e));
    }
  }

  async function report(c: NewsComment) {
    try {
      await reportComment(c.id);
      setNotice("Жалоба отправлена.");
    } catch (e) {
      setNotice(errorMessage(e));
    }
  }

  return (
    <div className="news-comments">
      {loadError && <p className="error">{loadError}</p>}
      {!comments && !loadError && <p className="muted">Загрузка комментариев…</p>}
      {comments && comments.length === 0 && <p className="muted">Комментариев пока нет.</p>}

      <ul className="news-comment-list">
        {comments?.map((c) => (
          <li key={c.id} className="news-comment">
            <div className="news-comment-head">
              <strong>{c.userName}</strong>
              <span className="muted">{formatTime(c.createdAt)}</span>
            </div>
            {/* Только текст: HTML из комментариев не выводим. */}
            <p className="news-comment-text">{c.text}</p>
            <button type="button" className="news-link" onClick={() => void (c.isMine ? remove(c) : report(c))}>
              {c.isMine ? "Удалить" : "Пожаловаться"}
            </button>
          </li>
        ))}
      </ul>

      {notice && <p className="muted">{notice}</p>}

      <div className="news-comment-form">
        <textarea
          value={text}
          maxLength={MAX_LENGTH}
          rows={3}
          placeholder="Ваш комментарий. Не больше одной ссылки, только https."
          onChange={(e) => setText(e.target.value)}
        />
        <div className="news-comment-form-row">
          <span className="muted">
            {text.length}/{MAX_LENGTH}
          </span>
          <button type="button" onClick={() => void submit()} disabled={sending || !text.trim()}>
            {sending ? "Отправляю…" : "Отправить"}
          </button>
        </div>
        {formError && <p className="error">{formError}</p>}
      </div>
    </div>
  );
}
