import { useState } from "react";
import { useNotes } from "./useNotes";

export default function Notes() {
  const n = useNotes();
  const [text, setText] = useState("");
  return (
    <div>
      <form onSubmit={(e) => { e.preventDefault(); void n.add(text); setText(""); }}>
        <input value={text} onChange={(e) => setText(e.target.value)} />
        <button disabled={!text.trim()}>Добавить</button>
      </form>
      {n.error && <p className="error">{n.error}</p>}
      <ul>
        {n.items.map((x) => (
          <li key={x.id}>{x.text} <button onClick={() => void n.remove(x.id)}>✕</button></li>
        ))}
      </ul>
    </div>
  );
}