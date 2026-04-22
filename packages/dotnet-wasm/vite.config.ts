import { defineConfig } from 'vite'
import { resolve } from 'path'
import { copyFileSync, mkdirSync, existsSync } from 'fs'

export default defineConfig({
  build: {
    lib: {
      entry: resolve(__dirname, 'src/index.ts'),
      name: 'DotnetWeb',
      formats: ['umd', 'es'],
      fileName: (format) => `dotnet-web.${format}.js`,
    },
    rollupOptions: {
      // Bundle everything, no externals
      external: [],
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
      const srcDir = resolve(__dirname, 'src/runtime')
      const managedDir = resolve(__dirname, 'public/managed')
      
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
          console.warn(`⚠ File not found: ${file}`)
        }
      }
    }
  }]
})
