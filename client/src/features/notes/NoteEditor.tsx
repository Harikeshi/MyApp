import { useEffect, useLayoutEffect, useRef, useState, type ClipboardEvent, type DragEvent } from "react";
import { errorMessage } from "../../api/http";
import { NOTE_TAGS, type Note, type NoteInput, type NoteTag } from "../../api/notes";
import ConfirmDialog from "../chats/ConfirmDialog";
import { imageToDataUrl } from "./images";
import { sanitizeHtml } from "./sanitize";

const COLORS = ["#1c2330", "#c62828", "#e65100", "#1b7f3b", "#2457d6", "#7b1fa2", "#667085"];
const MAX_CONTENT = 9_000_000; // сервер принимает до 10 000 000 символов

interface Props {
  /** null — новая запись. */
  initial: Note | null;
  onSave: (input: NoteInput) => Promise<void>;
  onCancel: () => void;
  /** Сообщает, есть ли несохранённые изменения (вкладка спрашивает подтверждение при уходе). */
  onDirtyChange: (dirty: boolean) => void;
}

/**
 * Форма записи: название, тег и текст с цветом и картинками. Пока не нажато «Сохранить», запись только правится
 * в форме и в базу не попадает. Форматирование делает встроенный contenteditable (document.execCommand).
 */
export default function NoteEditor({ initial, onSave, onCancel, onDirtyChange }: Props) {
  const [title, setTitle] = useState(initial?.title ?? "");
  const [tag, setTag] = useState<NoteTag>(initial?.tag ?? "task");
  const [dirty, setDirtyState] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [confirmCancel, setConfirmCancel] = useState(false);

  const editorRef = useRef<HTMLDivElement>(null);
  const fileRef = useRef<HTMLInputElement>(null);
  const rangeRef = useRef<Range | null>(null); // выделение внутри текста: кнопки панели его не теряют

  const setDirty = (value: boolean) => {
    setDirtyState(value);
    onDirtyChange(value);
  };

  useLayoutEffect(() => {
    if (editorRef.current) editorRef.current.innerHTML = sanitizeHtml(initial?.content ?? "");
    document.execCommand("styleWithCSS", false, "true"); // цвет пишется как style="color", а не <font>
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // закрыли страницу с несохранённой записью: браузер переспросит
  useEffect(() => {
    if (!dirty) return;
    const warn = (e: BeforeUnloadEvent) => e.preventDefault();
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  useEffect(() => () => onDirtyChange(false), [onDirtyChange]);

  const rememberSelection = () => {
    const sel = window.getSelection();
    if (sel && sel.rangeCount > 0 && editorRef.current?.contains(sel.anchorNode)) rangeRef.current = sel.getRangeAt(0).cloneRange();
  };

  /** Возвращает курсор в текст (в конец, если там ещё ничего не выделяли). */
  const restoreSelection = () => {
    const editor = editorRef.current;
    if (!editor) return;
    editor.focus();
    const sel = window.getSelection();
    if (!sel) return;
    sel.removeAllRanges();
    if (rangeRef.current) {
      sel.addRange(rangeRef.current);
    } else {
      const range = document.createRange();
      range.selectNodeContents(editor);
      range.collapse(false);
      sel.addRange(range);
    }
  };

  const exec = (command: string, value?: string) => {
    restoreSelection();
    document.execCommand(command, false, value);
    rememberSelection();
    setDirty(true);
  };

  const insertImages = async (files: File[]) => {
    setError(null);
    for (const file of files) {
      try {
        exec("insertImage", await imageToDataUrl(file));
      } catch (e) {
        setError(e instanceof Error ? e.message : "Не удалось добавить картинку");
      }
    }
  };

  const imagesOf = (list: FileList | null | undefined) => Array.from(list ?? []).filter((f) => f.type.startsWith("image/"));

  const onPaste = (e: ClipboardEvent<HTMLDivElement>) => {
    const images = imagesOf(e.clipboardData.files);
    e.preventDefault();
    if (images.length > 0) void insertImages(images);
    else exec("insertText", e.clipboardData.getData("text/plain")); // чужое оформление не тащим
  };

  const onDrop = (e: DragEvent<HTMLDivElement>) => {
    const images = imagesOf(e.dataTransfer.files);
    if (images.length === 0) return;
    e.preventDefault();
    void insertImages(images);
  };

  const save = async () => {
    if (busy || !title.trim()) return;
    const content = sanitizeHtml(editorRef.current?.innerHTML ?? "");
    if (content.length > MAX_CONTENT) {
      setError("Запись слишком большая: уберите часть картинок.");
      return;
    }

    setBusy(true);
    setError(null);
    try {
      await onSave({ title: title.trim(), tag, content });
      setDirty(false);
    } catch (e) {
      setError(errorMessage(e));
      setBusy(false);
    }
  };

  const cancel = () => (dirty ? setConfirmCancel(true) : onCancel());

  // кнопки панели не отнимают у текста фокус и выделение
  const keepFocus = (e: { preventDefault: () => void }) => e.preventDefault();

  return (
    <div className="nt-editor">
      <div className="nt-edit-head">
        <input
          type="text"
          className="nt-title-input"
          value={title}
          maxLength={200}
          placeholder="Название записи"
          autoFocus={initial === null}
          onChange={(e) => {
            setTitle(e.target.value);
            setDirty(true);
          }}
        />
        <select
          value={tag}
          aria-label="Тег"
          onChange={(e) => {
            setTag(e.target.value as NoteTag);
            setDirty(true);
          }}
        >
          {NOTE_TAGS.map((t) => (
            <option key={t.value} value={t.value}>
              {t.label}
            </option>
          ))}
        </select>
      </div>

      <div className="nt-toolbar" role="toolbar" aria-label="Форматирование">
        <button type="button" title="Жирный" onMouseDown={keepFocus} onClick={() => exec("bold")}>
          <b>Ж</b>
        </button>
        <button type="button" title="Курсив" onMouseDown={keepFocus} onClick={() => exec("italic")}>
          <i>К</i>
        </button>
        <button type="button" title="Подчёркнутый" onMouseDown={keepFocus} onClick={() => exec("underline")}>
          <u>Ч</u>
        </button>
        <button type="button" title="Зачёркнутый" onMouseDown={keepFocus} onClick={() => exec("strikeThrough")}>
          <s>З</s>
        </button>
        <button type="button" title="Маркированный список" onMouseDown={keepFocus} onClick={() => exec("insertUnorderedList")}>
          • —
        </button>
        <button type="button" title="Нумерованный список" onMouseDown={keepFocus} onClick={() => exec("insertOrderedList")}>
          1.
        </button>
        <span className="nt-sep" />
        {COLORS.map((c) => (
          <button
            key={c}
            type="button"
            className="nt-swatch"
            title="Цвет текста"
            aria-label={`Цвет ${c}`}
            style={{ background: c }}
            onMouseDown={keepFocus}
            onClick={() => exec("foreColor", c)}
          />
        ))}
        <input
          type="color"
          className="nt-color"
          title="Другой цвет"
          aria-label="Другой цвет текста"
          defaultValue="#2457d6"
          onFocus={rememberSelection}
          onChange={(e) => exec("foreColor", e.target.value)}
        />
        <span className="nt-sep" />
        <button type="button" title="Вставить картинку" onMouseDown={keepFocus} onClick={() => fileRef.current?.click()}>
          🖼 Картинка
        </button>
        <input
          ref={fileRef}
          type="file"
          accept="image/*"
          multiple
          hidden
          onChange={(e) => {
            const files = imagesOf(e.target.files);
            e.target.value = "";
            if (files.length > 0) void insertImages(files);
          }}
        />
      </div>

      <div
        ref={editorRef}
        className="nt-content nt-surface"
        contentEditable
        suppressContentEditableWarning
        role="textbox"
        aria-multiline="true"
        aria-label="Текст записи"
        onInput={() => setDirty(true)}
        onKeyUp={rememberSelection}
        onMouseUp={rememberSelection}
        onBlur={rememberSelection}
        onPaste={onPaste}
        onDrop={onDrop}
      />

      {error && (
        <p className="nt-error" role="alert">
          {error}
        </p>
      )}

      <div className="nt-actions">
        <button type="button" className="btn btn-primary" disabled={busy || !title.trim()} onClick={() => void save()}>
          {busy ? "Сохраняю…" : "Сохранить"}
        </button>
        <button type="button" className="btn" disabled={busy} onClick={cancel}>
          Отмена
        </button>
        {dirty && <span className="nt-unsaved">Есть несохранённые изменения</span>}
      </div>

      {confirmCancel && (
        <ConfirmDialog
          title="Отменить изменения?"
          message="Всё, что вы изменили в этой записи, будет потеряно."
          confirmLabel="Отменить"
          onCancel={() => setConfirmCancel(false)}
          onConfirm={() => {
            setConfirmCancel(false);
            setDirty(false);
            onCancel();
          }}
        />
      )}
    </div>
  );
}
