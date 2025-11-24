# Build Scripts

## update-wasm.mjs

Atualiza os arquivos WASM compactados usados pelos code runners.

### Uso

```bash
npm run update-wasm
```

### O que faz

1. Copia arquivos WASM dos `node_modules`:
   - `esbuild-wasm/esbuild.wasm` → `public/wasm/esbuild.wasm.gz`
   - `@jitl/quickjs-wasmfile-release-asyncify/dist/emscripten-module.wasm` → `public/wasm/quickjs-asyncify.wasm.gz`

2. Comprime cada arquivo com gzip nível 9 (máxima compressão)

3. Exibe estatísticas de compressão

### Quando executar

- Após atualizar `esbuild-wasm` ou `quickjs-emscripten`
- Antes de fazer deploy em produção
- Quando adicionar novos runners que precisam de WASM

### Adicionar novos arquivos WASM

Edite o array `WASM_FILES` no script:

```javascript
const WASM_FILES = [
  {
    name: 'nome-exibicao',
    source: 'node_modules/pacote/arquivo.wasm',
    output: 'arquivo.wasm.gz',
  },
]
```
