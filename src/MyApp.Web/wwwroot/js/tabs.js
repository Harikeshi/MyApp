import { $ } from "./dom.js";

// Переключение вкладок. Событие "tabchange" слушает React-вкладка «Статистика» (client/src/shared/tabs.ts).
export function setTab(name) {
    document.querySelectorAll("#tabs button").forEach(b =>
        b.classList.toggle("active", b.dataset.tab === name));
    document.querySelectorAll('[id^="tab-"]').forEach(el =>
        el.hidden = el.id !== "tab-" + name);
    window.dispatchEvent(new CustomEvent("tabchange", { detail: name }));
}

export function initTabs() {
    $("tabs").onclick = e => {
        const b = e.target.closest("button[data-tab]");
        if (b) setTab(b.dataset.tab);
    };
}
