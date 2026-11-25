// Сессия живёт долго: токен доступа короткий (его срок задаёт сервер), а refresh-токен хранится дольше и обновляет его
// без участия пользователя (см. refreshSession и http.ts). Оба токена лежат в localStorage: так вход
// сохраняется после закрытия вкладки и браузера. Выход (logout) и «Сессия истекла» стирают оба.
const ACCESS_KEY = "token";
const REFRESH_KEY = "refreshToken";

export const getToken = () => localStorage.getItem(ACCESS_KEY);
const getRefreshToken = () => localStorage.getItem(REFRESH_KEY);

export const setTokens = (accessToken: string, refreshToken?: string) => {
  localStorage.setItem(ACCESS_KEY, accessToken);
  if (refreshToken) localStorage.setItem(REFRESH_KEY, refreshToken);
};

export const clearToken = () => {
  localStorage.removeItem(ACCESS_KEY);
  localStorage.removeItem(REFRESH_KEY);
};

// Что делать, когда сервер ответил 401 на запрос с токеном (задаёт AuthProvider).
let unauthorizedHandler: () => void = () => {};
export const onUnauthorized = (handler: () => void) => {
  unauthorizedHandler = handler;
};
export const notifyUnauthorized = () => unauthorizedHandler();

/** ok — токены обновлены; denied — сервер отверг refresh-токен (нужен новый вход); offline — нет связи, сессия цела. */
export type RefreshResult = "ok" | "denied" | "offline";

let refreshing: Promise<RefreshResult> | null = null;

/** Обновляет токен доступа. Одновременные вызовы делят один запрос. */
export function refreshSession(): Promise<RefreshResult> {
  refreshing ??= doRefresh().finally(() => {
    refreshing = null;
  });
  return refreshing;
}

async function doRefresh(): Promise<RefreshResult> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) return "denied";

  let res: Response;
  try {
    res = await fetch("/refresh", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    });
  } catch {
    return "offline";
  }

  if (res.status === 401 || res.status === 403) return "denied";
  if (!res.ok) return "offline";             // 429, 5xx: сессию не трогаем, попробуем позже

  try {
    const data = (await res.json()) as { accessToken?: string; refreshToken?: string };
    if (!data.accessToken) return "denied";
    setTokens(data.accessToken, data.refreshToken);
    return "ok";
  } catch {
    return "offline";
  }
}
