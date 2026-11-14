import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import Stats from "./features/stats/Stats";

createRoot(document.getElementById("react-root")!).render(
  <StrictMode>
    <Stats />
  </StrictMode>
);
