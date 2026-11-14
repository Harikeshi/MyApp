import { getToken, notifyUnauthorized } from "./session.js";
import { captchaActive, captchaToken, resetCaptcha } from "./captcha.js";

export async function api(path, { method = "GET", body, captcha = false } = {}) {
    const headers = {};
    if (body) headers["Content-Type"] = "application/json";

    const token = getToken();
    if (token) headers["Authorization"] = "Bearer " + token;

    const useCaptcha = captcha && captchaActive();
    if (useCaptcha) headers["X-Captcha-Token"] = captchaToken();

    let res;
    try {
        res = await fetch(path, { method, headers, body: body ? JSON.stringify(body) : undefined });
    } catch {
        return Response.error();                       // сети нет: status 0, res.ok === false
    } finally {
        if (useCaptcha) resetCaptcha();
    }

    if (res.status === 401 && getToken() && path !== "/logout") notifyUnauthorized();
    return res;
}

export async function errorText(res) {
    if (res.status === 0) return "Нет связи с сервером, попробуйте ещё раз";
    if (res.status === 429) return "Слишком много запросов, подождите минуту";
    try {
        const data = await res.json();
        if (data.errors && data.errors.InvalidToken) return "Ссылка недействительна или устарела: запросите сброс заново";
        if (data.errors && data.errors.DuplicateEmail) return "Эта почта уже зарегистрирована";
        if (data.errors) return Object.values(data.errors).flat().join(" ");
        if (data.error === "captcha_required") return "Пройдите проверку captcha";
        if (data.error === "captcha_failed") return "Проверка captcha не пройдена, попробуйте ещё раз";
        if (data.detail === "NotAllowed") return "Почта не подтверждена: перейдите по ссылке из письма";
        if (data.detail === "LockedOut") return "Аккаунт временно заблокирован из-за неудачных попыток входа";
        if (res.status === 401) return "Неверная почта или пароль";
    } catch { /* тело не JSON */ }
    return "Ошибка " + res.status;
}
