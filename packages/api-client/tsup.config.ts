import { defineConfig } from 'tsup';

export default defineConfig({
  entry: {
    index: 'src/index.ts',
  },
  format: ['esm', 'cjs'],
  dts: true,
  sourcemap: true,
  clean: true,
  splitting: true,
  treeshake: true,
  target: 'es2022',
  external: ['react', 'next', '@tanstack/react-query'],
  esbuildOptions(options) {
    options.mainFields = ['module', 'main'];
    options.jsx = 'automatic';
  },
});
