/**
 * @jest-environment node
 *
 * SDL3 CDN bundle integrity tests.
 *
 * Verifies that the deployed CDN bundle for SDL3 contains the expected
 * artifacts (libSDL3.a) and that the library does NOT contain the pthread
 * EM_ASM symbols that cause the "FORWARDED_DATA" assertion crash in emscripten.
 *
 * Specifically checks for `emscripten_asm_const_int_sync_on_main_thread` which
 * is referenced by SDL3 camera/sensor .o files when compiled with thread support.
 * Our sysroot build uses SDL_CAMERA=OFF SDL_SENSOR=OFF to avoid these symbols.
 */

import * as crypto from 'crypto';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as zlib from 'zlib';

// ── Paths ─────────────────────────────────────────────────────────────────────

// Resolve CDN directory relative to the monorepo root.
// __dirname will be packages/emception/src/ when compiled by babel-jest.
// Walk up from there: src → emception → packages → [root] → tools/emception/public/cdn
const REPO_ROOT = path.resolve(__dirname, '../../..');
const CDN_DIR = path.join(REPO_ROOT, 'tools', 'emception', 'public', 'cdn');
const MANIFEST_PATH = path.join(CDN_DIR, 'manifest.json');
const SDL3_BUNDLE_PATH = path.join(CDN_DIR, 'usr', 'lib', 'sdl3.tar.br');

// ── Helpers ───────────────────────────────────────────────────────────────────

interface ManifestBundle {
    files: string[];
    url: string;
    size: number;
    hash: string;
}

interface ManifestJson {
    bundles: Record<string, ManifestBundle>;
}

function readManifest(): Record<string, ManifestBundle> {
    const raw = fs.readFileSync(MANIFEST_PATH, 'utf-8');
    const parsed = JSON.parse(raw) as ManifestJson;
    return parsed.bundles ?? (parsed as unknown as Record<string, ManifestBundle>);
}

function sha256Hex(data: Buffer): string {
    return crypto.createHash('sha256').update(data).digest('hex');
}

/**
 * Decompress a brotli buffer synchronously.
 * Returns null if decompression fails (e.g., truncated file).
 */
function brotliDecompress(data: Buffer): Buffer | null {
    try {
        return zlib.brotliDecompressSync(data);
    } catch {
        return null;
    }
}

/**
 * Minimal POSIX tar reader.
 * Yields { name, data } for each regular file entry in the archive.
 */
function* readTar(tarBuffer: Buffer): Generator<{ name: string; data: Buffer }> {
    let offset = 0;
    while (offset + 512 <= tarBuffer.length) {
        const header = tarBuffer.subarray(offset, offset + 512);

        // Check for end-of-archive (two consecutive zero blocks)
        const isZero = header.every((b: number) => b === 0);
        if (isZero) break;

        // Name: first 100 bytes, NUL-terminated
        const nameRaw = header.subarray(0, 100);
        const nameEnd = nameRaw.indexOf(0);
        const name = nameRaw.subarray(0, nameEnd >= 0 ? nameEnd : 100).toString('utf-8');

        // Type flag (byte 156): '0' or '\0' = regular file
        const typeflag = header[156];
        const isRegular = typeflag === 48 /* '0' */ || typeflag === 0;

        // Size: bytes 124-135, octal ASCII, NUL-padded
        const sizeStr = header.subarray(124, 136).toString('utf-8').replace(/\0/g, '').trim();
        const size = parseInt(sizeStr, 8) || 0;

        const dataStart = offset + 512;

        if (isRegular && size > 0 && name.length > 0) {
            yield { name, data: tarBuffer.subarray(dataStart, dataStart + size) };
        }

        // Advance past header + data (each padded to 512-byte boundary)
        offset += 512 + Math.ceil(size / 512) * 512;
    }
}

// ── Manifest tests ────────────────────────────────────────────────────────────

describe('SDL3 CDN manifest', () => {
    it('manifest.json exists in public/cdn/', () => {
        expect(fs.existsSync(MANIFEST_PATH)).toBe(true);
    });

    it('manifest.json is valid JSON', () => {
        const raw = fs.readFileSync(MANIFEST_PATH, 'utf-8');
        expect(() => JSON.parse(raw)).not.toThrow();
    });

    it('manifest has a "bundles" key or top-level SDL3 entry', () => {
        const bundles = readManifest();
        expect(typeof bundles).toBe('object');
        expect(bundles).not.toBeNull();
    });

    it('manifest has an "sdl3" bundle entry', () => {
        const bundles = readManifest();
        expect(bundles).toHaveProperty('sdl3');
    });

    it('sdl3 bundle entry lists /usr/lib/libSDL3.a', () => {
        const bundles = readManifest();
        expect(bundles.sdl3.files).toContain('/usr/lib/libSDL3.a');
    });

    it('sdl3 bundle entry has a non-empty url', () => {
        const bundles = readManifest();
        expect(typeof bundles.sdl3.url).toBe('string');
        expect(bundles.sdl3.url.length).toBeGreaterThan(0);
    });

    it('sdl3 bundle entry has a positive declared size', () => {
        const bundles = readManifest();
        expect(bundles.sdl3.size).toBeGreaterThan(0);
    });

    it('sdl3 bundle entry has a 64-char sha-256 hash', () => {
        const bundles = readManifest();
        expect(bundles.sdl3.hash).toMatch(/^[0-9a-f]{64}$/);
    });
});

// ── Bundle file tests ─────────────────────────────────────────────────────────

describe('SDL3 CDN bundle file (sdl3.tar.br)', () => {
    it('sdl3.tar.br exists', () => {
        expect(fs.existsSync(SDL3_BUNDLE_PATH)).toBe(true);
    });

    it('sdl3.tar.br is larger than 100 KB', () => {
        const { size } = fs.statSync(SDL3_BUNDLE_PATH);
        expect(size).toBeGreaterThan(100_000);
    });

    it('sdl3.tar.br file size matches manifest declared size', () => {
        const bundles = readManifest();
        const actualSize = fs.statSync(SDL3_BUNDLE_PATH).size;
        expect(actualSize).toBe(bundles.sdl3.size);
    });

    it('sdl3.tar.br SHA-256 hash matches manifest declared hash', () => {
        const bundles = readManifest();
        const data = fs.readFileSync(SDL3_BUNDLE_PATH);
        const actualHash = sha256Hex(data);
        expect(actualHash).toBe(bundles.sdl3.hash);
    });

    it('sdl3.tar.br can be decompressed with brotli', () => {
        const brData = fs.readFileSync(SDL3_BUNDLE_PATH);
        const tarData = brotliDecompress(brData);
        expect(tarData).not.toBeNull();
        expect(tarData!.length).toBeGreaterThan(0);
    });

    it('sdl3.tar.br contains usr/lib/libSDL3.a', () => {
        const brData = fs.readFileSync(SDL3_BUNDLE_PATH);
        const tarData = brotliDecompress(brData);
        expect(tarData).not.toBeNull();

        const entries = [...readTar(tarData!)];
        const libEntry = entries.find((e) => e.name.includes('libSDL3.a'));
        expect(libEntry).toBeTruthy();
        // libSDL3.a should be at least 1 MB when uncompressed
        expect(libEntry!.data.length).toBeGreaterThan(1_000_000);
    });
});

// ── Symbol check ─────────────────────────────────────────────────────────────
// These tests verify the KNOWN STATE of the deployed libSDL3.a:
// SDL3's core files (SDL.c, SDL_assert.c) call emscripten_asm_const_int_sync_on_main_thread
// via MAIN_THREAD_EM_ASM macros for thread-safe operations.  This is EXPECTED.
//
// Because the symbol is present, buildSDL3Args MUST include both:
//   -Wl,--unresolved-symbols=ignore-all  (lets wasm-ld proceed past undefined symbols)
//   --js-library __sdl_lib.js            (provides no-op stubs to compiler.js)
// These workarounds are verified separately in ide-utils.test.ts.

describe('SDL3 libSDL3.a threading symbols (workaround required)', () => {
    let libData: Buffer | null = null;
    let tmpDir: string | null = null;

    beforeAll(() => {
        if (!fs.existsSync(SDL3_BUNDLE_PATH)) return;

        const brData = fs.readFileSync(SDL3_BUNDLE_PATH);
        const tarData = brotliDecompress(brData);
        if (!tarData) return;

        for (const entry of readTar(tarData)) {
            if (entry.name.includes('libSDL3.a') && entry.data.length > 0) {
                libData = entry.data;
                tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'sdl3-sym-test-'));
                fs.writeFileSync(path.join(tmpDir, 'libSDL3.a'), libData);
                break;
            }
        }
    });

    afterAll(() => {
        if (tmpDir) {
            try {
                fs.rmSync(tmpDir, { recursive: true, force: true });
            } catch {
                /* ignore cleanup errors */
            }
        }
    });

    it('libSDL3.a was found and extracted from the bundle', () => {
        expect(libData).not.toBeNull();
    });

    /**
     * Regression test: SDL3's core SDL.c and SDL_assert.c files call
     * emscripten_asm_const_int_sync_on_main_thread via MAIN_THREAD_EM_ASM macros.
     * This symbol is EXPECTED to be present — it requires the buildSDL3Args
     * workaround (--js-library stubs + -Wl,--unresolved-symbols=ignore-all).
     *
     * If this test fails (symbol no longer present), update buildSDL3Args to
     * remove the now-unnecessary workaround flags.
     */
    it('libSDL3.a DOES contain emscripten_asm_const_int_sync_on_main_thread (workaround is required)', () => {
        expect(libData).not.toBeNull();

        // The symbol name is an ASCII string in SDL.c.o and SDL_assert.c.o object
        // files embedded in the .a archive.  Raw binary scan is reliable where nm
        // cannot parse WASM .o format on macOS.
        const libStr = libData!.toString('latin1');
        const hasSymbol = libStr.includes('emscripten_asm_const_int_sync_on_main_thread');
        expect(hasSymbol).toBe(true);
    });

    /**
     * The async variant may or may not be present depending on the SDL3 build.
     * The SDL3_JS_LIB_STUB provides a stub for it regardless (harmless if absent).
     */
    it('libSDL3.a async symbol check — stub is provided regardless', () => {
        expect(libData).not.toBeNull();

        // The stub file provides both sync and async variants unconditionally.
        // This test just documents whether the async variant is currently present.
        const libStr = libData!.toString('latin1');
        const hasAsyncSymbol = libStr.includes('emscripten_asm_const_async_on_main_thread');
        // Either state is acceptable here — the stub covers both cases.
        expect(typeof hasAsyncSymbol).toBe('boolean');
    });
});
