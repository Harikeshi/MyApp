// Мост со страницей index.html: она сообщает о смене вкладки событием "tabchange".
export const TAB_CHANGE_EVENT = "tabchange";

/** Подписка на смену вкладки; возвращает функцию отписки. */
export function onTabChange(handler: (tab: string) => void): () => void {
  const listener = (e: Event) => handler((e as CustomEvent<string>).detail);
  window.addEventListener(TAB_CHANGE_EVENT, listener);
  return () => window.removeEventListener(TAB_CHANGE_EVENT, listener);
}

// Страница сообщает о выходе из аккаунта событием "sessionend" (wwwroot/js/account.js).
export const SESSION_END_EVENT = "sessionend";

/** Подписка на выход из аккаунта (чтобы не показывать данные прошлого пользователя); возвращает функцию отписки. */
export function onSessionEnd(handler: () => void): () => void {
  window.addEventListener(SESSION_END_EVENT, handler);
  return () => window.removeEventListener(SESSION_END_EVENT, handler);
}
