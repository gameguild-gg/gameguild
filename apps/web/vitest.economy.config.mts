import react from '@vitejs/plugin-react';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vitest/config';

const rootDir = path.dirname(fileURLToPath(import.meta.url));
const reportsDirectory = process.env.ECONOMY_WEB_COVERAGE_DIR ?? 'coverage/economy';

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: [
      'src/lib/economy/**/*.test.{ts,tsx}',
      'src/lib/marketplace/**/*.test.{ts,tsx}',
      'src/lib/ads/google-ad-manager-web-rewarded-adapter.test.ts',
      'src/components/economy/**/*.test.{ts,tsx}',
      'src/components/marketplace/**/*.test.{ts,tsx}',
    ],
    server: {
      deps: {
        inline: ['next-intl'],
      },
    },
    coverage: {
      provider: 'v8',
      reportsDirectory,
      reporter: ['text', 'json', 'json-summary', 'cobertura'],
      include: [
        'src/lib/economy/**/*.ts',
        'src/lib/marketplace/**/*.ts',
        'src/lib/ads/google-ad-manager-web-rewarded-adapter.ts',
        'src/components/economy/**/*.{ts,tsx}',
        'src/components/marketplace/**/*.{ts,tsx}',
      ],
      exclude: ['**/*.test.*'],
      thresholds: {
        lines: 100,
        branches: 100,
        functions: 100,
        statements: 100,
      },
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(rootDir, './src'),
      '@game-guild/ui': path.resolve(rootDir, '../../packages/infrastructure/ui/src'),
      '@game-guild/client/react': path.resolve(
        rootDir,
        '../../packages/infrastructure/client/src/integrations/react/index.ts',
      ),
      '@game-guild/client/next': path.resolve(
        rootDir,
        '../../packages/infrastructure/client/src/integrations/next/index.ts',
      ),
      '@game-guild/client': path.resolve(rootDir, '../../packages/infrastructure/client/src/index.ts'),
      'emception/testing': path.resolve(
        rootDir,
        '../../tools/emception/packages/core/src/testing/index.ts',
      ),
      emception: path.resolve(rootDir, '../../tools/emception/packages/core/src/index.ts'),
    },
  },
});
