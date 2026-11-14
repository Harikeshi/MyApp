import { initCaptcha } from "./captcha.js";
import { getToken, onUnauthorized } from "./session.js";
import { initTabs } from "./tabs.js";
import { initTodos } from "./todos.js";
import { initAccount, isResetFlow, logout, showApp } from "./account.js";

onUnauthorized(() => logout("Сессия истекла, войдите снова"));

initCaptcha();
initTabs();
initTodos();
initAccount();

if (getToken() && !isResetFlow()) showApp();
