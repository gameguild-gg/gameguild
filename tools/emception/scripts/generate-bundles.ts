/**
 * Generate .tar.br bundle archives from the CDN file tree.
 *
 * Reads build/cdn/manifest.json (produced by generate-manifest.ts), groups files
 * into bundles, creates tar archives, compresses them with brotli, writes the
 * bundles into build/cdn/, and rewrites manifest.json with bundle metadata.
 *
 * EVERY file must belong to a bundle -- no unbundled individual downloads.
 *
 * Bundle groups:
 *   tool bundles        -- paired .mjs + .wasm at /usr/lib/tool.mjs,.wasm
 *   cache-crt           -- CRT startup objects (crt1.o, crtbegin.o, etc.)
 *   cache-core          -- default linker libs (libc, libc++, etc.)
 *   cache-libcxx-variants -- non-default libc++/libc++abi/libunwind variants
 *   cache-libc-variants -- non-default libc/allocator/compiler_rt variants
 *   cache-gl-variants   -- libGL emulation variants (100+ tiny files)
 *   cache-wasmfs        -- libwasmfs variants
 *   cache-sanitizers    -- ASan/LSan/UBSan runtime libraries
 *   cache-debug         -- debug-mode variants of all families (libc++-debug, libc-debug, etc.)
 *   cache-misc          -- embind, fetch, sqlite3, zlib, thinlto, etc.
 *   usr-include         -- all files under /usr/include/
 *   emscripten-core     -- all files under /usr/lib/emscripten/ (excl. cache-lib)
 *   python-runtime      -- all files under /usr/lib/python + pkgconfig + libpython
 *   usr-bin             -- /usr/bin/ + /etc/ (small scripts and config)
 *   libcurl             -- /usr/lib/libcurl (static library)
 *   sdl3               -- SDL3 core static lib + headers (via emsdk port)
 *   imgui              -- Dear ImGui static lib + headers
 *   usr-share           -- /usr/share/ files
 *   home               -- /home/ (emscripten port cache pre-seed)
 *
 * Version-specific paths like /usr/lib/python3.13/ are referenced in the
 * manifest as-is but the bundle is named "python-runtime" (not "python3.13").
 */

import * as crypto from 'crypto';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { fileURLToPath } from 'url';
import * as zlib from 'zlib';
import { createDeterministicTar } from './lib/deterministic-tar.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = process.cwd();
const OUTPUT_DIR = process.env.OUTPUT_DIR || path.join(ROOT, 'build', 'cdn');
const MANIFEST_FILE = process.env.MANIFEST_FILE || path.join(ROOT, 'build', 'cdn', 'manifest.json');

enableBuildKeepalive('generate-bundles');

// ──────────────────── helpers ────────────────────

/** SHA-256 hex hash of a buffer. */
function sha256(data: Buffer): string {
  return crypto.createHash('sha256').update(data).digest('hex');
}

/** Compress a buffer with brotli using Node.js zlib. */
function brotliCompress(data: Buffer, outPath: string): Promise<Buffer> {
  return new Promise((resolve, reject) => {
    zlib.brotliCompress(
      data,
      {
        params: {
          [zlib.constants.BROTLI_PARAM_QUALITY]: 11,
          // P4: 16 MB back-reference window (RFC 7932 max = 24 bits).
          // Node.js default is 22 bits (4 MB).  Large cache bundles have
          // pattern repetitions spanning >4 MB, so this saves ~5–8 MB.
          [zlib.constants.BROTLI_PARAM_LGWIN]: 24,
        },
      },
      (err, result) => {
        if (err) return reject(err);
        try {
          fs.writeFileSync(outPath, result);
          resolve(result);
        } catch (writeErr) {
          reject(writeErr);
        }
      },
    );
  });
}

/**
 * Run async jobs concurrently with a maximum of `concurrency` at a time.
 * Returns results in the same order as the input jobs array.
 */
async function runParallel<T>(jobs: (() => Promise<T>)[], concurrency: number): Promise<T[]> {
  const results: T[] = new Array(jobs.length);
  let nextIndex = 0;

  async function worker() {
    while (true) {
      const idx = nextIndex++;
      if (idx >= jobs.length) return;
      results[idx] = await jobs[idx]();
    }
  }

  const workers = Array.from({ length: Math.min(concurrency, jobs.length) }, () => worker());
  await Promise.all(workers);
  return results;
}

// ──────────────────── main ────────────────────

interface ManifestFile {
  size?: number;
  hash?: string;
  executable?: boolean;
  priority?: string;
  symlink?: string;
  bundle?: string;
}

interface BundleSpec {
  files: string[];
  url: string;
  size: number;
  hash: string;
}

interface Manifest {
  version: number;
  generated: string;
  baseUrl: string;
  files: Record<string, ManifestFile>;
  bundles: Record<string, BundleSpec>;
}

async function main() {
  console.log('Generating bundles...');
  console.log(`Manifest: ${MANIFEST_FILE}`);
  console.log(`CDN dir:  ${OUTPUT_DIR}`);

  // Enforce brotli-only bundle artifacts.
  // Remove stale .tar.gz files from previous runs so they can't be served accidentally.
  for (const entry of fs.readdirSync(OUTPUT_DIR, { recursive: true })) {
    const rel = String(entry);
    if (!rel.endsWith('.tar.gz')) continue;
    const full = path.join(OUTPUT_DIR, rel);
    if (fs.existsSync(full)) fs.unlinkSync(full);
  }

  if (!fs.existsSync(MANIFEST_FILE)) {
    console.error(`Manifest not found at ${MANIFEST_FILE}. Run build:manifest first.`);
    process.exit(1);
  }

  const manifest: Manifest = JSON.parse(fs.readFileSync(MANIFEST_FILE, 'utf-8'));

  // ── Define bundle groups ──
  // EVERY non-symlink file must belong to exactly one bundle.

  // Collect all real file paths (skip symlinks)
  const allPaths = Object.keys(manifest.files).filter((p) => !manifest.files[p].symlink);

  // Track which files have been assigned to a bundle
  const assigned = new Set<string>();
  const bundleFiles = new Map<string, string[]>();

  // --- 1. Tool pairs: <tool>.wasm + <tool>.mjs ---
  const toolBasenames = new Set<string>();
  for (const p of allPaths) {
    const m = p.match(/^\/usr\/lib\/([^/]+)\.(wasm|mjs)$/);
    if (m) toolBasenames.add(m[1]);
  }
  const toolBundles: string[] = [];
  for (const base of [...toolBasenames].sort()) {
    const hasWasm = allPaths.includes(`/usr/lib/${base}.wasm`);
    const hasMjs = allPaths.includes(`/usr/lib/${base}.mjs`);
    if (hasWasm && hasMjs) {
      toolBundles.push(base);
      const files = [`/usr/lib/${base}.mjs`, `/usr/lib/${base}.wasm`];

      // note: block commented out because i don't know if it is better to have or not
      // todo: decide if it is better to use this merging logic or just overwrite.
      // // If a bundle with the same name already exists, merge the tool-pair files instead of
      // // overwriting the existing bundle contents.
      // const existing = bundleFiles.get(base);
      // if (existing) {
      //   const merged = [...existing];
      //   for (const f of files) {
      //     if (!merged.includes(f)) merged.push(f);
      //   }
      //   bundleFiles.set(base, merged);
      // } else {
      //   bundleFiles.set(base, files);
      // }

      bundleFiles.set(base, files);
      for (const f of files) assigned.add(f);
    }
  }

  // --- 2. Prefix-based groups (order matters — first match wins) ---
  // python-runtime: /usr/lib/python*/ + /usr/lib/pkgconfig/ bundled together
  const pythonPrefixes: string[] = [];
  for (const p of allPaths) {
    const m = p.match(/^\/usr\/lib\/(python[^/]+)\//);
    if (m && !pythonPrefixes.includes(`/usr/lib/${m[1]}/`)) {
      pythonPrefixes.push(`/usr/lib/${m[1]}/`);
    }
  }
  pythonPrefixes.push('/usr/lib/pkgconfig/');
  pythonPrefixes.push('/usr/lib/libpython');

  // --- 2a. Emscripten cache sub-bundles (pattern-based classification) ---
  // The cache-lib directory has 361 files but a default hello-world only needs
  // ~14 of them (~5MB). Split into 8 sub-bundles by library family so only
  // the relevant group is downloaded on demand.
  const CACHE_PREFIX = '/usr/lib/emscripten/cache-lib/';

  // SDL3 pre-assignment — must run before the cache-lib sweep (2a) and the
  // usr-include prefix sweep (2b) so all SDL3 artefacts land in a single
  // dedicated bundle rather than being split across cache-misc / usr-include.
  const sdl3PrePrefixes = [
    `${CACHE_PREFIX}wasm32-emscripten/libSDL3`, // libSDL3.a from emsdk port
    '/usr/include/SDL3/', // SDL3 public headers
    '/usr/lib/emscripten_ports/sdl3/', // SDL3 port manifests
  ];
  const sdl3PreFiles: string[] = [];
  for (const p of allPaths) {
    if (assigned.has(p)) continue;
    if (sdl3PrePrefixes.some((pfx) => p.startsWith(pfx))) {
      sdl3PreFiles.push(p);
      assigned.add(p);
    }
  }
  bundleFiles.set('sdl3', sdl3PreFiles);

  // Libraries included by the default emcc link flags
  const coreLibs = new Set([
    'libc.a',
    'libcompiler_rt.a',
    'libc++-noexcept.a',
    'libc++abi-noexcept.a',
    'libdlmalloc.a',
    'libGL-getprocaddr.a',
    'libal.a',
    'libhtml5.a',
    'libstandalonewasm-nocatch.a',
    'libstubs.a',
    'libsockets.a',
    'libunwind-noexcept.a',
    'libjsmath.a',
    'libnoexit.a',
  ]);

  function classifyCacheFile(filePath: string): string {
    // Handle thinlto/ subdirectory
    if (filePath.includes('/thinlto/')) return 'cache-misc';

    const basename = path.basename(filePath);

    // CRT startup objects
    if (basename.endsWith('.o')) return 'cache-crt';

    // Sanitizer runtimes
    if (/^lib(asan|lsan|ubsan|sanitizer)/.test(basename)) return 'cache-sanitizers';

    // WasmFS variants
    if (basename.startsWith('libwasmfs')) return 'cache-wasmfs';

    // Default-link core libraries (checked before family patterns)
    if (coreLibs.has(basename)) return 'cache-core';

    // GL emulation variants (after core check so libGL-getprocaddr.a lands in core)
    if (basename.startsWith('libGL')) return 'cache-gl-variants';

    // Debug-mode variants of any family (libc++-debug, libc-debug, dlmalloc-debug, etc.)
    // Isolated into a single optional bundle to keep release bundles slim.
    // Checked BEFORE the family patterns so debug variants don't pollute them.
    if (/-debug/.test(basename)) return 'cache-debug';

    // C++/unwind variant families
    if (/^lib(c\+\+|c\+\+abi|unwind)/.test(basename)) return 'cache-libcxx-variants';

    // C library / allocator / compiler-rt variant families
    if (/^lib(c[-_.]|c_optz|compiler_rt-|dlmalloc-|emmalloc|mimalloc|printf_long_double)/.test(basename)) return 'cache-libc-variants';

    // Everything else (embind, fetch, sqlite3, zlib, wasm_workers, etc.)
    return 'cache-misc';
  }

  const cacheBundleNames = [
    'cache-crt',
    'cache-core',
    'cache-libcxx-variants',
    'cache-libc-variants',
    'cache-gl-variants',
    'cache-wasmfs',
    'cache-sanitizers',
    'cache-debug',
    'cache-misc',
  ];
  for (const name of cacheBundleNames) {
    bundleFiles.set(name, []);
  }

  for (const p of allPaths) {
    if (assigned.has(p)) continue;
    if (!p.startsWith(CACHE_PREFIX)) continue;
    const bundle = classifyCacheFile(p);
    bundleFiles.get(bundle)!.push(p);
    assigned.add(p);
  }

  // --- 2b. Prefix-based groups (order matters — first match wins) ---
  const prefixGroups: { name: string; prefixes: string[]; outputPath: string }[] = [
    // Clang resource-dir builtin headers (stddef.h, stdarg.h, *intrin.h).
    // Required for `clang -cc1` direct invocation in the browser, since cc1
    // mode bypasses the driver's auto-resource-dir detection.
    // Placed first so it wins over any future overlap with /usr/lib/* groups.
    { name: 'clang-headers', prefixes: ['/usr/lib/clang/'], outputPath: '/usr/lib/clang-headers.tar.br' },
    { name: 'usr-include', prefixes: ['/usr/include/'], outputPath: '/usr/include.tar.br' },
    { name: 'emscripten-core', prefixes: ['/usr/lib/emscripten/'], outputPath: '/usr/lib/emscripten-core.tar.br' },
    { name: 'python-runtime', prefixes: pythonPrefixes, outputPath: '/usr/lib/python-runtime.tar.br' },
    { name: 'usr-bin', prefixes: ['/usr/bin/', '/etc/'], outputPath: '/usr/bin.tar.br' },
    { name: 'libcurl', prefixes: ['/usr/lib/libcurl'], outputPath: '/usr/lib/libcurl.tar.br' },
    // Note: SDL3 lib, headers and port manifests are pre-assigned above (sdl3PrePrefixes).
    // These prefixes act as forward-compat fallback only (first-match wins, assigned files are skipped).
    { name: 'sdl3', prefixes: ['/usr/include/SDL3/', '/usr/lib/emscripten_ports/sdl3/'], outputPath: '/usr/lib/sdl3.tar.br' },
    { name: 'imgui', prefixes: ['/usr/lib/libimgui', '/usr/include/imgui/'], outputPath: '/usr/lib/imgui.tar.br' },
    {
      name: 'raylib',
      prefixes: [
        '/usr/lib/libraylib',
        '/usr/lib/libraygui',
        '/usr/lib/libphysac',
        '/usr/lib/librlights',
        '/usr/include/raylib/',
      ],
      outputPath: '/usr/lib/raylib.tar.br',
    },
    {
      name: 'allegro',
      prefixes: ['/usr/lib/liballegro', '/usr/lib/libSDL2.a', '/usr/include/allegro5/'],
      // NOTE: SDL2 headers are intentionally excluded — they are not needed by
      // user Allegro code (SDL2 is an impl detail hidden inside the static libs).
      outputPath: '/usr/lib/allegro.tar.br',
    },
    { name: 'usr-share', prefixes: ['/usr/share/'], outputPath: '/usr/share.tar.br' },
    { name: 'home', prefixes: ['/home/'], outputPath: '/home.tar.br' },
  ];

  for (const { name } of prefixGroups) {
    // Guard: do not overwrite bundles that were pre-assigned above (e.g. sdl3).
    if (!bundleFiles.has(name)) bundleFiles.set(name, []);
  }

  for (const p of allPaths) {
    if (assigned.has(p)) continue;
    for (const { name, prefixes } of prefixGroups) {
      if (prefixes.some((pfx) => p.startsWith(pfx))) {
        bundleFiles.get(name)!.push(p);
        assigned.add(p);
        break; // first match wins
      }
    }
  }

  // --- 3. Catch-all: remaining files go into "usr-lib-misc" ---
  const miscFiles: string[] = [];
  for (const p of allPaths) {
    if (!assigned.has(p)) {
      miscFiles.push(p);
      assigned.add(p);
    }
  }
  if (miscFiles.length > 0) {
    bundleFiles.set('usr-lib-misc', miscFiles);
  }

  // Remove empty bundles
  for (const [name, files] of bundleFiles) {
    if (files.length === 0) {
      bundleFiles.delete(name);
      console.log(`  Skipping empty bundle: ${name}`);
    }
  }

  // Verify ALL files are assigned
  const unassigned = allPaths.filter((p) => !assigned.has(p));
  if (unassigned.length > 0) {
    console.error(`ERROR: ${unassigned.length} files are not assigned to any bundle:`);
    for (const p of unassigned.slice(0, 10)) console.error(`  ${p}`);
    process.exit(1);
  }

  console.log(`\nBundle groups (${bundleFiles.size}):`);
  for (const [name, files] of bundleFiles) {
    console.log(`  ${name}: ${files.length} files`);
  }
  console.log(`Total files: ${allPaths.length}, all assigned to bundles.`);

  // ── P0: Global SHA-256 dedup pre-pass ─────────────────────────────────────
  // Invariant: no SHA-256 hash may be written as raw bytes more than once
  // across all .tar.br outputs. Second+ occurrences become
  // { symlink: canonicalPath } manifest entries — LazyFS.readFile() already
  // resolves these by recursively reading from the canonical bundle.
  {
    const seenHashes = new Map<string, string>(); // hash → first canonical manifest path
    let dedupCount = 0;
    let savedBytes = 0;

    // Compute pre-dedup total uncompressed bytes (for P0 report)
    let beforeUncompressedBytes = 0;
    for (const [, files] of bundleFiles) {
      for (const fp of files) {
        const e = manifest.files[fp];
        if (e && !e.symlink && typeof e.size === 'number') beforeUncompressedBytes += e.size;
      }
    }

    for (const [, files] of bundleFiles) {
      for (const filePath of files) {
        const entry = manifest.files[filePath];
        if (!entry || entry.symlink) continue; // already a symlink from source sysroot
        const hash = entry.hash;
        if (!hash) continue;
        if (seenHashes.has(hash)) {
          const canonicalPath = seenHashes.get(hash)!;
          const fileSize = entry.size ?? 0;
          // Replace manifest entry with a symlink; raw bytes will NOT be tarred
          manifest.files[filePath] = { symlink: canonicalPath };
          dedupCount++;
          savedBytes += fileSize;
          console.log(`  [dedup] ${filePath} → ${canonicalPath} (saves ${(fileSize / 1024).toFixed(0)}KB)`);
        } else {
          seenHashes.set(hash, filePath);
        }
      }
    }

    // Compute post-dedup total
    let afterUncompressedBytes = 0;
    for (const [, files] of bundleFiles) {
      for (const fp of files) {
        const e = manifest.files[fp];
        if (e && !e.symlink && typeof e.size === 'number') afterUncompressedBytes += e.size;
      }
    }

    console.log(`\nP0 dedup summary:`);
    console.log(`  Unique hashes:              ${seenHashes.size}`);
    console.log(`  Duplicate files collapsed:  ${dedupCount}`);
    console.log(`  Uncompressed bytes saved:   ${(savedBytes / (1024 * 1024)).toFixed(1)} MB`);
    console.log(`  Before (uncompressed total): ${(beforeUncompressedBytes / (1024 * 1024)).toFixed(1)} MB`);
    console.log(`  After  (uncompressed total): ${(afterUncompressedBytes / (1024 * 1024)).toFixed(1)} MB`);
  }

  // ── Generate tar.br archives ──

  const cpuCount = os.cpus().length;
  console.log(`\nUsing ${cpuCount} CPU cores for parallel brotli compression`);

  manifest.bundles = {};
  let totalCompressed = 0;
  let totalUncompressed = 0;

  // Phase 1: Prepare tar archives (fast, sequential, in-memory)
  interface BundleJob {
    bundleName: string;
    entries: { path: string; data: Uint8Array; executable?: boolean }[];
    tar: Buffer;
    bundleRelPath: string;
    outPath: string;
  }

  const jobs: BundleJob[] = [];

  for (const [bundleName, files] of bundleFiles) {
    // Read file data from CDN output directory
    const entries: { path: string; data: Uint8Array; executable?: boolean }[] = [];
    for (const filePath of files) {
      // P0: skip entries that were deduped to symlinks in the pre-pass
      if (manifest.files[filePath]?.symlink) continue;
      const cdnPath = path.join(OUTPUT_DIR, filePath.substring(1));
      if (!fs.existsSync(cdnPath)) {
        console.warn(`  Warning: ${filePath} not found at ${cdnPath}, skipping`);
        continue;
      }
      const data = fs.readFileSync(cdnPath);
      const entry = manifest.files[filePath];
      entries.push({
        path: filePath,
        data: new Uint8Array(data),
        executable: entry?.executable ?? false,
      });
    }

    if (entries.length === 0) {
      console.warn(`  Warning: no files found for bundle ${bundleName}`);
      continue;
    }

    // P5: Sort by extension then path so structurally similar files (e.g. all
    // .a, all .pyc) are adjacent.  Brotli can then reference earlier identical
    // or near-identical blocks within its window, improving ratios across the
    // whole bundle (~2–4 MB saving across the tarball).
    entries.sort((a, b) => {
      const extA = path.extname(a.path);
      const extB = path.extname(b.path);
      return extA !== extB ? extA.localeCompare(extB) : a.path.localeCompare(b.path);
    });

    // Create tar
    const tar = createDeterministicTar(entries);
    totalUncompressed += tar.length;

    // Determine output path
    let bundleRelPath: string;
    if (toolBundles.includes(bundleName)) {
      bundleRelPath = `/usr/lib/${bundleName}.tar.br`;
    } else {
      // Use the stored output path from the prefix group definition
      const group = prefixGroups.find((g) => g.name === bundleName);
      if (group) {
        bundleRelPath = group.outputPath;
      } else {
        // Fallback (shouldn't happen)
        bundleRelPath = `/usr/lib/${bundleName}.tar.br`;
      }
    }

    const outPath = path.join(OUTPUT_DIR, bundleRelPath.substring(1));
    const outDir = path.dirname(outPath);
    if (!fs.existsSync(outDir)) {
      fs.mkdirSync(outDir, { recursive: true });
    }

    jobs.push({ bundleName, entries, tar, bundleRelPath, outPath });
  }

  console.log(`\nCompressing ${jobs.length} bundles across ${cpuCount} cores...`);

  // Phase 2: Compress all bundles in parallel (one brotli job per core)
  const compressionJobs = jobs.map((job) => () => {
    const t0 = Date.now();
    console.log(`  [start] ${job.bundleName}: ${job.entries.length} files, ${(job.tar.length / (1024 * 1024)).toFixed(1)}MB tar`);
    return brotliCompress(job.tar, job.outPath).then((compressed) => {
      const elapsed = Date.now() - t0;
      const ratio = job.tar.length > 0 ? ((1 - compressed.length / job.tar.length) * 100).toFixed(1) : '0';
      console.log(
        `  [done]  ${job.bundleName}: ` +
        `${(job.tar.length / 1024).toFixed(0)}KB → ${(compressed.length / 1024).toFixed(0)}KB ` +
        `(${ratio}% compression), ${(elapsed / 1000).toFixed(1)}s`,
      );
      return compressed;
    });
  });

  const compressedResults = await runParallel(compressionJobs, cpuCount);

  // Phase 3: Update manifest with results (sequential, fast)
  for (let i = 0; i < jobs.length; i++) {
    const job = jobs[i];
    const compressed = compressedResults[i];
    const hash = sha256(compressed);

    totalCompressed += compressed.length;

    const bundleUrl = `${manifest.baseUrl}${job.bundleRelPath}`;
    manifest.bundles[job.bundleName] = {
      files: job.entries.map((e) => e.path),
      url: bundleUrl,
      size: compressed.length,
      hash,
    };

    for (const entry of job.entries) {
      if (manifest.files[entry.path]) {
        manifest.files[entry.path].bundle = job.bundleName;
      }
    }
  }

  // Write updated manifest
  fs.writeFileSync(MANIFEST_FILE, JSON.stringify(manifest, null, 2));

  console.log(
    `\nBundles generated: ${bundleFiles.size} bundles, ` +
    `${(totalUncompressed / (1024 * 1024)).toFixed(1)}MB uncompressed → ` +
    `${(totalCompressed / (1024 * 1024)).toFixed(1)}MB compressed`,
  );
  console.log(`Manifest updated: ${MANIFEST_FILE}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
