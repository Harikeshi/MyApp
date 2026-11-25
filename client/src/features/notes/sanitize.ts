// Записи хранятся как HTML. Перед показом и перед сохранением он очищается по белому списку:
// остаются только простое форматирование, цвет текста и картинки data:image. Всё остальное (скрипты,
// обработчики onclick, чужие ссылки на картинки, стили кроме цвета) выбрасывается.
const ALLOWED = new Set(["B", "STRONG", "I", "EM", "U", "S", "STRIKE", "BR", "P", "DIV", "SPAN", "UL", "OL", "LI", "H1", "H2", "H3", "BLOCKQUOTE", "IMG"]);
const DROP = new Set(["SCRIPT", "STYLE", "IFRAME", "OBJECT", "EMBED", "LINK", "META", "SVG", "MATH", "FORM", "TEMPLATE", "NOSCRIPT"]);
const COLOR = /^(#[0-9a-f]{3,8}|rgba?\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}\s*(,\s*[\d.]+\s*)?\)|[a-z]{3,20})$/i;
const IMAGE_SRC = /^data:image\/(png|jpe?g|gif|webp);base64,[a-z0-9+/=]+$/i;

export function sanitizeHtml(html: string): string {
  if (!html) return "";
  // оба документа «неживые»: скрипты не выполняются, картинки не загружаются
  const source = new DOMParser().parseFromString(html, "text/html");
  const target = document.implementation.createHTMLDocument("");
  const root = target.createElement("div");

  const copy = (node: Node, parent: Node) => {
    if (node.nodeType === Node.TEXT_NODE) {
      parent.appendChild(target.createTextNode(node.textContent ?? ""));
      return;
    }
    if (node.nodeType !== Node.ELEMENT_NODE) return;

    const el = node as HTMLElement;
    const tag = el.tagName;
    if (DROP.has(tag)) return;

    // чужой тег: сам выбрасываем, содержимое оставляем (FONT от старых версий браузеров превращаем в SPAN)
    const outTag = tag === "FONT" ? "SPAN" : tag;
    if (!ALLOWED.has(outTag)) {
      el.childNodes.forEach((child) => copy(child, parent));
      return;
    }

    const out = target.createElement(outTag.toLowerCase());
    if (outTag === "IMG") {
      const src = el.getAttribute("src") ?? "";
      if (!IMAGE_SRC.test(src)) return;
      out.setAttribute("src", src);
      out.setAttribute("alt", el.getAttribute("alt") ?? "");
    } else {
      const color = el.style?.color || el.getAttribute("color") || "";
      if (color && COLOR.test(color.trim())) (out as HTMLElement).style.color = color.trim();
    }

    el.childNodes.forEach((child) => copy(child, out));
    parent.appendChild(out);
  };

  source.body.childNodes.forEach((child) => copy(child, root));
  return root.innerHTML;
}
