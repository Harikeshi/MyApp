// Cloudflare Turnstile: скрипт подгружается один раз, когда впервые понадобилась форма входа.
interface TurnstileApi {
  render(container: HTMLElement, options: { sitekey: string }): string;
  getResponse(widgetId: string): string | undefined;
  reset(widgetId: string): void;
  remove(widgetId: string): void;
}

declare global {
  interface Window {
    turnstile?: TurnstileApi;
  }
}

const CALLBACK = "onTurnstileLoaded";
const SRC = `https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit&onload=${CALLBACK}`;

let loading: Promise<TurnstileApi> | null = null;

export function loadTurnstile(): Promise<TurnstileApi> {
  if (window.turnstile) return Promise.resolve(window.turnstile);
  if (loading) return loading;

  loading = new Promise<TurnstileApi>((resolve, reject) => {
    (window as unknown as Record<string, unknown>)[CALLBACK] = () => {
      if (window.turnstile) resolve(window.turnstile);
      else reject(new Error("turnstile unavailable"));
    };

    const script = document.createElement("script");
    script.src = SRC;
    script.async = true;
    script.onerror = () => {
      loading = null; // при следующем открытии формы попробуем ещё раз
      script.remove();
      reject(new Error("turnstile failed to load"));
    };
    document.head.appendChild(script);
  });
  return loading;
}
