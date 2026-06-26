/**
 * Memory management helpers
 */
export function createMemoryHelpers(memory: WebAssembly.Memory) {
  const textDecoder = new TextDecoder('utf-8')
  const textEncoder = new TextEncoder()

  return {
    // Read a null-terminated string from memory
    readCString: (ptr: number, maxLength: number = 1024): string => {
      const view = new Uint8Array(memory.buffer)
      let length = 0
      while (length < maxLength && view[ptr + length] !== 0) {
        length++
      }
      return textDecoder.decode(new Uint8Array(memory.buffer, ptr, length))
    },

    // Read a string with known length
    readString: (ptr: number, length: number): string => {
      return textDecoder.decode(new Uint8Array(memory.buffer, ptr, length))
    },

    // Write a string to memory
    writeString: (ptr: number, str: string, maxLength?: number): number => {
      const bytes = textEncoder.encode(str)
      const length = maxLength ? Math.min(bytes.length, maxLength) : bytes.length
      const view = new Uint8Array(memory.buffer, ptr, length)
      view.set(bytes.slice(0, length))
      return length
    },

    // Read various numeric types
    readI32: (ptr: number) => new Int32Array(memory.buffer, ptr, 1)[0],
    readU32: (ptr: number) => new Uint32Array(memory.buffer, ptr, 1)[0],
    readI64: (ptr: number) => new BigInt64Array(memory.buffer, ptr, 1)[0],
    readU64: (ptr: number) => new BigUint64Array(memory.buffer, ptr, 1)[0],
    readF32: (ptr: number) => new Float32Array(memory.buffer, ptr, 1)[0],
    readF64: (ptr: number) => new Float64Array(memory.buffer, ptr, 1)[0],

    // Write various numeric types
    writeI32: (ptr: number, value: number) => (new Int32Array(memory.buffer, ptr, 1)[0] = value),
    writeU32: (ptr: number, value: number) => (new Uint32Array(memory.buffer, ptr, 1)[0] = value),
    writeI64: (ptr: number, value: bigint) =>
      (new BigInt64Array(memory.buffer, ptr, 1)[0] = value),
    writeU64: (ptr: number, value: bigint) =>
      (new BigUint64Array(memory.buffer, ptr, 1)[0] = value),
    writeF32: (ptr: number, value: number) => (new Float32Array(memory.buffer, ptr, 1)[0] = value),
    writeF64: (ptr: number, value: number) => (new Float64Array(memory.buffer, ptr, 1)[0] = value),
  }
}
