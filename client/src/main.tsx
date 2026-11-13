import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import Stats from "./Stats";
import "./styles.css";

createRoot(document.getElementById("react-root")!).render(
  <StrictMode>
    <Stats />
  </StrictMode>
);
