export const formatTime = (iso: string): string =>
  new Date(iso).toLocaleString("ru-RU", { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" });

const TOPIC_LABELS: Record<string, string> = { markets: "Рынки", crypto: "Крипта", manuls: "Манулы" };

export const topicLabel = (topic: string): string => TOPIC_LABELS[topic] ?? topic;
