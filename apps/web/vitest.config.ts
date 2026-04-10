import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';

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
      '@': path.resolve(__dirname, './src'),
      '@game-guild/ui': path.resolve(__dirname, '../../packages/ui/src'),
      '@game-guild/client/react': path.resolve(
        __dirname,
        '../../packages/client/src/integrations/react/index.ts'
      ),
      '@game-guild/client': path.resolve(
        __dirname,
        '../../packages/client/src/index.ts'
      ),
    },
  },
});
