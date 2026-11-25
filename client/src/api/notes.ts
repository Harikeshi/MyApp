import { deleteJson, getJson, postJson, putJson } from "./http";

export type NoteTag = "task" | "faq";

export const NOTE_TAGS: { value: NoteTag; label: string }[] = [
  { value: "task", label: "task" },
  { value: "faq", label: "faq" },
];

/** Строка списка: без содержимого. */
export interface NoteListItem {
  id: number;
  title: string;
  tag: NoteTag;
  updatedAt: string;
  preview: string;
}

export interface Note {
  id: number;
  title: string;
  tag: NoteTag;
  /** HTML записи: текст с цветом и картинки (data:-ссылки). */
  content: string;
  createdAt: string;
  updatedAt: string;
}

export interface NoteInput {
  title: string;
  tag: NoteTag;
  content: string;
}

export const fetchNotes = (signal?: AbortSignal) => getJson<NoteListItem[]>("/notes", signal);

export const fetchNote = (id: number, signal?: AbortSignal) => getJson<Note>(`/notes/${id}`, signal);

export const createNote = (input: NoteInput) => postJson<Note>("/notes", input);

export const updateNote = (id: number, input: NoteInput) => putJson<Note>(`/notes/${id}`, input);

export const removeNote = (id: number) => deleteJson(`/notes/${id}`);
