import { useCallback, useEffect, useState } from "react";
import { errorMessage } from "../../api/http";
import { createNote, fetchNotes, removeNote, type Note } from "../../api/notes";

export function useNotes() {
  const [items, setItems] = useState<Note[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try { setItems(await fetchNotes()); setError(null); }
    catch (e) { setError(errorMessage(e)); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const add = async (text: string) => {
    try { await createNote(text); await load(); }
    catch (e) { setError(errorMessage(e)); }
  };
  const remove = async (id: number) => {
    try { await removeNote(id); await load(); }
    catch (e) { setError(errorMessage(e)); }
  };
  return { items, error, add, remove };
}