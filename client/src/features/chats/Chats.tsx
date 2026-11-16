import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import type { MessageRole } from "../../types/chats";
import MessageContent from "./MessageContent";
import { useChats } from "./useChats";
import "./chats.css";

const ROLE_LABEL: Record<MessageRole, string> = { user: "Вы", assistant: "Ассистент", system: "Система" };
const MAX_INPUT_HEIGHT = 160;

const dateFormat = new Intl.DateTimeFormat("ru-RU", {
  day: "2-digit",
  month: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
});
const formatDate = (iso: string) => dateFormat.format(new Date(iso));

export default function Chats({ active }: { active: boolean }) {
  const c = useChats(active);
  const [draft, setDraft] = useState("");
  const messagesRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);

  // прокрутка к последнему сообщению
  useEffect(() => {
    const el = messagesRef.current;
    if (el) el.scrollTop = el.scrollHeight;
  }, [c.messages, c.sending]);

  // поле ввода растёт вместе с текстом
  useEffect(() => {
    const el = inputRef.current;
    if (!el) return;
    el.style.height = "auto";
    el.style.height = `${Math.min(el.scrollHeight, MAX_INPUT_HEIGHT)}px`;
  }, [draft]);

  const submit = async () => {
    const text = draft.trim();
    if (!text || c.sending || !c.modelId) return;

    setDraft("");
    const ok = await c.send(text);
    if (!ok) setDraft((current) => current || text); // не отправилось: возвращаем текст в поле
  };

  const onKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey && !e.nativeEvent.isComposing) {
      e.preventDefault();
      void submit();
    }
  };

  const isNewChat = c.chatId === null;

  return (
    <div className="ch">
      <aside className="ch-sidebar">
        <div className="ch-sidebar-head">
          <select
            className="ch-model"
            value={c.modelId ?? ""}
            disabled={c.sending || c.models.length === 0}
            onChange={(e) => c.selectModel(e.target.value)}
          >
            {c.models.map((m) => (
              <option key={m.id} value={m.id}>
                {m.name}
              </option>
            ))}
          </select>
          <button type="button" className="ch-new" disabled={c.sending || !c.modelId} onClick={c.startNewChat}>
            + Новый чат
          </button>
        </div>

        <ul className="ch-list">
          {c.chats.map((chat) => (
            <li key={chat.id}>
              <button
                type="button"
                className={chat.id === c.chatId ? "ch-item active" : "ch-item"}
                disabled={c.sending}
                onClick={() => void c.openChat(chat.id)}
              >
                <span className="ch-title">{chat.title}</span>
                <span className="ch-meta">
                  {formatDate(chat.updatedAt)} · сообщений: {chat.messageCount}
                </span>
              </button>
            </li>
          ))}
        </ul>
        {c.chats.length === 0 && c.modelId && <p className="ch-muted">Чатов пока нет</p>}
        {c.models.length === 0 && <p className="ch-muted">Нет доступных моделей</p>}
      </aside>

      <section className="ch-main">
        <div className="ch-messages" ref={messagesRef}>
          {isNewChat && c.messages.length === 0 && (
            <p className="ch-muted">Напишите первое сообщение: чат создастся автоматически.</p>
          )}

          {c.messages.map((m) => (
            <div key={m.id} className={`ch-msg ch-msg-${m.role}`}>
              <div className="ch-msg-head">
                <span className="ch-role">{ROLE_LABEL[m.role]}</span>
                <time dateTime={m.createdAt}>{formatDate(m.createdAt)}</time>
              </div>
              <MessageContent text={m.content} markdown={m.role === "assistant"} />
            </div>
          ))}

          {c.sending && <div className="ch-msg ch-msg-assistant ch-typing">Ассистент отвечает…</div>}
        </div>

        {c.error && (
          <p className="ch-error" role="alert">
            {c.error}
          </p>
        )}

        <div className="ch-input">
          <textarea
            ref={inputRef}
            rows={1}
            value={draft}
            disabled={!c.modelId}
            placeholder="Сообщение (Enter — отправить, Shift+Enter — новая строка)"
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={onKeyDown}
          />
          <button
            type="button"
            className="ch-send"
            disabled={c.sending || !c.modelId || draft.trim() === ""}
            onClick={() => void submit()}
          >
            Отправить
          </button>
        </div>
      </section>
    </div>
  );
}
