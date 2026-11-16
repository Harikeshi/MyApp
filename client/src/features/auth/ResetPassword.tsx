import { useState, type FormEvent } from "react";
import { errorMessage } from "../../api/http";
import { useAuth } from "./AuthContext";

/** Новый пароль по ссылке из письма. */
export default function ResetPassword() {
  const { finishReset } = useAuth();
  const [password, setPassword] = useState("");
  const [repeat, setRepeat] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (password !== repeat) {
      setError("Пароли не совпадают");
      return;
    }

    setBusy(true);
    setError(null);
    try {
      await finishReset(password); // успех: приложение само покажет страницу входа
    } catch (e) {
      setError(errorMessage(e));
      setBusy(false);
    }
  };

  return (
    <div className="login-wrap">
      <form className="login" onSubmit={submit}>
        <h1>Новый пароль</h1>
        <label>
          Новый пароль
          <input
            type="password"
            value={password}
            autoComplete="new-password"
            autoFocus
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
        <label>
          Повторите пароль
          <input
            type="password"
            value={repeat}
            autoComplete="new-password"
            onChange={(e) => setRepeat(e.target.value)}
          />
        </label>
        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
        <button disabled={busy || !password || !repeat}>Сохранить пароль</button>
      </form>
    </div>
  );
}
