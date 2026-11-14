import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Собирает ровно два файла: assets/app.js и assets/app.css (их подключает wwwroot/index.html).
export default defineConfig({
  plugins: [react()],
  build: {
    // Путь относительно папки client/. Раньше был "..src/..." (без «/»): сборка уходила в мусорную папку client/..src
    outDir: "../src/MyApp.Web/wwwroot/app",
    emptyOutDir: true,
    cssCodeSplit: false,
    rollupOptions: {
      input: "src/main.tsx",           // без index.html: страницу отдаёт существующий wwwroot/index.html
      output: {
        entryFileNames: "assets/app.js",
        assetFileNames: "assets/app[extname]",
        inlineDynamicImports: true,
      },
    },
  },
});
