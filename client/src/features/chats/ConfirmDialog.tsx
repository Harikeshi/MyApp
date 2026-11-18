import { useEffect } from "react";
import { createPortal } from "react-dom";

interface Props {
  title: string;
  message: string;
  confirmLabel: string;
  onConfirm: () => void;
  onCancel: () => void;
}

/** Всплывающее окно подтверждения по центру экрана. Esc и клик по затемнению отменяют. */
export default function ConfirmDialog({ title, message, confirmLabel, onConfirm, onCancel }: Props) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onCancel();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onCancel]);

  return createPortal(
    <div className="dlg-backdrop" onClick={onCancel}>
      <div
        className="dlg"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="dlg-title"
        aria-describedby="dlg-text"
        onClick={(e) => e.stopPropagation()}
      >
        <h3 id="dlg-title">{title}</h3>
        <p id="dlg-text">{message}</p>
        <div className="dlg-actions">
          <button type="button" className="dlg-btn" autoFocus onClick={onCancel}>
            Отмена
          </button>
          <button type="button" className="dlg-btn dlg-danger" onClick={onConfirm}>
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
