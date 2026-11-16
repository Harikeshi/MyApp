import { getJson, postJson } from "./http";
import type { AiModel, Chat, SendMessageResult, ChatMessage } from "../types/chats";

// Запросы идут на тот же адрес, что и страница: /api/** MyApp пересылает сервису AiChat.
export const chatsApi = {
  getModels: (signal?: AbortSignal) => getJson<AiModel[]>("/api/models", signal),

  getChatsByModel: (modelId: string, signal?: AbortSignal) =>
    getJson<Chat[]>(`/api/chats/model/${modelId}`, signal),

  getMessages: (chatId: string, signal?: AbortSignal) =>
    getJson<ChatMessage[]>(`/api/chats/${chatId}/messages`, signal),

  createChat: (modelId: string, title: string) => postJson<Chat>("/api/chats", { modelId, title }),

  sendMessage: (chatId: string, content: string) =>
    postJson<SendMessageResult>(`/api/chats/${chatId}/messages`, { content }),
};
