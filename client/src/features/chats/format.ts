/** 12345 -> "12 345" */
export const formatNumber = (n: number): string => n.toLocaleString("ru-RU");

/** Короткая запись для подписей на графике: 1234 -> "1,2 тыс.", 2500000 -> "2,5 млн". */
export function formatCompact(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1).replace(".", ",")} млн`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1).replace(".", ",")} тыс.`;
  return String(n);
}
