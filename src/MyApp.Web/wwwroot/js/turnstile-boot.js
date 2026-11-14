// Обычный (не модульный) скрипт: должен выполниться раньше api.js Cloudflare и раньше модулей.
// Cloudflare вызывает onTurnstile, когда готов; модуль captcha.js узнаёт об этом по событию
// (так нет гонки, если api.js загрузится быстрее модулей).
window.onTurnstile = () => {
    window.turnstileReady = true;
    window.dispatchEvent(new Event("turnstile-ready"));
};
