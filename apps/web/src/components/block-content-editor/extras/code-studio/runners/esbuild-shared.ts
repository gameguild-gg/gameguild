import * as esbuild from 'esbuild-wasm'
import { loadCompressedWasm } from './wasm-loader'

let esbuildInitialized = false
let initializationPromise: Promise<void> | null = null

export async function initEsbuild() {
  // Se já inicializado, retorna imediatamente
  if (esbuildInitialized) {
    return
  }

  // Se está em processo de inicialização, aguarda a promise existente
  if (initializationPromise) {
    return initializationPromise
  }

  // Inicia nova inicialização
  initializationPromise = (async () => {
    try {
      const wasmBuffer = await loadCompressedWasm('/langs/esbuild.wasm.gz')
      await esbuild.initialize({
        wasmModule: await WebAssembly.compile(wasmBuffer),
      })
      esbuildInitialized = true
    } catch (error) {
      // Reset em caso de erro para permitir retry
      initializationPromise = null
      throw error
    }
  })()

  return initializationPromise
}

export { esbuild }
