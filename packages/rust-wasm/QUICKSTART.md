# 🚀 Quick Start - Rust WASM

## Próximos Passos

### 1️⃣ Instalar Emscripten (5-10 min)

```bash
cd /home/miguel/Projects/gameguild-gg/gameguild/packages/rust-wasm
./setup-emscripten.sh
```

Pressione `y` quando perguntado, aguarde o download (~600MB).

### 2️⃣ Ativar Emscripten na sessão atual

```bash
source ~/emsdk/emsdk_env.sh
```

**Dica:** Adicione ao `~/.bashrc` para ativar automaticamente:
```bash
echo 'source "$HOME/emsdk/emsdk_env.sh" 2>/dev/null' >> ~/.bashrc
```

### 3️⃣ Verificar instalação

```bash
emcc --version
# Deve mostrar: emcc (Emscripten gcc/clang-like replacement) 3.x.x
```

### 4️⃣ Build do Mock Compiler (~10 seg)

```bash
npm run build-mock
```

Isso cria `/public/rust/mrustc.js` e `mrustc.wasm` - um compilador mock para testes.

### 5️⃣ Build do Package TypeScript

```bash
npm run build
```

### 6️⃣ Testar no navegador

```bash
cd ../../apps/web
npm run dev
```

Abra http://localhost:3000, vá para Code Studio, selecione Rust e teste:

```rust
fn main() {
    println!("Hello from Rust!");
}
```

## O que esperar?

### ✅ Com Mock Compiler (agora)
- Valida sintaxe básica (tem `fn main()`?)
- Detecta `println!` e extrai conteúdo
- Retorna output simulado
- Perfeito para testar infraestrutura

### 🔄 Com mrustc Real (futuro)
- Compilação real de Rust → WASM
- Todas as features da linguagem
- Standard library completa
- Execução real no browser

## Estrutura Criada

```
packages/rust-wasm/
├── src/
│   ├── index.ts              ← RustCompiler class
│   ├── types.ts              ← Interfaces
│   └── rust/
│       ├── runtime-loader.ts ← Carrega WASM
│       ├── compiler.ts       ← Wrapper compilação
│       └── executor.ts       ← Executa WASM
├── rust-runtime/
│   ├── mrustc-mock.cpp       ← Mock compiler
│   ├── wasm-wrapper.cpp      ← Wrapper mrustc real
│   ├── Makefile.mock         ← Build mock
│   └── Makefile.wasm         ← Build real
├── public/rust/
│   ├── mrustc.js             ← Gerado pelo build
│   └── mrustc.wasm           ← Gerado pelo build
├── build-rust.sh             ← Build mrustc real
├── setup-emscripten.sh       ← Instala Emscripten
└── package.json

apps/web/src/components/editor/extras/code-studio/runners/
├── rust-runner.ts            ← Runner integrado
├── wasm-loader.ts            ← +loadRustFilesystem()
└── index.ts                  ← +RustRunner
```

## Comandos Úteis

```bash
# Build apenas mock (rápido)
npm run build-mock

# Build mrustc real (lento, experimental)
npm run build-runtime

# Build package TS
npm run build

# Setup completo
npm run setup

# Limpar artifacts
npm run clean-artifacts

# Type check
npm run type-check
```

## Troubleshooting

### "em++: command not found"
→ Execute: `source ~/emsdk/emsdk_env.sh`

### "mrustc build failed"
→ Normal! mrustc real é experimental. Use mock por enquanto.

### Mock funciona mas real não
→ Esperado. Mock é estável, real precisa desenvolvimento.

## Arquitetura

```
[Code Studio] → [RustRunner] → [RustCompiler]
                                      ↓
                           [RuntimeLoader] → [main.js]
                                                  ↓
                                           [mrustc.wasm]
                                                  ↓
                                    [compile_rust() C++ function]
                                                  ↓
                                         [Parse & Execute]
                                                  ↓
                                      [Return: SUCCESS/ERROR]
```

## Próximas Iterações

1. ✅ **Fase 1: Mock** ← Você está aqui
   - Infraestrutura completa
   - Validação básica
   - Testes de integração

2. ⏳ **Fase 2: mrustc Simples**
   - Compilar expressões simples
   - Sem std library
   - Apenas tipos primitivos

3. ⏳ **Fase 3: mrustc Completo**
   - Standard library
   - Modules
   - Crates

4. ⏳ **Fase 4: Produção**
   - Performance
   - Caching
   - Error handling

## Foco Local

✅ Tudo roda no browser  
✅ Zero dependências de backend  
✅ Sem APIs externas  
✅ Offline-first  

Começamos com mock para validar toda a pipeline, depois iteramos no compilador real!
