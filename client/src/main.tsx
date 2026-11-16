import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import Stats from "./features/stats/Stats";
import Chats from "./features/chats/Chats";

createRoot(document.getElementById("react-root")!).render(
  <StrictMode>
    <Stats />
  </StrictMode>
);

createRoot(document.getElementById("chats-root")!).render(
  <StrictMode>
    <Chats />
  </StrictMode>
);
