export const $ = id => document.getElementById(id);

export function show(text, ok = false) {
    const m = $("msg");
    m.textContent = text;
    m.className = "msg " + (ok ? "ok" : "err");
}
