import { useState, type FormEvent } from "react";
import { forgotPassword, register } from "../../api/account";
import { errorMessage } from "../../api/http";
import { useAuth, type Notice } from "./AuthContext";
import { useCaptcha } from "./useCaptcha";

type Mode = "login" | "register" | "forgot";

const TITLES: Record<Mode, string> = { login: "Вход", register: "Регистрация", forgot: "Сброс пароля" };
const SUBMIT: Record<Mode, string> = { login: "Войти", register: "Зарегистрироваться", forgot: "Отправить ссылку" };

const err = (text: string): Notice => ({ text, ok: false });
const ok = (text: string): Notice => ({ text, ok: true });

/** Вход, регистрация и запрос сброса пароля: одна карточка, три режима. */
export default function AuthPage() {
  const { login, notice } = useAuth();
  const captcha = useCaptcha();

  const [mode, setMode] = useState<Mode>("login");
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [msg, setMsg] = useState<Notice | null>(notice);
  const [busy, setBusy] = useState(false);

  const switchMode = (next: Mode) => {
    setMode(next);
    setMsg(null);
  };

  const canSubmit = !busy && email.trim() !== "" && (mode === "forgot" || password !== "");

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setMsg(null);

    const mail = email.trim();
    const name = displayName.trim();
    if (mode === "register" && name.length < 2) {
      setMsg(err("Введите имя пользователя (от 2 до 30 символов)"));
      return;
    }

    setBusy(true);
    try {
      if (mode === "login") {
        await login(mail, password, captcha.token());
      } else if (mode === "register") {
        await register(mail, password, name, captcha.token());
        setMode("login");
        setPassword("");
        setMsg(ok("Готово. Проверьте почту, подтвердите адрес и затем войдите."));
      } else {
        await forgotPassword(mail, captcha.token());
        setMsg(ok("Мы отправили письмо со ссылкой для сброса пароля."));
      }
    } catch (e) {
      setMsg(err(errorMessage(e)));
    } finally {
      captcha.reset(); // токен одноразовый
      setBusy(false);
    }
  };

  return (
    <div className="login-wrap">
      <form className="login" onSubmit={submit}>
        {/* Файл лежит в client/public: Vite кладёт его в wwwroot/app/logo.png, страница отдаётся с /app/ */}
        <img className="login-logo" src="/app/logo.png" alt="Cupboard" width={360} height={199} />
        <h1>{TITLES[mode]}</h1>

        {mode === "register" && (
          <label>
            Имя пользователя
            <input
              value={displayName}
              maxLength={30}
              autoComplete="nickname"
              onChange={(e) => setDisplayName(e.target.value)}
            />
          </label>
        )}

        <label>
          Почта
          <input
            type="email"
            value={email}
            autoComplete="username"
            autoFocus
            onChange={(e) => setEmail(e.target.value)}
          />
        </label>

        {mode !== "forgot" && (
          <label>
            Пароль
            <input
              type="password"
              value={password}
              autoComplete={mode === "register" ? "new-password" : "current-password"}
              onChange={(e) => setPassword(e.target.value)}
            />
          </label>
        )}

        <div ref={captcha.containerRef} />

        {msg && (
          <p className={msg.ok ? "success" : "error"} role="status">
            {msg.text}
          </p>
        )}

        <button disabled={!canSubmit}>{SUBMIT[mode]}</button>

        <p className="login-links">
          {mode === "login" ? (
            <>
              <button type="button" className="link" onClick={() => switchMode("register")}>
                Создать аккаунт
              </button>
              <button type="button" className="link" onClick={() => switchMode("forgot")}>
                Забыли пароль?
              </button>
            </>
          ) : (
            <button type="button" className="link" onClick={() => switchMode("login")}>
              Назад ко входу
            </button>
          )}
        </p>
      </form>
    </div>
  );
}
