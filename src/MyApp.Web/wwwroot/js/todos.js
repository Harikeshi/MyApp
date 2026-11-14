import { $, show } from "./dom.js";
import { api, errorText } from "./api.js";
import { PAGE_SIZE } from "./config.js";

const PRIORITY_LABEL = { low: "низкий", normal: "", high: "высокий" };

let page = 1;

export const resetTodosPage = () => { page = 1; };

function formatDate(iso) {                       // "2027-01-15" -> "15.01.2027", без сдвига часовых поясов
    const [y, m, d] = iso.split("-");
    return `${d}.${m}.${y}`;
}

function renderTodo(t, today) {
    const li = document.createElement("li");
    li.className = [t.isDone ? "done" : "", t.priority].filter(Boolean).join(" ");

    const cb = document.createElement("input");
    cb.type = "checkbox";
    cb.checked = t.isDone;
    cb.onchange = async () => {
        const r = await api(`/todos/${t.id}`, {
            method: "PUT",
            body: { title: t.title, isDone: cb.checked, dueDate: t.dueDate, priority: t.priority }
        });
        if (!r.ok) show(await errorText(r));
        loadTodos();
    };

    const span = document.createElement("span");
    span.textContent = t.title;

    li.append(cb, span);

    if (PRIORITY_LABEL[t.priority]) {
        const prio = document.createElement("span");
        prio.className = "prio";
        prio.textContent = PRIORITY_LABEL[t.priority];
        li.append(prio);
    }
    if (t.dueDate) {
        const due = document.createElement("span");
        due.className = "due" + (!t.isDone && t.dueDate < today ? " overdue" : "");
        due.textContent = formatDate(t.dueDate);
        li.append(due);
    }

    const del = document.createElement("button");
    del.textContent = "Удалить";
    del.onclick = async () => { await api(`/todos/${t.id}`, { method: "DELETE" }); loadTodos(); };
    li.append(del);

    return li;
}

function renderPager({ page: current, totalPages }) {
    $("pager").hidden = totalPages <= 1;
    $("pageInfo").textContent = `Страница ${current} из ${totalPages}`;
    $("prevPage").disabled = current <= 1;
    $("nextPage").disabled = current >= totalPages;
}

export async function loadTodos() {
    const res = await api(`/todos?page=${page}&pageSize=${PAGE_SIZE}`);
    if (!res.ok) {
        show("Не удалось загрузить задачи: " + await errorText(res));
        return;
    }

    const data = await res.json();      // { items, page, pageSize, totalCount, totalPages }

    // Страница опустела (например, удалили последнюю задачу на последней странице): переходим назад.
    if (data.items.length === 0 && page > 1) {
        page = Math.max(1, data.totalPages);
        return loadTodos();
    }

    const today = new Date().toLocaleDateString("sv-SE");        // локальная дата в виде YYYY-MM-DD
    $("list").replaceChildren(...data.items.map(t => renderTodo(t, today)));
    renderPager(data);
}

async function addTodo() {
    const title = $("title").value.trim();
    if (!title) return;
    const body = { title, priority: $("priority").value };
    const due = $("due").value;
    if (due) body.dueDate = due;

    const res = await api("/todos", { method: "POST", body });
    if (res.ok) {
        $("title").value = "";
        $("due").value = "";
        $("priority").value = "normal";
        page = 1;                       // порядок зависит от приоритета и срока: показываем с начала
        loadTodos();
    } else {
        show(await errorText(res));
    }
}

export function initTodos() {
    $("add").onclick = addTodo;
    $("prevPage").onclick = () => { if (page > 1) { page--; loadTodos(); } };
    $("nextPage").onclick = () => { page++; loadTodos(); };
}
