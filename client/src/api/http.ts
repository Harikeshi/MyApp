// Единственное место, где клиент знает про токен и fetch.
// Пока вход делает wwwroot/index.html, он кладёт токен в sessionStorage; когда логин переедет в React,
// поменяется только эта функция.
const TOKEN_KEY = "token";

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/** Сообщение сервера из тела ответа ({ error } у чатов, { detail }/{ title } у ProblemDetails MyApp). */
async function serverMessage(res: Response): Promise<string | null> {
  try {
    const data = (await res.json()) as { error?: unknown; detail?: unknown; title?: unknown };
    for (const value of [data.error, data.detail, data.title]) {
      if (typeof value === "string" && value) return value;
    }
  } catch {
    /* тело не JSON */
  }
  return null;
}

async function request<T>(method: string, path: string, body?: unknown, signal?: AbortSignal): Promise<T> {
  const token = sessionStorage.getItem(TOKEN_KEY);
  if (!token) throw new ApiError(401, "Войдите, чтобы увидеть данные");

  const headers: Record<string, string> = { Authorization: `Bearer ${token}`, Accept: "application/json" };
  if (body !== undefined) headers["Content-Type"] = "application/json";

  const res = await fetch(path, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  });

  if (res.status === 401) throw new ApiError(401, "Сессия истекла, войдите снова");
  if (!res.ok) throw new ApiError(res.status, (await serverMessage(res)) ?? `Ошибка ${res.status}`);

  return (await res.json()) as T;
}

export const getJson = <T>(path: string, signal?: AbortSignal) => request<T>("GET", path, undefined, signal);

export const postJson = <T>(path: string, body: unknown, signal?: AbortSignal) =>
  request<T>("POST", path, body, signal);
