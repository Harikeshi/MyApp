/** Сколько символов описания показываем, пока оно свёрнуто. Поменяйте под свой дизайн. */
export const PREVIEW_CHARS = 280;

/** Если скрывать нечего (остаток меньше этого), текст не сворачиваем: кнопка ради пары слов только мешает. */
const MIN_HIDDEN_CHARS = 40;

export interface Shortened {
  text: string;
  truncated: boolean;
}

/** Обрезает по границе слова и добавляет «…». Возвращает исходный текст, если он достаточно короткий. */
export function shorten(text: string): Shortened {
  if (text.length <= PREVIEW_CHARS + MIN_HIDDEN_CHARS) return { text, truncated: false };

  const cut = text.slice(0, PREVIEW_CHARS);
  const lastSpace = cut.lastIndexOf(' ');
  const base = lastSpace > PREVIEW_CHARS * 0.6 ? cut.slice(0, lastSpace) : cut;
  return { text: base.trimEnd() + '…', truncated: true };
}
