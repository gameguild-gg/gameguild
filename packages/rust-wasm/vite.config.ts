import { defineConfig } from 'vite'
import { resolve } from 'path'

export default defineConfig({
  build: {
    lib: {
      entry: resolve(__dirname, 'src/index.ts'),
      name: 'RustWeb',
      formats: ['es', 'umd'],
      fileName: (format) => `rust-web.${format}.js`
    },
    rollupOptions: {
      external: [],
      output: {
        globals: {}
      }
    },
    outDir: 'dist',
    emptyOutDir: false // Don't clear dist folder (tsc generates .d.ts files there)
  }
})
