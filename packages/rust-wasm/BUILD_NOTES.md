# mrustc WASM Build Notes

## Build Status

Building mrustc to WASM is experimental. Current challenges:

### Known Issues

1. **File System Operations**
   - mrustc expects POSIX file system
   - Need to use Emscripten FS or WASI
   - Virtual filesystem for source files

2. **C++ Standard Library**
   - Some C++ features limited in WASM
   - `std::thread` not available
   - `std::filesystem` needs emulation

3. **Process Spawning**
   - mrustc may try to spawn subprocesses
   - Not possible in browser WASM
   - Need to inline all operations

### Required Patches

Create file: `patches/mrustc-wasm-full.patch`

```patch
# Disable file system access, use in-memory FS
# Disable process spawning
# Use Emscripten threading APIs
# Adapt path operations for WASI
```

### Build Configuration

Emscripten flags needed:

```bash
-s ALLOW_MEMORY_GROWTH=1    # Dynamic memory
-s TOTAL_MEMORY=512MB       # Initial heap
-s MODULARIZE=1             # ES6 module
-s EXPORT_ES6=1             # Export format
-s EXPORTED_FUNCTIONS=[...]  # Expose compile functions
-s EXPORTED_RUNTIME_METHODS=['ccall','cwrap']
-s FORCE_FILESYSTEM=1       # Enable FS
-s WASM=1                   # Target WASM
```

### Alternative: Simplified mrustc-wasm

Consider forking mrustc and creating a simplified version:

**mrustc-wasm goals:**
- Remove file I/O (use in-memory)
- Single-threaded only
- Inline all compilation stages
- Minimal stdlib (no std::filesystem)
- Direct WASM output (skip LLVM)

## Testing the Build

After successful build:

```javascript
// Test in browser console
const result = await window.RustCompiler.compile(`
fn main() {
    println!("Hello from mrustc!");
}
`)

console.log(result)
```

## Fallback Plan

If mrustc build continues to fail:

1. **Stage 1**: Use rustc locally, cache WASM outputs
2. **Stage 2**: Create minimal Rust interpreter in WASM
3. **Stage 3**: Port critical mrustc parts to TypeScript/WASM

## Resources

- [mrustc GitHub](https://github.com/thepowersgang/mrustc)
- [Emscripten Docs](https://emscripten.org/docs/porting/files.html)
- [WASI Filesystem](https://github.com/WebAssembly/WASI/blob/main/phases/snapshot/docs.md)
