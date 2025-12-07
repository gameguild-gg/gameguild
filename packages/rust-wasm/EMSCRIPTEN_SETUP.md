# Emscripten Setup Guide

Para compilar mrustc para WebAssembly, você precisa do Emscripten SDK.

## Instalação Rápida (Linux/macOS)

```bash
# 1. Clone o Emscripten SDK
cd ~
git clone https://github.com/emscripten-core/emsdk.git
cd emsdk

# 2. Instale a versão mais recente
./emsdk install latest

# 3. Ative o SDK
./emsdk activate latest

# 4. Configure as variáveis de ambiente (sessão atual)
source ./emsdk_env.sh

# 5. Configure permanentemente (adicione ao ~/.bashrc ou ~/.zshrc)
echo 'source "$HOME/emsdk/emsdk_env.sh"' >> ~/.bashrc
```

## Instalação (Windows)

```powershell
# 1. Clone o Emscripten SDK
cd C:\
git clone https://github.com/emscripten-core/emsdk.git
cd emsdk

# 2. Instale a versão mais recente
emsdk install latest

# 3. Ative o SDK
emsdk activate latest

# 4. Configure as variáveis de ambiente
emsdk_env.bat
```

## Verificar Instalação

```bash
emcc --version
# Deve mostrar algo como: emcc (Emscripten gcc/clang-like replacement) 3.1.50

em++ --version
# Deve mostrar a mesma versão
```

## Build do rust-wasm

Após instalar o Emscripten:

```bash
cd packages/rust-wasm
npm run build-runtime
```

## Troubleshooting

### "emcc: command not found"

Execute: `source ~/emsdk/emsdk_env.sh` (ou reabra o terminal)

### Build muito lento

mrustc é um projeto grande. Em máquinas com pouco RAM:
- Use `make -j2` em vez de `make -j$(nproc)`
- Aloque mais swap
- Considere usar alternativas (Rust Playground API)

### Build falha com erros de compilação

mrustc não foi projetado para WASM. Pode ser necessário:
- Aplicar patches manualmente
- Usar versão específica do Emscripten
- Contribuir com fixes upstream

## Alternativas (Recomendado)

Se o build falhar, considere:

1. **Rust Playground API** - Backend oficial do Rust
2. **@runno/wasi** - Binários Rust pré-compilados
3. **Server-side** - rustc local com API

Veja `ALTERNATIVES.md` para detalhes.
