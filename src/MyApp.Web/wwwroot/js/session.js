// Токен доступа. Тот же ключ читает React-клиент (client/src/api/http.ts), поэтому имя менять нельзя.
const KEY = "token";

let token = sessionStorage.getItem(KEY);
let unauthorizedHandler = () => {};

export const getToken = () => token;

export function setToken(value) {
    token = value;
    sessionStorage.setItem(KEY, value);
}

export function clearToken() {
    token = null;
    sessionStorage.removeItem(KEY);
}

/** Что делать, когда сервер ответил 401 на запрос с токеном (задаёт main.js). */
export const onUnauthorized = handler => { unauthorizedHandler = handler; };
export const notifyUnauthorized = () => unauthorizedHandler();
