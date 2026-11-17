import { useState } from "react";
import { chatsApi } from "../../api/chats";
import { errorMessage } from "../../api/http";
import type { Attachment } from "../../types/chats";

export const formatSize = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} Б`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} КБ`;
  return `${(bytes / 1024 / 1024).toFixed(1)} МБ`;
};

interface Props {
  chatId: string | null;
  messageId: string;
  attachments: Attachment[];
}

/** Файлы сообщения с кнопкой «Скачать». У ещё не отправленного (временного) сообщения кнопок нет. */
export default function AttachmentList({ chatId, messageId, attachments }: Props) {
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const downloadChatId = messageId.startsWith("pending-") ? null : chatId;

  const run = async (key: string, action: () => Promise<void>) => {
    if (busy) return;
    setBusy(key);
    setError(null);
    try {
      await action();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusy(null);
    }
  };

  return (
    <div className="ch-attachments">
      <ul className="ch-files">
        {attachments.map((a) => (
          <li key={a.id} className="ch-file">
            <span aria-hidden="true">📎</span>
            <span className="ch-file-name" title={a.fileName}>
              {a.fileName}
            </span>
            <span className="ch-file-size">{formatSize(a.fileSize)}</span>
            {downloadChatId !== null && (
              <button
                type="button"
                className="ch-file-dl"
                disabled={busy !== null}
                onClick={() => void run(a.id, () => chatsApi.downloadAttachment(downloadChatId, a))}
              >
                {busy === a.id ? "…" : "Скачать"}
              </button>
            )}
          </li>
        ))}
      </ul>

      {downloadChatId !== null && attachments.length > 1 && (
        <button
          type="button"
          className="ch-file-all"
          disabled={busy !== null}
          onClick={() => void run("all", () => chatsApi.downloadMessageFiles(downloadChatId, messageId))}
        >
          {busy === "all" ? "Готовлю архив…" : "Скачать всё (.zip)"}
        </button>
      )}

      {error && (
        <p className="ch-file-error" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}
