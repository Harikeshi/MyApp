import { useEffect, useState, type FormEvent } from "react";
import { errorMessage } from "../../api/http";
import { useAuth, type Notice } from "../auth/AuthContext";
import "./settings.css";

// Те же правила, что на сервере (DisplayNameRules): сервер всё равно проверяет заново.
const NAME_MIN = 2;
const NAME_MAX = 30;
const NAME_PATTERN = /^[\p{L}\p{N}][\p{L}\p{N} ._-]*$/u;

function validateName(raw: string): string | null {
  const name = raw.trim();
  if (name.length < NAME_MIN || name.length > NAME_MAX) return `Имя должно содержать от ${NAME_MIN} до ${NAME_MAX} символов`;
  if (!NAME_PATTERN.test(name)) return "В имени допустимы буквы, цифры, пробел и символы . _ -";
  return null;
}

export default function Settings() {
  const { user, updateName, deleteAccount } = useAuth();

  // ---------- имя ----------
  const [name, setName] = useState(user?.name ?? "");
  const [nameBusy, setNameBusy] = useState(false);
  const [nameNotice, setNameNotice] = useState<Notice | null>(null);

  useEffect(() => setName(user?.name ?? ""), [user?.name]);

  const nameChanged = name.trim() !== (user?.name ?? "");

  const saveName = async (e: FormEvent) => {
    e.preventDefault();
    if (nameBusy || !nameChanged) return;

    const problem = validateName(name);
    if (problem) {
      setNameNotice({ text: problem, ok: false });
      return;
    }

    setNameBusy(true);
    setNameNotice(null);
    try {
      await updateName(name.trim());
      setNameNotice({ text: "Имя сохранено", ok: true });
    } catch (err) {
      setNameNotice({ text: errorMessage(err), ok: false });
    } finally {
      setNameBusy(false);
    }
  };

  // ---------- удаление аккаунта ----------
  const [confirming, setConfirming] = useState(false);
  const [password, setPassword] = useState("");
  const [deleteBusy, setDeleteBusy] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const cancelDelete = () => {
    setConfirming(false);
    setPassword("");
    setDeleteError(null);
  };

  const submitDelete = async (e: FormEvent) => {
    e.preventDefault();
    if (deleteBusy) return;
    if (!password) {
      setDeleteError("Введите пароль");
      return;
    }

    setDeleteBusy(true);
    setDeleteError(null);
    try {
      await deleteAccount(password); // при успехе приложение вернёт на страницу входа
    } catch (err) {
      setDeleteError(errorMessage(err));
      setDeleteBusy(false);
    }
  };

  return (
    <div className="se">
      <h2>Настройки</h2>

      <section className="se-section">
        <h3>Профиль</h3>
        <p className="muted se-email">Почта: {user?.email || "—"}</p>

        <form className="se-form" onSubmit={(e) => void saveName(e)}>
          <label>
            Имя
            <input
              type="text"
              value={name}
              maxLength={NAME_MAX}
              autoComplete="nickname"
              disabled={nameBusy}
              onChange={(e) => {
                setName(e.target.value);
                setNameNotice(null);
              }}
            />
          </label>
          <div className="se-actions">
            <button type="submit" className="se-btn primary" disabled={nameBusy || !nameChanged}>
              {nameBusy ? "Сохраняю…" : "Сохранить"}
            </button>
          </div>
          {nameNotice && (
            <p className={nameNotice.ok ? "success" : "error"} role={nameNotice.ok ? "status" : "alert"}>
              {nameNotice.text}
            </p>
          )}
        </form>
      </section>

      <section className="se-section se-danger">
        <h3>Удаление аккаунта</h3>
        <p className="muted">
          Будут безвозвратно удалены ваши задачи, чаты, сообщения и загруженные файлы. Отменить это нельзя.
        </p>

        {!confirming ? (
          <button type="button" className="se-btn danger-outline" onClick={() => setConfirming(true)}>
            Удалить аккаунт…
          </button>
        ) : (
          <form className="se-form" onSubmit={(e) => void submitDelete(e)}>
            <label>
              Для подтверждения введите пароль
              <input
                type="password"
                value={password}
                autoComplete="current-password"
                autoFocus
                disabled={deleteBusy}
                onChange={(e) => {
                  setPassword(e.target.value);
                  setDeleteError(null);
                }}
              />
            </label>
            <div className="se-actions">
              <button type="submit" className="se-btn danger" disabled={deleteBusy || password === ""}>
                {deleteBusy ? "Удаляю…" : "Удалить навсегда"}
              </button>
              <button type="button" className="se-btn" disabled={deleteBusy} onClick={cancelDelete}>
                Отмена
              </button>
            </div>
            {deleteError && (
              <p className="error" role="alert">
                {deleteError}
              </p>
            )}
          </form>
        )}
      </section>
    </div>
  );
}
