import { defineConfig } from 'vite'
import { dirname, resolve } from 'path'
import { copyFileSync, mkdirSync, existsSync } from 'fs'
import { fileURLToPath } from 'url'

const packageRoot = dirname(fileURLToPath(import.meta.url))

export default defineConfig({
  build: {
    lib: {
      entry: resolve(packageRoot, 'src/index.ts'),
      name: 'DotnetWeb',
      formats: ['umd', 'es'],
      fileName: (format) => `dotnet-web.${format}.js`,
    },
    rollupOptions: {
      // Bundle everything, no externals
      external: [],
      output: {
        exports: 'named',
      },
    },
    target: 'esnext',
    minify: false,
  },
  server: {
    headers: {
      'Cross-Origin-Embedder-Policy': 'require-corp',
      'Cross-Origin-Opener-Policy': 'same-origin',
    },
    fs: {
      // Ignore .NET build output directories
      deny: ['**/dotnet-runtime/bin/**', '**/dotnet-runtime/obj/**'],
    },
  },
  // Exclude .NET build artifacts from being watched
  optimizeDeps: {
    exclude: ['dotnet-runtime'],
  },
  // Copy dotnet runtime files - including blazor.boot.json
  plugins: [{
    name: 'copy-dotnet-runtime',
    buildStart() {
      const srcDir = resolve(packageRoot, 'src/runtime')
      const managedDir = resolve(packageRoot, 'public/managed')

      // The lightweight package build is valid without the optional runtime.
      // `pnpm setup` creates this directory before requesting a runtime bundle.
      if (!existsSync(managedDir)) return
      
      if (!existsSync(srcDir)) {
        mkdirSync(srcDir, { recursive: true })
      }
      
      // Copy JS files and config to src
      const requiredFiles = [
        'dotnet.js',
        'dotnet.runtime.js',
        'dotnet.native.js',
        'blazor.boot.json'  // Copy config too!
      ]
      
      for (const file of requiredFiles) {
        const srcFile = resolve(managedDir, file)
        const destFile = resolve(srcDir, file)
        
        if (existsSync(srcFile)) {
          copyFileSync(srcFile, destFile)
          console.log(`✓ Copied ${file} to src/runtime/`)
        } else {
          throw new Error(`Incomplete .NET runtime: ${file} is missing from public/managed`)
        }
      }
    }
  }]
})
