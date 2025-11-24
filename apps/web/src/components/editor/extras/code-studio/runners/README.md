# Code Runners

Sistema de execução de código 100% no navegador usando WebAssembly.

## Linguagens Implementadas

### ✅ JavaScript
- Engine: QuickJS (WASM)
- Sandbox: Completo
- Timeout: 30s (configurável)
- Tamanho: ~368KB (comprimido gzip)
- Fonte: `/wasm/quickjs-asyncify.wasm.gz`

### ✅ TypeScript
- Transpiler: esbuild (WASM)
- Engine: QuickJS (WASM)
- Sandbox: Completo
- Timeout: 30s (configurável)
- Tamanho: ~3.5MB (esbuild) + ~368KB (quickjs) (comprimidos gzip)
- Fonte: `/wasm/esbuild.wasm.gz` + `/wasm/quickjs-asyncify.wasm.gz`

### ✅ Python
- Engine: Pyodide (CPython 3.12 + WASM)
- Sandbox: Completo
- Timeout: 30s (configurável)
- Tamanho: ~2.7MB WASM (comprimido) + ~6MB runtime
- Fonte: `/wasm/pyodide.asm.wasm.gz` + `/pyodide/` (local)
- Features: stdlib completo, numpy, pandas (via micropip sob demanda)
- Versão: 0.26.4

## Arquivos WASM

Todos os arquivos são servidos compactados (gzip) e descompactados no navegador:

**`public/wasm/`:**
- `esbuild.wasm.gz` - 3.5MB (descompactado: ~12.9MB)
- `quickjs-asyncify.wasm.gz` - 369KB (descompactado: ~1MB)
- `pyodide.asm.wasm.gz` - 3.1MB (descompactado: ~9.6MB)

**`public/pyodide/`:**
- `pyodide.js.gz` - 5.8KB (descompactado: ~15KB)
- `pyodide.asm.js.gz` - 227KB (descompactado: ~1.2MB)

**Total: ~7.1MB compactado (71.2% de redução do original 24.7MB)**

Os arquivos são descompactados no cliente usando `pako` antes de serem executados.

### Atualizar WASM

Para atualizar todos os arquivos WASM (local + Pyodide do CDN):

```bash
npm run update-wasm
```

Este script:
1. Comprime WASM do `node_modules` (esbuild, quickjs)
2. Baixa Pyodide do CDN e comprime tudo
3. Salva em `public/wasm/` e `public/pyodide/`
4. Roda automaticamente no `postinstall`

## Uso

```typescript
import { UnifiedCodeRunner } from './runners'

const runner = new UnifiedCodeRunner({ timeout: 30000 })

// JavaScript
const jsResult = await runner.run('javascript', `
  console.log('Hello World')
  console.error('Error message')
`)

// Python
const pyResult = await runner.run('python', `
import sys
print('Python version:', sys.version)
print('Hello from Python!')
`)

console.log(result.stdout) // Output
console.log(result.stderr) // Errors
console.log(result.exitCode) // 0 or 1
console.log(result.executionTime) // ms

runner.dispose()
```

## Próximas Linguagens

- Lua (Wasmoon)
- C/C++ (Emscripten)
- C# (Blazor WASM)
- Rust (WASI)
