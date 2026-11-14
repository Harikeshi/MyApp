import { SITEKEY } from "./config.js";

let widgetId = null;

export function initCaptcha() {
    if (!SITEKEY) return;

    const render = () => { widgetId = window.turnstile.render("#captcha", { sitekey: SITEKEY }); };
    if (window.turnstileReady) render();
    else window.addEventListener("turnstile-ready", render, { once: true });
}

export const captchaActive = () => Boolean(SITEKEY) && widgetId !== null;
export const captchaToken = () => window.turnstile.getResponse(widgetId) || "";
export const resetCaptcha = () => window.turnstile.reset(widgetId);      // токен одноразовый
