import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    globals: true,
    environment: 'node',
    include: ['tests/**/*.test.ts', 'tests/**/*.test.tsx'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      include: ['src/**/*.ts', 'src/**/*.tsx', 'scripts/**/*.ts'],
      exclude: [
        'src/generated/**',
        'src/**/*.d.ts',
        // CLI orchestrator: exercised through generated artifacts, not useful as unit coverage.
        'scripts/generate.ts',
        // Type-only files (emit no runtime JS)
        'src/runtime/client.ts',
        'src/runtime/auth/types.ts',
        'src/runtime/errors/types.ts',
        'src/runtime/transport/types.ts',
        'src/runtime/result/types.ts',
        'src/runtime/tenant/types.ts',
        'src/runtime/auth/providers/types.ts',
        // Barrel re-export files (no logic, just re-exports)
        'src/index.ts',
        'src/runtime/index.ts',
        'src/runtime/auth/index.ts',
        'src/runtime/auth/providers/index.ts',
        'src/runtime/deduplication/index.ts',
        'src/runtime/devtools/index.ts',
        'src/runtime/errors/index.ts',
        'src/runtime/result/index.ts',
        'src/runtime/tenant/index.ts',
        'src/runtime/transport/index.ts',
        'src/plugins/index.ts',
        'src/integrations/react/index.ts',
      ],
    },
  },
});
