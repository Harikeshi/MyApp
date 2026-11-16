import type { ResetParams } from "../../api/account";

/** Параметры из ссылки в письме: #reset=1&email=...&code=... Читаются один раз при загрузке страницы. */
function readResetParams(): ResetParams | null {
  const p = new URLSearchParams(location.hash.slice(1));
  if (p.get("reset") !== "1") return null;

  const email = p.get("email");
  const code = p.get("code");
  history.replaceState(null, "", location.pathname + location.search); // убираем код из адресной строки
  return email && code ? { email, code } : null;
}

export const initialResetParams = readResetParams();
