/**
 * Тонкий адаптер над HTTP. Если в клиенте уже есть общий http-клиент (подстановка токена, обработка 401),
 * замените тело `request` вызовом вашего клиента, остальной код новостей менять не нужно.
 */
export class HttpError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

export async function request<T>(method: string, url: string, body?: unknown): Promise<T> {
  const token = localStorage.getItem('token'); // TODO: ключ, под которым клиент хранит токен
  const headers: Record<string, string> = {};
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const res = await fetch(url, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (!res.ok) {
    let message = res.statusText;
    try {
      const problem = await res.json();
      message = problem.detail ?? problem.title ?? message;
    } catch {
      /* тело не JSON */
    }
    throw new HttpError(res.status, message);
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

export const errorMessage = (e: unknown): string =>
  e instanceof Error && e.message ? e.message : 'Что-то пошло не так. Попробуйте ещё раз.';
