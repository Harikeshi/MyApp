export const formatTime = (iso: string): string =>
  new Date(iso).toLocaleString("ru-RU", { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" });

export const topicLabel = (topic: string): string => (topic === "crypto" ? "Крипта" : "Рынки");
