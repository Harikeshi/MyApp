// Типы повторяют контракт сервиса AiChat (AiChat.Api/Dtos/ChatDtos.cs); все id — Guid в виде строки.
export interface AiModel {
  id: string;
  name: string;
  description: string | null;
}

export interface Chat {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  messageCount: number;
}

export type MessageRole = "user" | "assistant" | "system";

/** Файл, прикреплённый к сообщению (загруженный пользователем или созданный моделью). */
export interface Attachment {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
}

export interface ChatMessage {
  id: string;
  role: MessageRole;
  content: string;
  createdAt: string;
  /** Нет у временного сообщения и у ответов старого сервера. */
  attachments?: Attachment[];
}

/** Ответ POST /api/chats/{id}/messages: сохранённое сообщение пользователя и ответ модели. */
export interface SendMessageResult {
  userMessage: ChatMessage;
  assistantMessage: ChatMessage;
  usage: { inputTokens: number; outputTokens: number };
}
