// Вход, регистрация и сброс пароля. Эти запросы идут без токена (кроме /me и /logout),
// поэтому не через http.ts: ошибки здесь разбираются по-своему (коды Identity и captcha).
import { ApiError, getJson } from "./http";
import { getToken } from "./session";

export interface Profile {
  name: string;
  email: string;
}

/** Параметры из ссылки в письме: #reset=1&email=...&code=... */
export interface ResetParams {
  email: string;
  code: string;
}

const NETWORK_ERROR = "Нет связи с сервером, попробуйте ещё раз";

interface ErrorBody {
  errors?: Record<string, string[]>;
  error?: string;
  detail?: string;
}

async function post(path: string, body: unknown, captchaToken?: string): Promise<Response> {
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  if (captchaToken !== undefined) headers["X-Captcha-Token"] = captchaToken;
  try {
    return await fetch(path, { method: "POST", headers, body: JSON.stringify(body) });
  } catch {
    throw new ApiError(0, NETWORK_ERROR);
  }
}

async function readBody(res: Response): Promise<ErrorBody | null> {
  try {
    return (await res.json()) as ErrorBody;
  } catch {
    return null; // тело не JSON
  }
}

function errorText(status: number, data: ErrorBody | null): string {
  if (status === 429) return "Слишком много запросов, подождите минуту";
  if (data) {
    if (data.errors?.InvalidToken) return "Ссылка недействительна или устарела: запросите сброс заново";
    if (data.errors?.DuplicateEmail) return "Эта почта уже зарегистрирована";
    if (data.errors) return Object.values(data.errors).flat().join(" ");
    if (data.error === "captcha_required") return "Пройдите проверку captcha";
    if (data.error === "captcha_failed") return "Проверка captcha не пройдена, попробуйте ещё раз";
    if (data.detail === "NotAllowed") return "Почта не подтверждена: перейдите по ссылке из письма";
    if (data.detail === "LockedOut") return "Аккаунт временно заблокирован из-за неудачных попыток входа";
  }
  if (status === 401) return "Неверная почта или пароль";
  return `Ошибка ${status}`;
}

async function fail(res: Response): Promise<never> {
  throw new ApiError(res.status, errorText(res.status, await readBody(res)));
}

/** Возвращает токен доступа. captchaToken: undefined — captcha на странице отключена. */
export async function login(email: string, password: string, captchaToken?: string): Promise<string> {
  const res = await post("/login", { email, password }, captchaToken);
  if (!res.ok) return fail(res);
  return ((await res.json()) as { accessToken: string }).accessToken;
}

export async function register(
  email: string,
  password: string,
  displayName: string,
  captchaToken?: string,
): Promise<void> {
  const res = await post("/account/register", { email, password, displayName }, captchaToken);
  if (!res.ok) await fail(res);
}

export async function forgotPassword(email: string, captchaToken?: string): Promise<void> {
  const res = await post("/account/forgot", { email }, captchaToken);
  if (res.ok) return;

  const data = await readBody(res);
  if (res.status === 404) {
    throw new ApiError(
      404,
      data?.error === "email_not_registered"
        ? "Пользователь с такой почтой не зарегистрирован."
        : "Сервис временно недоступен, попробуйте позже.",
    );
  }
  if (data?.error === "email_not_confirmed") {
    throw new ApiError(res.status, "Почта ещё не подтверждена: сначала подтвердите её по ссылке из письма.");
  }
  throw new ApiError(res.status, errorText(res.status, data));
}

export async function resetPassword(params: ResetParams, newPassword: string): Promise<void> {
  const res = await post("/resetPassword", { email: params.email, resetCode: params.code, newPassword });
  if (!res.ok) await fail(res);
}

export const fetchProfile = (signal?: AbortSignal) => getJson<Profile>("/me", signal);

/** Выход на сервере (отзывает refresh-токены). Ошибки не важны: клиент выходит в любом случае. */
export async function logoutRequest(): Promise<void> {
  try {
    await fetch("/logout", { method: "POST", headers: { Authorization: `Bearer ${getToken() ?? ""}` } });
  } catch {
    /* нет сети: выходим локально */
  }
}
