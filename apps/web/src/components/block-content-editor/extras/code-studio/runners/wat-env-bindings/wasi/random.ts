/**
 * WASI Random Interface (wasi:random)
 * Component Model standard interface for random number generation
 * 
 * @see https://github.com/WebAssembly/WASI/blob/main/preview2/random.wit
 */

export interface WasiRandom {
  'get-random-bytes': (len: bigint) => Uint8Array
  'get-random-u64': () => bigint
  'insecure-random': () => bigint
  'insecure-random-bytes': (len: bigint) => Uint8Array
}

export function createRandom(): WasiRandom {
  return {
    'get-random-bytes': (len: bigint) => {
      const bytes = new Uint8Array(Number(len))
      crypto.getRandomValues(bytes)
      return bytes
    },

    'get-random-u64': () => {
      const bytes = new Uint8Array(8)
      crypto.getRandomValues(bytes)
      return new DataView(bytes.buffer).getBigUint64(0, true)
    },

    'insecure-random': () => {
      return BigInt(Math.floor(Math.random() * Number.MAX_SAFE_INTEGER))
    },

    'insecure-random-bytes': (len: bigint) => {
      const bytes = new Uint8Array(Number(len))
      for (let i = 0; i < bytes.length; i++) {
        bytes[i] = Math.floor(Math.random() * 256)
      }
      return bytes
    },
  }
}
