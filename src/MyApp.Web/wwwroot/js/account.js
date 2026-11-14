import { $, show } from "./dom.js";
import { api, errorText } from "./api.js";
import { clearToken, setToken } from "./session.js";
import { setTab } from "./tabs.js";
import { loadTodos, resetTodosPage } from "./todos.js";

const creds = () => ({ email: $("email").value.trim(), password: $("password").value });

// ---------- режимы формы входа ----------
function setMode(mode) {                      // "login" | "register" | "forgot"
    const titles = { login: "Вход", register: "Регистрация", forgot: "Сброс пароля" };
    $("authTitle").textContent = titles[mode];
    $("nameRow").hidden = mode !== "register";
    $("pwdRow").hidden = mode === "forgot";
    $("password").autocomplete = mode === "register" ? "new-password" : "current-password";
    $("loginButtons").hidden = mode !== "login";
    $("registerButtons").hidden = mode !== "register";
    $("forgotButtons").hidden = mode !== "forgot";
    $("toRegister").hidden = mode !== "login";
    $("forgotLink").hidden = mode !== "login";
    $("toLogin").hidden = mode === "login";
    show("");
}

// ---------- вход, регистрация, выход ----------
async function login() {
    const res = await api("/login", { method: "POST", body: creds(), captcha: true });
    if (!res.ok) return show(await errorText(res));
    setToken((await res.json()).accessToken);
    $("password").value = "";
    show("");
    await showApp();
}

async function register() {
    const displayName = $("displayName").value.trim();
    if (displayName.length < 2) return show("Введите имя пользователя (от 2 до 30 символов)");
    const res = await api("/account/register", {
        method: "POST",
        body: { email: $("email").value.trim(), password: $("password").value, displayName },
        captcha: true
    });
    if (!res.ok) return show(await errorText(res));
    setMode("login");
    show("Готово. Проверьте почту, подтвердите адрес и затем войдите.", true);
}

async function loadUser() {
    const res = await api("/me");
    if (!res.ok) return;
    const me = await res.json();
    $("userName").textContent = me.name;
    $("userName").title = me.email;
}

export async function showApp() {
    $("auth").hidden = true;
    $("app").hidden = false;
    $("topbar").hidden = false;
    await Promise.all([loadUser(), loadTodos()]);
}

export function logout(message = "") {
    clearToken();
    resetTodosPage();
    $("app").hidden = true;
    $("topbar").hidden = true;
    $("userName").textContent = "";
    $("auth").hidden = false;
    setTab("todos");                           // после повторного входа открываем первую вкладку
    show(message);
}

// ---------- сброс пароля ----------
async function sendReset() {
    const email = $("email").value.trim();
    if (!email) return show("Введите почту");
    const res = await api("/account/forgot", { method: "POST", body: { email }, captcha: true });
    if (res.ok) return show("Мы отправили письмо со ссылкой для сброса пароля.", true);
    if (res.status === 404) {
        const data = await res.clone().json().catch(() => null);
        return show(data?.error === "email_not_registered"
            ? "Пользователь с такой почтой не зарегистрирован."
            : "Сервис временно недоступен, попробуйте позже.");
    }
    try {
        const data = await res.clone().json();
        if (data.error === "email_not_confirmed") return show("Почта ещё не подтверждена: сначала подтвердите её по ссылке из письма.");
    } catch { /* не JSON */ }
    show(await errorText(res));
}

// Параметры из ссылки в письме: #reset=1&email=...&code=...
function readResetParams() {
    const p = new URLSearchParams(location.hash.slice(1));
    return p.get("reset") === "1" ? { email: p.get("email"), code: p.get("code") } : null;
}

let resetParams = null;

async function doReset() {
    const pw = $("newPassword").value;
    if (pw !== $("newPassword2").value) return show("Пароли не совпадают");
    const res = await api("/resetPassword", {
        method: "POST",
        body: { email: resetParams.email, resetCode: resetParams.code, newPassword: pw }
    });
    if (!res.ok) return show(await errorText(res));
    resetParams = null;
    $("newPassword").value = $("newPassword2").value = "";
    $("reset").hidden = true;
    $("auth").hidden = false;
    setMode("login");
    show("Пароль изменён. Войдите с новым паролем.", true);
}

/** true, если страница открыта по ссылке сброса пароля (тогда автоматический вход не нужен). */
export const isResetFlow = () => resetParams !== null;

export function initAccount() {
    resetParams = readResetParams();
    if (resetParams) {
        history.replaceState(null, "", location.pathname);      // убираем код из адресной строки
        $("auth").hidden = true;
        $("reset").hidden = false;
    }

    $("toRegister").onclick = e => { e.preventDefault(); setMode("register"); };
    $("forgotLink").onclick = e => { e.preventDefault(); setMode("forgot"); };
    $("toLogin").onclick = e => { e.preventDefault(); setMode("login"); };
    $("sendReset").onclick = sendReset;
    $("doReset").onclick = doReset;
    $("login").onclick = login;
    $("register").onclick = register;
    $("logout").onclick = async () => {
        try { await api("/logout", { method: "POST" }); } catch { /* нет сети: выходим локально */ }
        logout();
    };
}
