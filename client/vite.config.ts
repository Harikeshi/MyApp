import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Собирает ровно два файла: assets/app.js и assets/app.css (их подключает ваш index.html).
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: "..src/MyApp.Web/wwwroot/app",          // <-- поправьте путь: папка wwwroot вашего ASP-проекта + /app
    emptyOutDir: true,
    cssCodeSplit: false,
    rollupOptions: {
      input: "src/main.tsx",           // без index.html: страницу отдаёт ваш существующий index.html
      output: {
        entryFileNames: "assets/app.js",
        assetFileNames: "assets/app[extname]",
        inlineDynamicImports: true,
      },
    },
  },
});
