# WAT Runner Environment Bindings Reference

This document provides a comprehensive reference for all environment bindings available to WebAssembly modules running in the WAT Runner.

## Table of Contents

- [Overview](#overview)
- [Namespace: `env`](#namespace-env)
- [Namespace: `console`](#namespace-console)
- [Namespace: `Math`](#namespace-math)
- [Namespace: `Date`](#namespace-date)
- [Namespace: `performance`](#namespace-performance)
- [Namespace: `crypto`](#namespace-crypto)
- [Namespace: `String`](#namespace-string)
- [Namespace: `Object`](#namespace-object)
- [Namespace: `Reflect`](#namespace-reflect)
- [Namespace: `assembly/index`](#namespace-assemblyindex)
- [Namespace: `GOImports`](#namespace-goimports)
- [DOM Bindings](#dom-bindings)
- [Memory Helpers](#memory-helpers)
- [Usage Examples](#usage-examples)

---

## Overview

The WAT Runner provides a rich set of environment bindings that allow WebAssembly modules to interact with JavaScript functionality. These bindings are organized into logical namespaces matching common WebAssembly import conventions.

All bindings are automatically available when you run WebAssembly code through the WAT Runner. You don't need to manually configure anything - just import the functions you need in your WAT/WASM code.

---

## Namespace: `env`

The `env` namespace is the most commonly used import namespace. It provides core functionality needed by most WebAssembly modules.

### Memory

| Export | Type | Description |
|--------|------|-------------|
| `memory` | `WebAssembly.Memory` | Shared memory instance (initial: 256 pages, max: 512 pages) |

### Abort and Tracing

| Export | Signature | Description |
|--------|-----------|-------------|
| `abort` | `(msgPtr?, filePtr?, line?, col?) -> void` | Abort execution with error message |
| `seed` | `() -> f64` | Returns a random seed value |
| `trace` | `(msgPtr, n?, a0?, a1?, a2?, a3?, a4?) -> void` | Trace/debug output |

### Console Output (Numeric Types)

| Export | Signature | Description |
|--------|-----------|-------------|
| `print_i32` | `(value: i32) -> void` | Print 32-bit integer |
| `print_i64` | `(value: i64) -> void` | Print 64-bit integer |
| `print_f32` | `(value: f32) -> void` | Print 32-bit float |
| `print_f64` | `(value: f64) -> void` | Print 64-bit float |
| `print_char` | `(charCode: i32) -> void` | Print single character |
| `print_newline` | `() -> void` | Print newline |
| `print` | `(ptr: i32, len: i32) -> void` | Print string from memory |
| `println` | `(ptr: i32, len: i32) -> void` | Print string with newline |

### Error Output

| Export | Signature | Description |
|--------|-----------|-------------|
| `error_i32` | `(value: i32) -> void` | Print i32 to stderr |
| `error_i64` | `(value: i64) -> void` | Print i64 to stderr |
| `error_f32` | `(value: f32) -> void` | Print f32 to stderr |
| `error_f64` | `(value: f64) -> void` | Print f64 to stderr |
| `eprint` | `(ptr: i32, len: i32) -> void` | Print string to stderr |
| `eprintln` | `(ptr: i32, len: i32) -> void` | Print string to stderr with newline |

### Generic Logging

| Export | Signature | Description |
|--------|-----------|-------------|
| `log` | `(value: i32) -> void` | Log value with newline |
| `debug` | `(value: i32) -> void` | Debug log with prefix |

### Integer Math Operations

| Export | Signature | Description |
|--------|-----------|-------------|
| `abs_i32` | `(x: i32) -> i32` | Absolute value (32-bit) |
| `abs_i64` | `(x: i64) -> i64` | Absolute value (64-bit) |
| `min_i32` | `(a: i32, b: i32) -> i32` | Minimum (32-bit) |
| `min_i64` | `(a: i64, b: i64) -> i64` | Minimum (64-bit) |
| `max_i32` | `(a: i32, b: i32) -> i32` | Maximum (32-bit) |
| `max_i64` | `(a: i64, b: i64) -> i64` | Maximum (64-bit) |

### Float Math Operations

| Export | Signature | Description |
|--------|-----------|-------------|
| `abs_f32` | `(x: f32) -> f32` | Absolute value |
| `abs_f64` | `(x: f64) -> f64` | Absolute value |
| `min_f32` | `(a: f32, b: f32) -> f32` | Minimum |
| `min_f64` | `(a: f64, b: f64) -> f64` | Minimum |
| `max_f32` | `(a: f32, b: f32) -> f32` | Maximum |
| `max_f64` | `(a: f64, b: f64) -> f64` | Maximum |
| `ceil_f32` | `(x: f32) -> f32` | Ceiling |
| `ceil_f64` | `(x: f64) -> f64` | Ceiling |
| `floor_f32` | `(x: f32) -> f32` | Floor |
| `floor_f64` | `(x: f64) -> f64` | Floor |
| `trunc_f32` | `(x: f32) -> f32` | Truncate |
| `trunc_f64` | `(x: f64) -> f64` | Truncate |
| `round_f32` | `(x: f32) -> f32` | Round |
| `round_f64` | `(x: f64) -> f64` | Round |
| `sqrt_f32` | `(x: f32) -> f32` | Square root |
| `sqrt_f64` | `(x: f64) -> f64` | Square root |

### Trigonometric Functions

| Export | Signature | Description |
|--------|-----------|-------------|
| `sin` | `(x: f64) -> f64` | Sine |
| `cos` | `(x: f64) -> f64` | Cosine |
| `tan` | `(x: f64) -> f64` | Tangent |
| `asin` | `(x: f64) -> f64` | Arc sine |
| `acos` | `(x: f64) -> f64` | Arc cosine |
| `atan` | `(x: f64) -> f64` | Arc tangent |
| `atan2` | `(y: f64, x: f64) -> f64` | Arc tangent (2 args) |

### Exponential and Logarithmic

| Export | Signature | Description |
|--------|-----------|-------------|
| `exp` | `(x: f64) -> f64` | Exponential (e^x) |
| `log` | `(x: f64) -> f64` | Natural logarithm |
| `log10` | `(x: f64) -> f64` | Base-10 logarithm |
| `log2` | `(x: f64) -> f64` | Base-2 logarithm |
| `pow` | `(base: f64, exp: f64) -> f64` | Power (base^exp) |

### Time Functions

| Export | Signature | Description |
|--------|-----------|-------------|
| `now` | `() -> f64` | Current timestamp (ms since epoch) |
| `Date.now` | `() -> f64` | Same as `now` |

### Random

| Export | Signature | Description |
|--------|-----------|-------------|
| `random` | `() -> f64` | Random number [0, 1) |
| `Math.random` | `() -> f64` | Same as `random` |

### Constants

| Export | Type | Value | Description |
|--------|------|-------|-------------|
| `NaN` | `f64` | `NaN` | Not a Number |
| `Infinity` | `f64` | `Infinity` | Positive infinity |
| `Math.E` | `f64` | `2.718...` | Euler's number |
| `Math.PI` | `f64` | `3.14159...` | Pi |
| `Math.LN2` | `f64` | `0.693...` | Natural log of 2 |
| `Math.LN10` | `f64` | `2.302...` | Natural log of 10 |
| `Math.LOG2E` | `f64` | `1.442...` | Log₂(e) |
| `Math.LOG10E` | `f64` | `0.434...` | Log₁₀(e) |
| `Math.SQRT1_2` | `f64` | `0.707...` | √(1/2) |
| `Math.SQRT2` | `f64` | `1.414...` | √2 |

---

## Namespace: `console`

Console logging functions for debugging.

| Export | Signature | Description |
|--------|-----------|-------------|
| `log` | `(ptr: i32, len?: i32) -> void` | Log message |
| `debug` | `(text: string) -> void` | Debug message |
| `info` | `(text: string) -> void` | Info message |
| `warn` | `(text: string) -> void` | Warning message |
| `error` | `(text: string) -> void` | Error message |
| `assert` | `(condition: bool, msg: string) -> void` | Assert condition |
| `time` | `(label: string) -> void` | Start timer |
| `timeLog` | `(label: string) -> void` | Log timer |
| `timeEnd` | `(label: string) -> void` | End timer |

---

## Namespace: `Math`

Complete JavaScript Math API.

### Constants

All constants from the `env` namespace are also available here:
- `E`, `LN2`, `LN10`, `LOG2E`, `LOG10E`, `PI`, `SQRT1_2`, `SQRT2`

### Methods

| Export | Signature | Description |
|--------|-----------|-------------|
| `abs` | `(x: f64) -> f64` | Absolute value |
| `acos` | `(x: f64) -> f64` | Arc cosine |
| `acosh` | `(x: f64) -> f64` | Hyperbolic arc cosine |
| `asin` | `(x: f64) -> f64` | Arc sine |
| `asinh` | `(x: f64) -> f64` | Hyperbolic arc sine |
| `atan` | `(x: f64) -> f64` | Arc tangent |
| `atan2` | `(y: f64, x: f64) -> f64` | Arc tangent (2 args) |
| `atanh` | `(x: f64) -> f64` | Hyperbolic arc tangent |
| `cbrt` | `(x: f64) -> f64` | Cube root |
| `ceil` | `(x: f64) -> f64` | Ceiling |
| `clz32` | `(x: f64) -> f64` | Count leading zeros |
| `cos` | `(x: f64) -> f64` | Cosine |
| `cosh` | `(x: f64) -> f64` | Hyperbolic cosine |
| `exp` | `(x: f64) -> f64` | Exponential (e^x) |
| `expm1` | `(x: f64) -> f64` | e^x - 1 |
| `floor` | `(x: f64) -> f64` | Floor |
| `fround` | `(x: f64) -> f32` | Round to float32 |
| `hypot` | `(x: f64, y: f64) -> f64` | Hypotenuse |
| `imul` | `(a: f64, b: f64) -> f64` | Integer multiply |
| `ln` | `(x: f64) -> f64` | Natural logarithm |
| `log10` | `(x: f64) -> f64` | Base-10 logarithm |
| `log1p` | `(x: f64) -> f64` | log(1 + x) |
| `log2` | `(x: f64) -> f64` | Base-2 logarithm |
| `max` | `(a: f64, b: f64) -> f64` | Maximum |
| `min` | `(a: f64, b: f64) -> f64` | Minimum |
| `pow` | `(base: f64, exp: f64) -> f64` | Power |
| `random` | `() -> f64` | Random [0, 1) |
| `round` | `(x: f64) -> f64` | Round |
| `sign` | `(x: f64) -> f64` | Sign (-1, 0, 1) |
| `sin` | `(x: f64) -> f64` | Sine |
| `sinh` | `(x: f64) -> f64` | Hyperbolic sine |
| `sqrt` | `(x: f64) -> f64` | Square root |
| `tan` | `(x: f64) -> f64` | Tangent |
| `tanh` | `(x: f64) -> f64` | Hyperbolic tangent |
| `trunc` | `(x: f64) -> f64` | Truncate |

---

## Namespace: `Date`

| Export | Signature | Description |
|--------|-----------|-------------|
| `now` | `() -> f64` | Current timestamp (ms) |

---

## Namespace: `performance`

| Export | Signature | Description |
|--------|-----------|-------------|
| `now` | `() -> f64` | High-resolution timestamp |

---

## Namespace: `crypto`

| Export | Signature | Description |
|--------|-----------|-------------|
| `getRandomValues` | `(ptr: i32, len: i32, mem: Memory) -> void` | Fill array with random bytes |
| `getRandomValuesN` | `(n: i32) -> Uint8Array` | Create array of n random bytes |

---

## Namespace: `String`

| Export | Signature | Description |
|--------|-----------|-------------|
| `fromCharCode` | `(code: i32) -> string` | Create string from char code |
| `fromCodePoint` | `(point: i32) -> string` | Create string from code point |
| `fromCodePoints` | `(...points: i32[]) -> string` | Create string from code points |

---

## Namespace: `Object`

| Export | Signature | Description |
|--------|-----------|-------------|
| `is` | `(a: any, b: any) -> bool` | Check object equality |
| `keys` | `(obj: any) -> any[]` | Get object keys |
| `values` | `(obj: any) -> any[]` | Get object values |
| `entries` | `(obj: any) -> any[]` | Get object entries |

---

## Namespace: `Reflect`

| Export | Signature | Description |
|--------|-----------|-------------|
| `get` | `(target: any, key: string) -> any` | Get property |
| `has` | `(target: any, key: string) -> bool` | Check property |
| `set` | `(target: any, key: string, val: any) -> bool` | Set property |

---

## Namespace: `assembly/index`

AssemblyScript-specific runtime hooks.

| Export | Signature | Description |
|--------|-----------|-------------|
| `abort` | `(msg, file, line, col) -> void` | Abort with context |
| `trace` | `(msg, n, ...args) -> void` | Trace message |
| `seed` | `() -> f64` | Random seed |

---

## Namespace: `GOImports`

Go WebAssembly runtime bindings.

| Export | Signature | Description |
|--------|-----------|-------------|
| `runtime.wasmExit` | `(code: i32) -> void` | Exit with code |
| `runtime.wasmWrite` | `(fd: i32, ptr: i32, len: i32) -> void` | Write to file descriptor |
| `runtime.nanotime` | `() -> i64` | Nanosecond timestamp |
| `runtime.walltime` | `() -> i64` | Wall clock time |
| `runtime.scheduleCallback` | `() -> i32` | Schedule callback |
| `runtime.clearScheduledCallback` | `() -> void` | Clear callback |
| `runtime.getRandomData` | `(ptr: i32, len: i32) -> void` | Get random data |

---

## DOM Bindings

Limited DOM bindings for sandbox environment. Mostly logging stubs.

### `document`

| Export | Signature | Description |
|--------|-----------|-------------|
| `createElement` | `(tag: string) -> object` | Create element (stub) |
| `getElementById` | `(id: string) -> object` | Get element (stub) |
| `write` | `(content: string) -> void` | Write to document |
| `writeln` | `(content: string) -> void` | Write line to document |

---

## Memory Helpers

Utility functions for reading/writing memory (available in TypeScript, not directly importable by WASM).

### String Operations

- `readCString(ptr, maxLen)` - Read null-terminated string
- `readString(ptr, len)` - Read string with known length
- `writeString(ptr, str, maxLen?)` - Write string to memory

### Numeric Operations

- `readI32(ptr)`, `writeI32(ptr, val)` - 32-bit signed int
- `readU32(ptr)`, `writeU32(ptr, val)` - 32-bit unsigned int
- `readI64(ptr)`, `writeI64(ptr, val)` - 64-bit signed int
- `readU64(ptr)`, `writeU64(ptr, val)` - 64-bit unsigned int
- `readF32(ptr)`, `writeF32(ptr, val)` - 32-bit float
- `readF64(ptr)`, `writeF64(ptr, val)` - 64-bit float

---

## Usage Examples

### Example 1: Basic Math (WAT)

```wat
(module
  (import "env" "print_f64" (func $print (param f64)))
  (import "Math" "sqrt" (func $sqrt (param f64) (result f64)))
  (import "Math" "PI" (global $PI f64))
  
  (func $main (export "main")
    ;; Calculate sqrt(PI)
    global.get $PI
    call $sqrt
    call $print
  )
)
```

### Example 2: Console Output (WAT)

```wat
(module
  (import "env" "print_i32" (func $print (param i32)))
  (import "env" "print_newline" (func $newline))
  
  (func $main (export "main")
    ;; Print numbers 1-10
    (local $i i32)
    (local.set $i (i32.const 1))
    (block $break
      (loop $continue
        local.get $i
        call $print
        call $newline
        
        local.get $i
        i32.const 1
        i32.add
        local.set $i
        
        local.get $i
        i32.const 11
        i32.lt_s
        br_if $continue
      )
    )
  )
)
```

### Example 3: Time and Random (WAT)

```wat
(module
  (import "Date" "now" (func $now (result f64)))
  (import "Math" "random" (func $random (result f64)))
  (import "env" "print_f64" (func $print (param f64)))
  
  (func $main (export "main")
    ;; Print current timestamp
    call $now
    call $print
    
    ;; Print random number
    call $random
    call $print
  )
)
```

### Example 4: Importing from Another WAT Module

```wat
;; File: math.wat
(module
  (func (export "add") (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.add
  )
)
```

```wat
;; File: main.wat
(module
  (import "math" "add" (func $add (param i32) (param i32) (result i32)))
  (import "env" "print_i32" (func $print (param i32)))
  
  (func $main (export "main")
    i32.const 10
    i32.const 20
    call $add
    call $print
  )
)
```

---

## Notes

1. **Memory**: A shared memory instance is automatically provided. Initial size is 256 pages (16MB), maximum is 512 pages (32MB).

2. **String Encoding**: Strings use UTF-8 encoding when read from or written to memory.

3. **Module Imports**: When using multiple WAT files, modules are registered under multiple naming conventions:
   - Full path without extension: `/wat/math.wat` → `/wat/math`
   - Filename without extension: `/wat/math.wat` → `math`

4. **WASI Support**: WASI snapshot preview1 is automatically available for modules that need it.

5. **Error Handling**: Runtime errors are captured and reported in stderr. Use the `abort` function for controlled error handling.

6. **Performance**: High-resolution timestamps are available via `performance.now()` for benchmarking.

---

## Support Matrix

| Language/Compiler | Compatibility | Notes |
|-------------------|---------------|-------|
| WAT (Text Format) | ✅ Full | Native support |
| AssemblyScript | ✅ Full | Use `assembly/index` imports |
| Rust (wasm32) | ✅ Good | Use WASI or custom bindings |
| C/C++ (emscripten) | ⚠️ Partial | May need emscripten imports |
| C/C++ (wasi-sdk) | ✅ Good | WASI support included |
| Go (TinyGo) | ✅ Good | GOImports namespace |
| Go (standard) | ⚠️ Limited | May need additional JS glue |

---

## Extending the Environment

To add custom bindings:

1. Edit `wat-env-bindings.ts`
2. Add your functions to the appropriate namespace creator function
3. Update this documentation

Example:

```typescript
// In createEnvBindings()
return {
  // ... existing bindings ...
  
  myCustomFunction: (value: number) => {
    console.log('Custom:', value)
    return value * 2
  }
}
```

Then use in WAT:

```wat
(import "env" "myCustomFunction" (func $custom (param i32) (result i32)))
```
