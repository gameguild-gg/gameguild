/**
 * WebAssembly Environment Bindings for WAT Runner
 * 
 * This file provides a comprehensive set of environment imports that WebAssembly
 * modules (compiled from WAT, AssemblyScript, C, Rust, etc.) can use.
 * 
 * Organized into logical namespaces matching common WebAssembly import conventions.
 */

/**
 * Creates the complete environment import object for WebAssembly modules
 */
export function createWatEnvironment(
  stdout: (text: string) => void,
  stderr: (text: string) => void,
  memory?: WebAssembly.Memory
): Record<string, any> {
  // Initialize shared memory if not provided
  const sharedMemory = memory || new WebAssembly.Memory({ initial: 256, maximum: 512 })

  return {
    env: createEnvBindings(stdout, stderr, sharedMemory),
    console: createConsoleBindings(stdout, stderr),
    Math: createMathBindings(),
    Date: createDateBindings(),
    performance: createPerformanceBindings(),
    crypto: createCryptoBindings(),
    String: createStringBindings(),
    Object: createObjectBindings(),
    Reflect: createReflectBindings(),
    // AssemblyScript-specific
    'assembly/index': createAssemblyScriptBindings(stdout, stderr),
    // Additional common namespaces
    wasi_unstable: {}, // Placeholder for WASI compatibility
    GOImports: createGoBindings(stdout, stderr), // For Go-compiled WASM
  }
}

/**
 * Core environment bindings (env namespace)
 * Most commonly used by all WebAssembly modules
 */
function createEnvBindings(
  stdout: (text: string) => void,
  stderr: (text: string) => void,
  memory: WebAssembly.Memory
): Record<string, any> {
  // Text decoder/encoder for string operations
  const textDecoder = new TextDecoder('utf-8')
  const textEncoder = new TextEncoder()

  // Helper to read string from memory
  const readString = (ptr: number, len: number): string => {
    const bytes = new Uint8Array(memory.buffer, ptr, len)
    return textDecoder.decode(bytes)
  }

  // Helper to write string to memory
  const writeString = (ptr: number, str: string): number => {
    const bytes = textEncoder.encode(str)
    const view = new Uint8Array(memory.buffer, ptr, bytes.length)
    view.set(bytes)
    return bytes.length
  }

  return {
    // Memory
    memory,

    // Abort function (used by many runtimes)
    abort: (
      messagePtr?: number,
      fileNamePtr?: number,
      line?: number,
      column?: number
    ) => {
      const message = messagePtr ? readString(messagePtr, 1024) : 'abort called'
      const fileName = fileNamePtr ? readString(fileNamePtr, 1024) : 'unknown'
      const errorMsg = `Abort: ${message} at ${fileName}:${line ?? 0}:${column ?? 0}`
      stderr(errorMsg)
      throw new Error(errorMsg)
    },

    // Seed for random number generation
    seed: () => Math.random(),

    // Tracing (for debugging)
    trace: (
      msgPtr: number,
      n?: number,
      a0?: number,
      a1?: number,
      a2?: number,
      a3?: number,
      a4?: number
    ) => {
      const msg = readString(msgPtr, 256)
      stdout(`[TRACE] ${msg} n=${n ?? 0} args=[${a0 ?? 0}, ${a1 ?? 0}, ${a2 ?? 0}, ${a3 ?? 0}, ${a4 ?? 0}]`)
    },

    // Console output functions (various numeric types)
    print_i32: (value: number) => stdout(value.toString()),
    print_i64: (value: bigint) => stdout(value.toString()),
    print_f32: (value: number) => stdout(value.toString()),
    print_f64: (value: number) => stdout(value.toString()),
    print_char: (charCode: number) => stdout(String.fromCharCode(charCode)),
    print_newline: () => stdout('\n'),
    print: (ptr: number, len: number) => stdout(readString(ptr, len)),
    println: (ptr: number, len: number) => stdout(readString(ptr, len) + '\n'),

    // Error output
    error_i32: (value: number) => stderr(value.toString()),
    error_i64: (value: bigint) => stderr(value.toString()),
    error_f32: (value: number) => stderr(value.toString()),
    error_f64: (value: number) => stderr(value.toString()),
    eprint: (ptr: number, len: number) => stderr(readString(ptr, len)),
    eprintln: (ptr: number, len: number) => stderr(readString(ptr, len) + '\n'),

    // Generic logging
    log: (value: number) => stdout(value.toString() + '\n'),
    debug: (value: number) => stdout(`[DEBUG] ${value}\n`),

    // Math operations (for environments that don't have native support)
    abs_i32: (x: number) => Math.abs(x) | 0,
    abs_i64: (x: bigint) => x < 0n ? -x : x,
    abs_f32: (x: number) => Math.abs(x),
    abs_f64: (x: number) => Math.abs(x),
    
    min_i32: (a: number, b: number) => Math.min(a, b) | 0,
    min_i64: (a: bigint, b: bigint) => a < b ? a : b,
    min_f32: (a: number, b: number) => Math.min(a, b),
    min_f64: (a: number, b: number) => Math.min(a, b),
    
    max_i32: (a: number, b: number) => Math.max(a, b) | 0,
    max_i64: (a: bigint, b: bigint) => a > b ? a : b,
    max_f32: (a: number, b: number) => Math.max(a, b),
    max_f64: (a: number, b: number) => Math.max(a, b),

    // Float operations
    ceil_f32: (x: number) => Math.ceil(x),
    ceil_f64: (x: number) => Math.ceil(x),
    floor_f32: (x: number) => Math.floor(x),
    floor_f64: (x: number) => Math.floor(x),
    trunc_f32: (x: number) => Math.trunc(x),
    trunc_f64: (x: number) => Math.trunc(x),
    round_f32: (x: number) => Math.round(x),
    round_f64: (x: number) => Math.round(x),
    sqrt_f32: (x: number) => Math.sqrt(x),
    sqrt_f64: (x: number) => Math.sqrt(x),

    // Trigonometric functions
    sin: (x: number) => Math.sin(x),
    cos: (x: number) => Math.cos(x),
    tan: (x: number) => Math.tan(x),
    asin: (x: number) => Math.asin(x),
    acos: (x: number) => Math.acos(x),
    atan: (x: number) => Math.atan(x),
    atan2: (y: number, x: number) => Math.atan2(y, x),

    // Exponential and logarithmic
    exp: (x: number) => Math.exp(x),
    ln: (x: number) => Math.log(x),
    log10: (x: number) => Math.log10(x),
    log2: (x: number) => Math.log2(x),
    pow: (base: number, exp: number) => Math.pow(base, exp),

    // Time
    now: () => Date.now(),
    'Date.now': () => Date.now(),

    // Random
    random: () => Math.random(),
    'Math.random': () => Math.random(),

    // Global constants
    NaN: NaN,
    Infinity: Infinity,
    'Math.E': Math.E,
    'Math.PI': Math.PI,
    'Math.LN2': Math.LN2,
    'Math.LN10': Math.LN10,
    'Math.LOG2E': Math.LOG2E,
    'Math.LOG10E': Math.LOG10E,
    'Math.SQRT1_2': Math.SQRT1_2,
    'Math.SQRT2': Math.SQRT2,

    // String operations helpers
    readString,
    writeString,
  }
}

/**
 * Console bindings (console namespace)
 */
function createConsoleBindings(
  stdout: (text: string) => void,
  stderr: (text: string) => void
): Record<string, any> {
  return {
    log: (ptr: number, len?: number) => {
      // If len is provided, it's a pointer to string data
      // Otherwise, treat ptr as a numeric value
      if (len !== undefined && len > 0) {
        stdout('[LOG] (string output not yet implemented)')
      } else {
        stdout(`[LOG] ${ptr}`)
      }
    },
    
    debug: (text: string) => stdout(`[DEBUG] ${text}`),
    info: (text: string) => stdout(`[INFO] ${text}`),
    warn: (text: string) => stderr(`[WARN] ${text}`),
    error: (text: string) => stderr(`[ERROR] ${text}`),
    
    assert: (condition: boolean, message: string) => {
      if (!condition) {
        stderr(`[ASSERT FAILED] ${message}`)
        throw new Error(`Assertion failed: ${message}`)
      }
    },
    
    time: (label: string) => console.time(label),
    timeLog: (label: string) => console.timeLog(label),
    timeEnd: (label: string) => console.timeEnd(label),
  }
}

/**
 * Math bindings (Math namespace)
 * Complete set of JavaScript Math functions
 */
function createMathBindings(): Record<string, any> {
  return {
    // Constants
    E: Math.E,
    LN2: Math.LN2,
    LN10: Math.LN10,
    LOG2E: Math.LOG2E,
    LOG10E: Math.LOG10E,
    PI: Math.PI,
    SQRT1_2: Math.SQRT1_2,
    SQRT2: Math.SQRT2,

    // Methods
    abs: (x: number) => Math.abs(x),
    acos: (x: number) => Math.acos(x),
    acosh: (x: number) => Math.acosh(x),
    asin: (x: number) => Math.asin(x),
    asinh: (x: number) => Math.asinh(x),
    atan: (x: number) => Math.atan(x),
    atan2: (y: number, x: number) => Math.atan2(y, x),
    atanh: (x: number) => Math.atanh(x),
    cbrt: (x: number) => Math.cbrt(x),
    ceil: (x: number) => Math.ceil(x),
    clz32: (x: number) => Math.clz32(x),
    cos: (x: number) => Math.cos(x),
    cosh: (x: number) => Math.cosh(x),
    exp: (x: number) => Math.exp(x),
    expm1: (x: number) => Math.expm1(x),
    floor: (x: number) => Math.floor(x),
    fround: (x: number) => Math.fround(x),
    hypot: (x: number, y: number) => Math.hypot(x, y),
    imul: (a: number, b: number) => Math.imul(a, b),
    ln: (x: number) => Math.log(x),
    log10: (x: number) => Math.log10(x),
    log1p: (x: number) => Math.log1p(x),
    log2: (x: number) => Math.log2(x),
    max: (a: number, b: number) => Math.max(a, b),
    min: (a: number, b: number) => Math.min(a, b),
    pow: (base: number, exponent: number) => Math.pow(base, exponent),
    random: () => Math.random(),
    round: (x: number) => Math.round(x),
    sign: (x: number) => Math.sign(x),
    sin: (x: number) => Math.sin(x),
    sinh: (x: number) => Math.sinh(x),
    sqrt: (x: number) => Math.sqrt(x),
    tan: (x: number) => Math.tan(x),
    tanh: (x: number) => Math.tanh(x),
    trunc: (x: number) => Math.trunc(x),
  }
}

/**
 * Date bindings (Date namespace)
 */
function createDateBindings(): Record<string, any> {
  return {
    now: () => Date.now(),
  }
}

/**
 * Performance bindings (performance namespace)
 */
function createPerformanceBindings(): Record<string, any> {
  return {
    now: () => performance.now(),
  }
}

/**
 * Crypto bindings (crypto namespace)
 */
function createCryptoBindings(): Record<string, any> {
  return {
    getRandomValues: (arrayPtr: number, length: number, memory: WebAssembly.Memory) => {
      const array = new Uint8Array(memory.buffer, arrayPtr, length)
      crypto.getRandomValues(array)
    },
    
    getRandomValuesN: (n: number) => {
      const array = new Uint8Array(n)
      crypto.getRandomValues(array)
      return array
    },
  }
}

/**
 * String bindings (String namespace)
 */
function createStringBindings(): Record<string, any> {
  return {
    fromCharCode: (charCode: number) => String.fromCharCode(charCode),
    fromCodePoint: (codePoint: number) => String.fromCodePoint(codePoint),
    fromCodePoints: (...codePoints: number[]) => String.fromCodePoint(...codePoints),
  }
}

/**
 * Object bindings (Object namespace)
 */
function createObjectBindings(): Record<string, any> {
  return {
    is: (a: any, b: any) => Object.is(a, b),
    keys: (obj: any) => Object.keys(obj),
    values: (obj: any) => Object.values(obj),
    entries: (obj: any) => Object.entries(obj),
  }
}

/**
 * Reflect bindings (Reflect namespace)
 */
function createReflectBindings(): Record<string, any> {
  return {
    get: (target: any, propertyKey: string) => Reflect.get(target, propertyKey),
    has: (target: any, propertyKey: string) => Reflect.has(target, propertyKey),
    set: (target: any, propertyKey: string, value: any) => Reflect.set(target, propertyKey, value),
  }
}

/**
 * AssemblyScript-specific bindings
 */
function createAssemblyScriptBindings(
  stdout: (text: string) => void,
  stderr: (text: string) => void
): Record<string, any> {
  return {
    // AssemblyScript runtime hooks
    abort: (message: any, fileName: any, line: number, column: number) => {
      stderr(`AssemblyScript abort: ${message} at ${fileName}:${line}:${column}`)
      throw new Error(`Abort: ${message}`)
    },

    trace: (message: any, n: number, ...args: number[]) => {
      stdout(`[TRACE] ${message} n=${n} args=${args.join(', ')}`)
    },

    seed: () => Date.now(),
  }
}

/**
 * Go WebAssembly bindings (GOImports namespace)
 * For WebAssembly compiled from Go
 */
function createGoBindings(
  stdout: (text: string) => void,
  stderr: (text: string) => void
): Record<string, any> {
  return {
    // Go's WebAssembly runtime expects specific imports
    'runtime.wasmExit': (code: number) => {
      stdout(`\n[Exit code: ${code}]`)
    },
    
    'runtime.wasmWrite': (fd: number, ptr: number, len: number) => {
      // fd: 1 = stdout, 2 = stderr
      const output = fd === 1 ? stdout : stderr
      output(`[Go write to fd=${fd}, len=${len}]`)
    },

    'runtime.nanotime': () => BigInt(Date.now() * 1000000),
    'runtime.walltime': () => BigInt(Date.now()),
    
    'runtime.scheduleCallback': () => 0,
    'runtime.clearScheduledCallback': () => {},
    'runtime.getRandomData': (ptr: number, len: number) => {},
  }
}

/**
 * DOM-like bindings (for AssemblyScript DOM features)
 * Limited implementation for sandbox environment
 */
export function createDOMBindings(
  stdout: (text: string) => void,
  stderr: (text: string) => void
): Record<string, any> {
  return {
    globalThis: {},
    
    // Document stubs
    document: {
      createElement: (tagName: string) => {
        stdout(`[DOM] createElement: ${tagName}`)
        return {}
      },
      getElementById: (id: string) => {
        stdout(`[DOM] getElementById: ${id}`)
        return null
      },
      write: (content: string) => stdout(`[Document.write] ${content}`),
      writeln: (content: string) => stdout(`[Document.writeln] ${content}\n`),
    },
  }
}

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
    writeI32: (ptr: number, value: number) => new Int32Array(memory.buffer, ptr, 1)[0] = value,
    writeU32: (ptr: number, value: number) => new Uint32Array(memory.buffer, ptr, 1)[0] = value,
    writeI64: (ptr: number, value: bigint) => new BigInt64Array(memory.buffer, ptr, 1)[0] = value,
    writeU64: (ptr: number, value: bigint) => new BigUint64Array(memory.buffer, ptr, 1)[0] = value,
    writeF32: (ptr: number, value: number) => new Float32Array(memory.buffer, ptr, 1)[0] = value,
    writeF64: (ptr: number, value: number) => new Float64Array(memory.buffer, ptr, 1)[0] = value,
  }
}
