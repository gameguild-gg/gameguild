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

## Arquivos WASM

Os arquivos WASM são servidos compactados (gzip) da pasta `public/wasm/`:
- `esbuild.wasm.gz` - 3.5MB (descompactado: ~11MB)
- `quickjs-asyncify.wasm.gz` - 368KB (descompactado: ~1.2MB)

São descompactados no cliente usando `pako` antes de serem compilados.

## Uso

```typescript
import { UnifiedCodeRunner } from './runners'

const runner = new UnifiedCodeRunner({ timeout: 30000 })

const result = await runner.run('javascript', `
  console.log('Hello World')
  console.error('Error message')
`)

console.log(result.stdout) // "Hello World"
console.log(result.stderr) // "Error message"
console.log(result.exitCode) // 0
console.log(result.executionTime) // 1.23

runner.dispose()
```

## Próximas Linguagens

- Python (Pyodide)
- Lua (Wasmoon)
- C/C++ (Emscripten)
- C# (Blazor WASM)
- Rust (WASI)
