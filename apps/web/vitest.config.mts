import react from '@vitejs/plugin-react';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vitest/config';

const rootDir = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.{test,spec}.{js,ts,jsx,tsx}'],
    exclude: ['src/**/*.{e2e,e2e.test}.{js,ts,jsx,tsx}'],
  },
  resolve: {
    alias: {
      '@': path.resolve(rootDir, './src'),
      '@game-guild/ui': path.resolve(rootDir, '../../packages/infrastructure/ui/src'),
      '@game-guild/client/react': path.resolve(
        rootDir,
        '../../packages/infrastructure/client/src/integrations/react/index.ts',
      ),
      '@game-guild/client': path.resolve(rootDir, '../../packages/infrastructure/client/src/index.ts'),
    },
  },
});
