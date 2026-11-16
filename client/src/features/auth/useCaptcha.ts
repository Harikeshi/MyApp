import { useCallback, useEffect, useRef } from "react";
import { SITEKEY } from "../../config";
import { loadTurnstile } from "./turnstile";

/**
 * Виджет Turnstile для формы. Положите containerRef на <div>.
 * token() даёт undefined, пока captcha отключена или не загрузилась (запрос уйдёт без заголовка,
 * сервер ответит captcha_required, если она у него включена); reset() нужен после каждого запроса (токен одноразовый).
 */
export function useCaptcha() {
  const containerRef = useRef<HTMLDivElement>(null);
  const widgetRef = useRef<string | null>(null);

  useEffect(() => {
    if (!SITEKEY) return;

    let cancelled = false;
    let id: string | null = null;

    loadTurnstile()
      .then((turnstile) => {
        if (cancelled || !containerRef.current) return;
        id = turnstile.render(containerRef.current, { sitekey: SITEKEY });
        widgetRef.current = id;
      })
      .catch(() => {
        /* captcha не загрузилась: войти не получится, если сервер её требует */
      });

    return () => {
      cancelled = true;
      if (id !== null) window.turnstile?.remove(id);
      widgetRef.current = null;
    };
  }, []);

  const token = useCallback((): string | undefined => {
    const id = widgetRef.current;
    return id === null ? undefined : window.turnstile?.getResponse(id) || "";
  }, []);

  const reset = useCallback(() => {
    const id = widgetRef.current;
    if (id !== null) window.turnstile?.reset(id);
  }, []);

  return { containerRef, token, reset };
}
