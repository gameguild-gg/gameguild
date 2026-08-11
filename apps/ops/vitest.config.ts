import { defineConfig } from "vitest/config";

export default defineConfig({
  // ponytail: shared by T7 (API routes) and T6/T8 (other route tests). Vite 8's
  // oxc transform needs the source tsconfig to *include* test files; T1's
  // exclude was relaxed in tsconfig.json so oxc can resolve them. Node env —
  // API route handlers have no DOM. Add jsdom per-test via @vitest/environment
  // if a future UI test needs it.
  test: {
    environment: "node",
    include: ["src/**/*.test.ts", "src/**/*.test.tsx"],
    globals: false,
  },
});
