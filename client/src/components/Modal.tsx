import { useEffect, type ReactNode } from "react";
import { createPortal } from "react-dom";
import "./modal.css";

interface Props {
  title: string;
  onClose: () => void;
  children: ReactNode;
  /** Кнопки внизу окна. */
  footer?: ReactNode;
}

/** Окно с формой по центру экрана. Закрывается по Esc и кнопкам; клик по затемнению не закрывает (не потерять набранное). */
export default function Modal({ title, onClose, children, footer }: Props) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);

  return createPortal(
    <div className="modal-backdrop">
      <div className="modal" role="dialog" aria-modal="true" aria-label={title}>
        <h3>{title}</h3>
        <div className="modal-body">{children}</div>
        {footer && <div className="modal-actions">{footer}</div>}
      </div>
    </div>,
    document.body,
  );
}
