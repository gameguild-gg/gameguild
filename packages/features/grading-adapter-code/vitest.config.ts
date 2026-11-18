import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  resolve: {
    alias: [
      {
        find: '@game-guild/coding/testing',
        replacement: fileURLToPath(new URL('../coding/src/testing/index.ts', import.meta.url)),
      },
      {
        find: '@game-guild/coding',
        replacement: fileURLToPath(new URL('../coding/src/index.ts', import.meta.url)),
      },
      {
        find: '@game-guild/grading',
        replacement: fileURLToPath(new URL('../grading/src/index.ts', import.meta.url)),
      },
    ],
  },
  test: {
    environment: 'node',
    include: ['src/**/*.{test,spec}.ts'],
  },
});
