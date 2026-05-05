# Virtual Filesystem

The VFS is owned by the kernel and exposed to every WASM process via a synchronous syscall bridge. Three backends are layered into a single mount tree.

## Layout

| Mount                  | Backend          | Behavior                                                         |
| ---------------------- | ---------------- | ---------------------------------------------------------------- |
| `/tmp`                 | IDBFS volatile   | In-memory `Map`, cleared on reload                               |
| `/home/user`           | IDBFS persistent | IndexedDB (`overlay-writes`, `user-files`) + write-through cache |
| `/usr`, `/lib`, `/etc` | LazyFS           | CDN-backed `.tar.br` bundles fetched on-demand                   |

Implementation: `packages/core/src/vfs/` (`idb.ts`, `lazy.ts`).

## Hijacking

- **Filesystem syscalls** (`open`, `read`, `write`, `stat`, …) are intercepted by a custom Emscripten FS implementation and routed to the kernel.
- **stdin / stdout / stderr** are replaced with callbacks so the shell can pipe processes and forward to xterm.
- All processes see the **same tree** — files written by one are visible to the next.

## LazyFS: bundle-based lazy loading

At build time `generate-manifest.ts` emits `manifest.json`:

```json
{
  "files": {
    "/usr/include/stdio.h": { "bundle": "crt0", "offset": 1024, "size": 512 }
  },
  "bundles": {
    "crt0": { "url": "/cdn/crt0.tar.br", "size": 2048 }
  }
}
```

On first access:

1. Look up the file in the manifest → resolve its bundle.
2. If the bundle is already unpacked (IndexedDB `lazyfs-cache-v3`) → return.
3. Otherwise fetch `/cdn/<bundle>.tar.br`, decompress (Brotli), batch-write entries to IndexedDB, then return the file.

After the first access, every other file in the same bundle is local. New manifest URLs invalidate the cache.

### Brotli decoder

LazyFS prefers `DecompressionStream("br")`. When unavailable (older Safari/Firefox) it falls back to a locally-built Emscripten brotli module shipped as `cdn/brotli_wasm.{js,wasm}`. There is **no `brotli-wasm` npm dependency**.

## Pure lazy — no preloading

The system never warms the cache or speculatively fetches. This guarantees:

- Fast startup (no blocking network I/O before the terminal appears)
- Minimal bandwidth (only what is touched is downloaded)
- Scales as the sysroot grows

The only exceptions are virtual filesystems (`/dev`, `/proc`) needed for process bookkeeping; they contain no user-accessible files.

## Async I/O via Asyncify

Emscripten POSIX APIs are synchronous, but LazyFS does network + IndexedDB work. Asyncify bridges the gap.

- Tools are compiled with `-sASYNCIFY -sASYNCIFY_STACK_SIZE=… -sASYNCIFY_IMPORTS=[…] -mno-reference-types`.
- The syscall handler returns a Promise when a fetch is needed.
- Asyncify unwinds the WASM stack, JS performs the I/O, Asyncify rewinds with the result.
- User code sees blocking semantics:

```c
FILE *f = fopen("/usr/include/stdio.h", "r"); // looks blocking
// internally: fetch + decompress + IDB write, then resume
```

Asyncify works in every modern browser (Safari and Firefox included). JSPI is **not** required.

> Do **not** use MEMFS as a cache layer for lazy-loaded files — it bypasses the Asyncify hooks and breaks suspension.

## I/O stream callbacks

| Stream | Callback                              | Purpose                                       |
| ------ | ------------------------------------- | --------------------------------------------- |
| stdin  | `async (size: number) => Uint8Array`  | Feed terminal input or upstream pipe data     |
| stdout | `(data: Uint8Array) => Promise<void>` | Routed to TTY / next process / capture buffer |
| stderr | `(data: Uint8Array) => Promise<void>` | Same as stdout but semantically separate      |

This decouples WASM processes from the browser and lets the kernel route I/O between processes, xterm, or pure capture buffers.
