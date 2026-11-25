const pad = (n: number) => String(n).padStart(2, "0");

/** ISO (UTC) -> значение поля datetime-local в местном времени: "2027-01-15T09:30". */
export function toLocalInput(iso: string): string {
  const d = new Date(iso);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

/** Значение поля datetime-local (местное время) -> ISO в UTC; пустое -> null. */
export function fromLocalInput(value: string): string | null {
  if (!value) return null;
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? null : d.toISOString();
}

const dueFormat = new Intl.DateTimeFormat("ru-RU", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });

/** "15.01.2027, 09:30" */
export const formatDue = (iso: string) => dueFormat.format(new Date(iso));

/** Начало сегодняшнего дня пользователя в мс: срок раньше этого момента считается просроченным. */
export const startOfTodayMs = () => {
  const d = new Date();
  d.setHours(0, 0, 0, 0);
  return d.getTime();
};
