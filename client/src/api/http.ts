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

export async function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  const token = sessionStorage.getItem(TOKEN_KEY);
  if (!token) throw new ApiError(401, "Войдите, чтобы увидеть данные");

  const res = await fetch(path, {
    headers: { Authorization: `Bearer ${token}`, Accept: "application/json" },
    signal,
  });

  if (res.status === 401) throw new ApiError(401, "Сессия истекла, войдите снова");
  if (!res.ok) throw new ApiError(res.status, `Ошибка ${res.status}`);

  return (await res.json()) as T;
}
