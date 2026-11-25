// Единственное место, где клиент знает про токен и fetch для запросов от имени пользователя.
import { getToken, notifyUnauthorized, refreshSession } from "./session";

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/** Текст ошибки для пользователя: сообщение сервера или «нет связи». */
export const errorMessage = (e: unknown): string => (e instanceof ApiError ? e.message : "Нет связи с сервером");

/** Сообщение сервера из тела ответа ({ error } у чатов, { detail }/{ errors }/{ title } у ProblemDetails MyApp). */
async function serverMessage(res: Response): Promise<string | null> {
  try {
    const data = (await res.json()) as { error?: unknown; detail?: unknown; errors?: unknown; title?: unknown };
    for (const value of [data.error, data.detail]) {
      if (typeof value === "string" && value) return value;
    }
    if (data.errors && typeof data.errors === "object") {
      const text = Object.values(data.errors as Record<string, unknown>)
        .flat()
        .filter((v): v is string => typeof v === "string")
        .join(" ");
      if (text) return text;
    }
    if (typeof data.title === "string" && data.title) return data.title;
  } catch {
    /* тело не JSON */
  }
  return null;
}

/**
 * fetch с токеном. Ответ 401 — токен доступа мог просто истечь: один раз обновляем его через refresh-токен
 * и повторяем запрос. Если не вышло, сессия закончилась.
 */
async function authFetch(path: string, init: RequestInit): Promise<Response> {
  const attempt = () => {
    const token = getToken();
    if (!token) throw new ApiError(401, "Войдите, чтобы увидеть данные");
    const headers = { ...(init.headers as Record<string, string> | undefined), Authorization: `Bearer ${token}` };
    return fetch(path, { ...init, headers });
  };

  let res = await attempt();
  if (res.status === 401) {
    const refreshed = await refreshSession();
    if (refreshed === "offline") throw new ApiError(0, "Нет связи с сервером");
    if (refreshed === "ok") res = await attempt();
  }

  if (res.status === 401) {
    notifyUnauthorized(); // сессия закончилась: приложение вернёт пользователя на страницу входа
    throw new ApiError(401, "Сессия истекла, войдите снова");
  }
  return res;
}

async function request<T>(method: string, path: string, body?: unknown, signal?: AbortSignal): Promise<T> {
  const headers: Record<string, string> = { Accept: "application/json" };
  // FormData уходит как есть: Content-Type с boundary браузер выставит сам
  const isForm = body instanceof FormData;
  if (body !== undefined && !isForm) headers["Content-Type"] = "application/json";

  const res = await authFetch(path, {
    method,
    headers,
    body: body === undefined ? undefined : isForm ? (body as FormData) : JSON.stringify(body),
    signal,
  });

  if (!res.ok) throw new ApiError(res.status, (await serverMessage(res)) ?? `Ошибка ${res.status}`);

  // PUT и DELETE отвечают 204 без тела
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const getJson = <T>(path: string, signal?: AbortSignal) => request<T>("GET", path, undefined, signal);

export const postJson = <T>(path: string, body: unknown, signal?: AbortSignal) =>
  request<T>("POST", path, body, signal);

export const postForm = <T>(path: string, form: FormData, signal?: AbortSignal) =>
  request<T>("POST", path, form, signal);

/**
 * Скачивает файл и отдаёт его браузеру как загрузку. Обычная ссылка <a href> не подходит:
 * токен хранится в localStorage, а не в cookie, и в такой запрос не попал бы заголовок Authorization.
 */
export async function downloadFile(path: string, fileName: string): Promise<void> {
  const res = await authFetch(path, {});
  if (!res.ok) throw new ApiError(res.status, (await serverMessage(res)) ?? `Ошибка ${res.status}`);

  const url = URL.createObjectURL(await res.blob());
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 10_000); // браузеру нужно время, чтобы начать загрузку
}

export const putJson = <T = void>(path: string, body: unknown) => request<T>("PUT", path, body);

export const patchJson = <T = void>(path: string, body: unknown) => request<T>("PATCH", path, body);

export const deleteJson = (path: string) => request<void>("DELETE", path);
