// Темы приходят в camelCase (так настроен JSON в приложении).
export type NewsTopic = "markets" | "crypto" | "manuls";
export type TopicFilter = "all" | NewsTopic;

export interface Translation {
  title: string;
  summary: string | null;
}

export interface NewsItem {
  id: number;
  sourceName: string;
  topic: NewsTopic;
  title: string;
  url: string;
  summary: string | null;
  imageUrl: string | null;
  publishedAt: string;
  commentCount: number;
  translation: Translation | null; // null: перевод ещё не делали
}

export interface NewsPage {
  items: NewsItem[];
  nextCursor: string | null; // null: новостей больше нет
}

export interface NewsComment {
  id: number;
  userName: string;
  text: string;
  createdAt: string;
  isMine: boolean;
}
