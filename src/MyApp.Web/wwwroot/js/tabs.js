import { $ } from "./dom.js";

// Переключение вкладок. Событие "tabchange" слушают React-вкладки «Статистика» и «Чаты» (client/src/shared/tabs.ts).
// Вкладка «Настройки» не в панели: она открывается кликом по имени пользователя.
export function setTab(name) {
    document.querySelectorAll("#tabs button").forEach(b =>
        b.classList.toggle("active", b.dataset.tab === name));
    $("userName").classList.toggle("active", name === "settings");
    document.body.classList.toggle("wide", name === "chats");      // чатам нужна ширина
    document.querySelectorAll('[id^="tab-"]').forEach(el =>
        el.hidden = el.id !== "tab-" + name);
    window.dispatchEvent(new CustomEvent("tabchange", { detail: name }));
}

export function initTabs() {
    $("tabs").onclick = e => {
        const b = e.target.closest("button[data-tab]");
        if (b) setTab(b.dataset.tab);
    };
    $("userName").onclick = () => setTab("settings");
}
