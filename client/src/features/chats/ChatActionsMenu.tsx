import { useLayoutEffect, useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";

const MENU_WIDTH = 200;
const GAP = 4;
const EDGE = 8;

export interface MenuItem {
  label: string;
  icon?: string;
  danger?: boolean;
  onClick: () => void;
}

interface Props {
  /** Положение кнопки «⋯», рядом с которой открывается меню. */
  anchor: DOMRect;
  items: MenuItem[];
  onClose: () => void;
}

/**
 * Всплывающее меню действий (над чатом или сообщением). Рисуется в document.body с position: fixed,
 * поэтому его не обрезает прокручиваемый список. Закрывается по клику мимо (прозрачная подложка), по Esc
 * и при изменении размера окна.
 */
export default function ChatActionsMenu({ anchor, items, onClose }: Props) {
  const ref = useRef<HTMLDivElement>(null);
  const [pos, setPos] = useState({
    top: anchor.bottom + GAP,
    left: Math.min(Math.max(EDGE, anchor.right - MENU_WIDTH), window.innerWidth - MENU_WIDTH - EDGE),
  });

  // если снизу не хватает места, открываем меню над кнопкой
  useLayoutEffect(() => {
    const height = ref.current?.offsetHeight ?? 0;
    if (anchor.bottom + GAP + height > window.innerHeight - EDGE) {
      setPos((p) => ({ ...p, top: Math.max(EDGE, anchor.top - GAP - height) }));
    }
  }, [anchor]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    window.addEventListener("resize", onClose);
    return () => {
      window.removeEventListener("keydown", onKey);
      window.removeEventListener("resize", onClose);
    };
  }, [onClose]);

  return createPortal(
    <>
      <div className="pop-backdrop" onClick={onClose} />
      <div className="pop-menu" ref={ref} role="menu" style={{ top: pos.top, left: pos.left, width: MENU_WIDTH }}>
        {items.map((item) => (
          <button
            key={item.label}
            type="button"
            role="menuitem"
            className={item.danger ? "pop-item pop-danger" : "pop-item"}
            onClick={() => {
              onClose();
              item.onClick();
            }}
          >
            {item.icon && <span aria-hidden="true">{item.icon}</span>}
            {item.label}
          </button>
        ))}
      </div>
    </>,
    document.body,
  );
}
