// Токен доступа хранится в sessionStorage под ключом "token" (как и раньше: открытые вкладки остаются в системе).
const KEY = "token";

export const getToken = () => sessionStorage.getItem(KEY);
export const setToken = (value: string) => sessionStorage.setItem(KEY, value);
export const clearToken = () => sessionStorage.removeItem(KEY);

// Что делать, когда сервер ответил 401 на запрос с токеном (задаёт AuthProvider).
let unauthorizedHandler: () => void = () => {};
export const onUnauthorized = (handler: () => void) => {
  unauthorizedHandler = handler;
};
export const notifyUnauthorized = () => unauthorizedHandler();
