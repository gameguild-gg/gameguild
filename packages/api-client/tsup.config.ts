import { defineConfig } from 'tsup';

export default defineConfig({
  entry: {
    index: 'src/index.ts',
    'integrations/next/index': 'src/integrations/next/index.ts',
    'integrations/react/index': 'src/integrations/react/index.ts',
    'plugins/index': 'src/plugins/index.ts',
  },
  format: ['esm', 'cjs'],
  dts: {
    entry: {
      index: 'src/index.ts',
      'integrations/next/index': 'src/integrations/next/index.ts',
      // Skip DTS for React integration - needs React Query v5 API updates
      // 'integrations/react/index': 'src/integrations/react/index.ts',
      'plugins/index': 'src/plugins/index.ts',
    },
  },
  sourcemap: true,
  clean: true,
  splitting: true,
  treeshake: true,
  target: 'es2022',
  external: ['react', 'next', 'next-auth'],
  esbuildOptions(options) {
    options.mainFields = ['module', 'main'];
  },
});
