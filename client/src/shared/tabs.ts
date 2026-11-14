// Мост со страницей index.html: она сообщает о смене вкладки событием "tabchange".
export const TAB_CHANGE_EVENT = "tabchange";

/** Подписка на смену вкладки; возвращает функцию отписки. */
export function onTabChange(handler: (tab: string) => void): () => void {
  const listener = (e: Event) => handler((e as CustomEvent<string>).detail);
  window.addEventListener(TAB_CHANGE_EVENT, listener);
  return () => window.removeEventListener(TAB_CHANGE_EVENT, listener);
}
