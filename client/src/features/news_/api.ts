import { request } from './http';
import type { NewsComment, NewsPage, NewsTopic, Translation } from './types';

export const newsApi = {
  list: (p: { topic?: NewsTopic; cursor?: string | null; limit?: number }) => {
    const qs = new URLSearchParams();
    if (p.topic) qs.set('topic', p.topic);
    if (p.cursor) qs.set('cursor', p.cursor);
    if (p.limit) qs.set('limit', String(p.limit));
    const query = qs.toString();
    return request<NewsPage>('GET', query ? `/news?${query}` : '/news');
  },
  translate: (id: number) => request<Translation>('POST', `/news/${id}/translation`),
  comments: (id: number) => request<NewsComment[]>('GET', `/news/${id}/comments`),
  addComment: (id: number, text: string) => request<NewsComment>('POST', `/news/${id}/comments`, { text }),
  deleteComment: (commentId: number) => request<void>('DELETE', `/news/comments/${commentId}`),
  reportComment: (commentId: number) => request<void>('POST', `/news/comments/${commentId}/report`),
};
