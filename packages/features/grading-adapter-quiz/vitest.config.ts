import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

export default defineConfig({
  resolve: {
    alias: {
      "@game-guild/grading": fileURLToPath(new URL("../grading/src/index.ts", import.meta.url)),
      "@game-guild/quiz": fileURLToPath(new URL("../quiz/src/index.ts", import.meta.url)),
    },
  },
  test: {
    environment: "node",
    include: ["src/**/*.{test,spec}.ts"],
  },
});
