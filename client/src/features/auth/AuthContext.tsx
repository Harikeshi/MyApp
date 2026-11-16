import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import * as accountApi from "../../api/account";
import type { Profile, ResetParams } from "../../api/account";
import { ApiError } from "../../api/http";
import { clearToken, getToken, onUnauthorized, setToken } from "../../api/session";
import { initialResetParams } from "./resetLink";

export interface Notice {
  text: string;
  ok: boolean;
}

type Status = "loading" | "anon" | "authed";

interface AuthState {
  status: Status;
  user: Profile | null;
  /** Сообщение для страницы входа (например, «Сессия истекла»). */
  notice: Notice | null;
  /** Не null, если страница открыта по ссылке сброса пароля из письма. */
  resetParams: ResetParams | null;
  login: (email: string, password: string, captchaToken?: string) => Promise<void>;
  logout: () => Promise<void>;
  finishReset: (newPassword: string) => Promise<void>;
}

const Ctx = createContext<AuthState>(null!);
export const useAuth = () => useContext(Ctx);

const NO_PROFILE: Profile = { name: "", email: "" };

export function AuthProvider({ children }: { children: ReactNode }) {
  const [resetParams, setResetParams] = useState<ResetParams | null>(initialResetParams);
  // по ссылке сброса пароля автоматический вход не нужен
  const [status, setStatus] = useState<Status>(() => (initialResetParams || !getToken() ? "anon" : "loading"));
  const [user, setUser] = useState<Profile | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);

  const endSession = useCallback((message: Notice | null = null) => {
    clearToken();
    setUser(null);
    setNotice(message);
    setStatus("anon");
  }, []);

  // 401 на любом запросе с токеном: сессия закончилась
  useEffect(() => {
    onUnauthorized(() => endSession({ text: "Сессия истекла, войдите снова", ok: false }));
  }, [endSession]);

  // при загрузке страницы: токен есть — спрашиваем профиль (заодно проверяем, что токен ещё действует)
  useEffect(() => {
    if (initialResetParams || !getToken()) return;

    let cancelled = false;
    accountApi
      .fetchProfile()
      .then((profile) => {
        if (cancelled) return;
        setUser(profile);
        setStatus("authed");
      })
      .catch((e: unknown) => {
        if (cancelled) return;
        if (e instanceof ApiError && e.status === 401) return; // сессию уже закрыл onUnauthorized
        setUser(NO_PROFILE); // профиль не загрузился, но токен не отвергнут: приложение всё равно открываем
        setStatus("authed");
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback<AuthState["login"]>(async (email, password, captchaToken) => {
    setToken(await accountApi.login(email, password, captchaToken));

    let profile = NO_PROFILE;
    try {
      profile = await accountApi.fetchProfile();
    } catch (e) {
      if (e instanceof ApiError && e.status === 401) return; // сессию уже закрыл onUnauthorized
    }
    setUser(profile);
    setNotice(null);
    setStatus("authed");
  }, []);

  const logout = useCallback(async () => {
    await accountApi.logoutRequest();
    endSession();
  }, [endSession]);

  const finishReset = useCallback<AuthState["finishReset"]>(
    async (newPassword) => {
      if (!resetParams) return;
      await accountApi.resetPassword(resetParams, newPassword);
      setResetParams(null);
      endSession({ text: "Пароль изменён. Войдите с новым паролем.", ok: true });
    },
    [resetParams, endSession],
  );

  const value = useMemo(
    () => ({ status, user, notice, resetParams, login, logout, finishReset }),
    [status, user, notice, resetParams, login, logout, finishReset],
  );

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}
