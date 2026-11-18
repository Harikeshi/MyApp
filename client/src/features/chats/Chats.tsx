import { useEffect, useRef, useState, type ChangeEvent, type KeyboardEvent } from "react";
import type { MessageRole } from "../../types/chats";
import AttachmentList, { formatSize } from "./AttachmentList";
import { formatNumber } from "./format";
import MessageContent from "./MessageContent";
import { useChats } from "./useChats";
import "./chats.css";

const ROLE_LABEL: Record<MessageRole, string> = { user: "Вы", assistant: "Ассистент", system: "Система" };
const MAX_INPUT_HEIGHT = 160;

// Лимиты вложений (на сервере AiChat: Storage:MaxFilesPerMessage / MaxFileBytes / MaxRequestBytes).
const MAX_FILES = 10;
const MAX_FILE_BYTES = 20 * 1024 * 1024;
const MAX_TOTAL_BYTES = 35 * 1024 * 1024;

const MOBILE_QUERY = "(max-width: 720px)";
const isMobile = () => window.matchMedia(MOBILE_QUERY).matches;

const dateFormat = new Intl.DateTimeFormat("ru-RU", {
  day: "2-digit",
  month: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
});
const formatDate = (iso: string) => dateFormat.format(new Date(iso));

const sameFile = (a: File, b: File) => a.name === b.name && a.size === b.size && a.lastModified === b.lastModified;

export default function Chats({ active }: { active: boolean }) {
  const c = useChats(active);
  const [draft, setDraft] = useState("");
  const [files, setFiles] = useState<File[]>([]);
  const [attachError, setAttachError] = useState<string | null>(null);
  // на телефоне выбор модели и чатов свёрнут, чтобы переписка занимала весь экран
  const [sidebarOpen, setSidebarOpen] = useState(() => !isMobile());
  const messagesRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

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

  const collapseOnMobile = () => {
    if (isMobile()) setSidebarOpen(false);
  };

  const onPickFiles = (e: ChangeEvent<HTMLInputElement>) => {
    const picked = Array.from(e.target.files ?? []);
    e.target.value = ""; // чтобы тот же файл можно было выбрать ещё раз
    if (picked.length === 0) return;

    const next = [...files];
    let problem: string | null = null;
    for (const f of picked) {
      if (next.some((x) => sameFile(x, f))) continue;
      if (f.size === 0) {
        problem = `Файл «${f.name}» пустой`;
        continue;
      }
      if (f.size > MAX_FILE_BYTES) {
        problem = `Файл «${f.name}» больше ${MAX_FILE_BYTES / 1024 / 1024} МБ`;
        continue;
      }
      if (next.length >= MAX_FILES) {
        problem = `Можно прикрепить не больше ${MAX_FILES} файлов`;
        break;
      }
      if (next.reduce((sum, x) => sum + x.size, 0) + f.size > MAX_TOTAL_BYTES) {
        problem = "Суммарный размер файлов слишком большой";
        continue;
      }
      next.push(f);
    }
    setFiles(next);
    setAttachError(problem);
  };

  const removeFile = (index: number) => {
    setFiles((prev) => prev.filter((_, i) => i !== index));
    setAttachError(null);
  };

  const canSend = !c.sending && !!c.modelId && (draft.trim() !== "" || files.length > 0);

  const submit = async () => {
    const text = draft.trim();
    if (!canSend) return;

    const toSend = files;
    setDraft("");
    setFiles([]);
    setAttachError(null);
    const ok = await c.send(text, toSend);
    if (!ok) {
      // не отправилось: возвращаем текст и файлы в поле
      setDraft((current) => current || text);
      setFiles((current) => (current.length > 0 ? current : toSend));
    }
  };

  const onKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey && !e.nativeEvent.isComposing) {
      e.preventDefault();
      void submit();
    }
  };

  const isNewChat = c.chatId === null;
  const modelName = c.models.find((m) => m.id === c.modelId)?.name ?? "";
  const chatTitle = isNewChat ? "Новый чат" : (c.chats.find((chat) => chat.id === c.chatId)?.title ?? "Чат");
  const shownError = attachError ?? c.error;

  // израсходовано в этом чате: у каждого ответа свой запрос с полной историей, поэтому токены суммируются
  const usedInput = c.messages.reduce((sum, m) => sum + (m.inputTokens ?? 0), 0);
  const usedOutput = c.messages.reduce((sum, m) => sum + (m.outputTokens ?? 0), 0);
  const usageLine = usedInput + usedOutput > 0 ? `токенов: ↑${formatNumber(usedInput)} ↓${formatNumber(usedOutput)}` : "";

  return (
    <div className="ch">
      <aside id="ch-sidebar" className="ch-sidebar" hidden={!sidebarOpen}>
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
          <button
            type="button"
            className="ch-new"
            disabled={c.sending || !c.modelId}
            onClick={() => {
              c.startNewChat();
              collapseOnMobile();
            }}
          >
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
                onClick={() => {
                  void c.openChat(chat.id);
                  collapseOnMobile();
                }}
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
        <div className="ch-bar">
          <button
            type="button"
            className="ch-toggle"
            aria-expanded={sidebarOpen}
            aria-controls="ch-sidebar"
            onClick={() => setSidebarOpen((open) => !open)}
          >
            <span aria-hidden="true">{sidebarOpen ? "✕" : "☰"}</span>
            <span>{sidebarOpen ? "Скрыть" : "Чаты"}</span>
          </button>
          <div className="ch-bar-title">
            {(modelName || usageLine) && (
              <span className="ch-bar-model">{[modelName, usageLine].filter(Boolean).join(" · ")}</span>
            )}
            <span className="ch-bar-chat">{chatTitle}</span>
          </div>
          <button
            type="button"
            className="ch-bar-new"
            aria-label="Новый чат"
            disabled={c.sending || !c.modelId}
            onClick={() => {
              c.startNewChat();
              collapseOnMobile();
            }}
          >
            ＋
          </button>
        </div>

        <div className="ch-messages" ref={messagesRef}>
          {isNewChat && c.messages.length === 0 && (
            <p className="ch-muted">Напишите первое сообщение или прикрепите файл: чат создастся автоматически.</p>
          )}

          {c.messages.map((m) => (
            <div key={m.id} className={`ch-msg ch-msg-${m.role}`}>
              <div className="ch-msg-head">
                <span className="ch-role">{ROLE_LABEL[m.role]}</span>
                <time dateTime={m.createdAt}>{formatDate(m.createdAt)}</time>
              </div>
              {m.content && <MessageContent text={m.content} markdown={m.role === "assistant"} />}
              {m.attachments && m.attachments.length > 0 && (
                <AttachmentList chatId={c.chatId} messageId={m.id} attachments={m.attachments} />
              )}
              {m.role === "assistant" && m.inputTokens != null && (
                <div className="ch-tokens" title="Токены запроса (вся история) и ответа">
                  ↑ {formatNumber(m.inputTokens)} · ↓ {formatNumber(m.outputTokens ?? 0)} токенов
                </div>
              )}
            </div>
          ))}

          {c.sending && <div className="ch-msg ch-msg-assistant ch-typing">Ассистент отвечает…</div>}
        </div>

        {shownError && (
          <p className="ch-error" role="alert">
            {shownError}
          </p>
        )}

        {files.length > 0 && (
          <ul className="ch-pending">
            {files.map((f, i) => (
              <li key={`${f.name}-${f.size}-${f.lastModified}`} className="ch-pending-item">
                <span className="ch-pending-name" title={f.name}>
                  {f.name}
                </span>
                <span className="ch-file-size">{formatSize(f.size)}</span>
                <button type="button" aria-label={`Убрать ${f.name}`} disabled={c.sending} onClick={() => removeFile(i)}>
                  ✕
                </button>
              </li>
            ))}
          </ul>
        )}

        <div className="ch-input">
          <input ref={fileInputRef} type="file" multiple hidden onChange={onPickFiles} />
          <button
            type="button"
            className="ch-attach"
            aria-label="Прикрепить файл"
            title="Прикрепить файл или ZIP-архив"
            disabled={c.sending || !c.modelId}
            onClick={() => fileInputRef.current?.click()}
          >
            📎
          </button>
          <textarea
            ref={inputRef}
            rows={1}
            value={draft}
            disabled={!c.modelId}
            placeholder="Сообщение (Enter — отправить, Shift+Enter — новая строка)"
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={onKeyDown}
          />
          <button type="button" className="ch-send" disabled={!canSend} onClick={() => void submit()}>
            Отправить
          </button>
        </div>
      </section>
    </div>
  );
}
