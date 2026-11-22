import { deleteJson, getJson, postJson } from "./http";
import type { NewsComment, NewsContent, NewsPage, NewsTopic, Translation } from "../types/news";

export interface NewsListParams {
  topic?: NewsTopic;
  cursor?: string | null;
  limit?: number;
}

export const fetchNews = ({ topic, cursor, limit }: NewsListParams, signal?: AbortSignal) => {
  const qs = new URLSearchParams();
  if (topic) qs.set("topic", topic);
  if (cursor) qs.set("cursor", cursor);
  if (limit) qs.set("limit", String(limit));
  const query = qs.toString();
  return getJson<NewsPage>(query ? `/news?${query}` : "/news", signal);
};

/** Полный текст статьи для «Читать полностью». */
export const fetchNewsContent = (newsId: number, signal?: AbortSignal) =>
  getJson<NewsContent>(`/news/${newsId}/content`, signal);

export const fetchComments = (newsId: number, signal?: AbortSignal) =>
  getJson<NewsComment[]>(`/news/${newsId}/comments`, signal);

export const addComment = (newsId: number, text: string) => postJson<NewsComment>(`/news/${newsId}/comments`, { text });

export const removeComment = (commentId: number) => deleteJson(`/news/comments/${commentId}`);

export const reportComment = (commentId: number) => postJson<void>(`/news/comments/${commentId}/report`, undefined);

/** Перевод по кнопке: первый запрос переводит и сохраняет, дальше сервер отдаёт готовый. */
export const translateNews = (newsId: number) => postJson<Translation>(`/news/${newsId}/translation`, undefined);
