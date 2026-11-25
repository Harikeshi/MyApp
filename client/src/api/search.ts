import { getJson } from "./http";
import { chatsApi } from "./chats";
import type { NoteTag } from "./notes";

export interface SearchTask {
  id: number;
  title: string;
  projectId: number | null;
  isDone: boolean;
}

export interface SearchNote {
  id: number;
  title: string;
  tag: NoteTag;
  preview: string;
}

export interface SearchProject {
  id: number;
  name: string;
}

export interface SearchChat {
  id: string;
  title: string;
  modelId: string;
  modelName: string;
}

export interface SearchResults {
  tasks: SearchTask[];
  notes: SearchNote[];
  projects: SearchProject[];
  chats: SearchChat[];
}

export const MIN_QUERY = 2;

const CHAT_PER_GROUP = 10;
const CHAT_INDEX_TTL_MS = 60_000;

let chatIndex: { at: number; items: SearchChat[] } | null = null;

/**
 * Чаты живут в сервисе AiChat. Ищем по названиям: список чатов всех моделей берём разом и держим минуту,
 * дальше фильтруем в браузере. Поиска по тексту сообщений у AiChat нет, пока его не добавят там.
 */
async function loadChatIndex(signal?: AbortSignal): Promise<SearchChat[]> {
  if (chatIndex && Date.now() - chatIndex.at < CHAT_INDEX_TTL_MS) return chatIndex.items;

  const models = await chatsApi.getModels(signal);
  const lists = await Promise.all(
    models.map(async (m) =>
      (await chatsApi.getChatsByModel(m.id, signal)).map((c) => ({ id: c.id, title: c.title, modelId: m.id, modelName: m.name })),
    ),
  );
  const items = lists.flat();
  chatIndex = { at: Date.now(), items };
  return items;
}

/** Сбрасывает кэш названий чатов (вызывается при выходе из аккаунта). */
export const resetChatIndex = () => {
  chatIndex = null;
};

const searchChats = async (query: string, signal?: AbortSignal): Promise<SearchChat[]> => {
  try {
    const q = query.toLowerCase();
    return (await loadChatIndex(signal)).filter((c) => c.title.toLowerCase().includes(q)).slice(0, CHAT_PER_GROUP);
  } catch (e) {
    if (signal?.aborted) throw e;
    return []; // чаты недоступны (сервис AiChat не отвечает): остальные результаты всё равно показываем
  }
};

export async function searchAll(query: string, signal?: AbortSignal): Promise<SearchResults> {
  const [own, chats] = await Promise.all([
    getJson<Omit<SearchResults, "chats">>(`/search?q=${encodeURIComponent(query)}`, signal),
    searchChats(query, signal),
  ]);
  return { ...own, chats };
}
