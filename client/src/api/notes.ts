import { deleteJson, getJson, postJson } from "./http";

export interface Note { id: number; text: string; }

export const fetchNotes = (signal?: AbortSignal) => getJson<Note[]>("/notes", signal);
export const createNote = (text: string) => postJson<Note>("/notes", { text });
export const removeNote = (id: number) => deleteJson(`/notes/${id}`);