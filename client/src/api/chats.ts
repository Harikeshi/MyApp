import { downloadFile, getJson, postForm, postJson } from "./http";
import type { AiModel, Attachment, Chat, SendMessageResult, ChatMessage } from "../types/chats";

/** Имя файла без каталогов: модель может вернуть путь вида "src/Program.cs". */
const baseName = (path: string) => path.split("/").pop() || "file";

// Запросы идут на тот же адрес, что и страница: /api/** MyApp пересылает сервису AiChat.
export const chatsApi = {
  getModels: (signal?: AbortSignal) => getJson<AiModel[]>("/api/models", signal),

  getChatsByModel: (modelId: string, signal?: AbortSignal) =>
    getJson<Chat[]>(`/api/chats/model/${modelId}`, signal),

  getMessages: (chatId: string, signal?: AbortSignal) =>
    getJson<ChatMessage[]>(`/api/chats/${chatId}/messages`, signal),

  createChat: (modelId: string, title: string) => postJson<Chat>("/api/chats", { modelId, title }),

  /** Без файлов уходит JSON, с файлами — multipart/form-data (поля content и files). */
  sendMessage: (chatId: string, content: string, files: File[] = []) => {
    const url = `/api/chats/${chatId}/messages`;
    if (files.length === 0) return postJson<SendMessageResult>(url, { content });

    const form = new FormData();
    form.append("content", content);
    for (const file of files) form.append("files", file, file.name);
    return postForm<SendMessageResult>(url, form);
  },

  downloadAttachment: (chatId: string, attachment: Attachment) =>
    downloadFile(`/api/chats/${chatId}/attachments/${attachment.id}`, baseName(attachment.fileName)),

  /** Все файлы сообщения одним ZIP-архивом. */
  downloadMessageFiles: (chatId: string, messageId: string) =>
    downloadFile(`/api/chats/${chatId}/messages/${messageId}/attachments.zip`, "files.zip"),
};
