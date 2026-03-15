/**
 * Generate .tar.br bundle archives from the CDN file tree.
 *
 * Reads build/manifest.json (produced by generate-manifest.ts), groups files
 * into bundles, creates tar archives, compresses them with brotli, writes the
 * bundles into build/cdn/, and rewrites manifest.json with bundle metadata.
 *
 * EVERY file must belong to a bundle — no unbundled individual downloads.
 *
 * Bundle groups:
 *   <tool>               – paired .mjs + .wasm at /usr/lib/<tool>.{mjs,wasm}
 *   usr-include          – all files under /usr/include/
 *   usr-lib-emscripten   – all files under /usr/lib/emscripten/
 *   python-runtime       – /usr/lib/python<ver>/ + /usr/lib/pkgconfig/ + /usr/lib/libpython*
 *   usr-bin              – /usr/bin/ + /etc/ (small scripts & config)
 *   libcurl              – /usr/lib/libcurl* (static library)
 *   usr-share            – /usr/share/ files
 *
 * Version-specific paths like /usr/lib/python3.13/ are referenced in the
 * manifest as-is but the bundle is named "python-runtime" (not "python3.13").
 */

import { exec, execSync } from 'child_process';
import * as crypto from 'crypto';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = process.cwd();
const OUTPUT_DIR = process.env.OUTPUT_DIR || path.join(ROOT, 'build', 'cdn');
const MANIFEST_FILE =
    process.env.MANIFEST_FILE || path.join(ROOT, 'build', 'manifest.json');

// ──────────────────── helpers ────────────────────

/** Create a POSIX tar archive (in memory) from a list of {path, data} entries. */
function createTar(
    entries: { path: string; data: Uint8Array; executable?: boolean }[],
): Buffer {
    const blocks: Buffer[] = [];

    for (const entry of entries) {
        // File name (strip leading / for tar convention)
        const name = entry.path.replace(/^\//, '');
        const header = Buffer.alloc(512);

        // name (0–99)
        header.write(name.slice(0, 100), 0, 100, 'utf-8');
        // mode (100–107)
        header.write(
            (entry.executable ? '0000755' : '0000644') + '\0',
            100,
            8,
            'utf-8',
        );
        // uid (108–115)
        header.write('0000000\0', 108, 8, 'utf-8');
        // gid (116–123)
        header.write('0000000\0', 116, 8, 'utf-8');
        // size (124–135) – 11 octal digits + NUL
        header.write(
            entry.data.length.toString(8).padStart(11, '0') + '\0',
            124,
            12,
            'utf-8',
        );
        // mtime (136–147)
        const mtime = Math.floor(Date.now() / 1000);
        header.write(mtime.toString(8).padStart(11, '0') + '\0', 136, 12, 'utf-8');
        // typeflag (156) – '0' = regular file
        header[156] = 48; // ASCII '0'
        // magic (257–262) "ustar\0"
        header.write('ustar\0', 257, 6, 'utf-8');
        // version (263–264) "00"
        header.write('00', 263, 2, 'utf-8');

        // Compute checksum: sum of all bytes with checksum field (148–155) as spaces
        header.fill(0x20, 148, 156); // 8 spaces
        let chksum = 0;
        for (let i = 0; i < 512; i++) chksum += header[i];
        header.write(chksum.toString(8).padStart(6, '0') + '\0 ', 148, 8, 'utf-8');

        blocks.push(header);

        // File data padded to 512-byte boundary
        const dataBlock = Buffer.alloc(Math.ceil(entry.data.length / 512) * 512);
        dataBlock.set(entry.data);
        blocks.push(dataBlock);
    }

    // End-of-archive marker (two 512-byte zero blocks)
    blocks.push(Buffer.alloc(1024));

    return Buffer.concat(blocks);
}

/** SHA-256 hex hash of a buffer. */
function sha256(data: Buffer): string {
    return crypto.createHash('sha256').update(data).digest('hex');
}

/** Compress a buffer with brotli at quality 11 via a temp file (async). */
function brotliCompress(data: Buffer, outPath: string): Promise<Buffer> {
    return new Promise((resolve, reject) => {
        const tmpIn = outPath + '.tmp.tar';
        fs.writeFileSync(tmpIn, data);
        exec(`brotli -q 11 -f "${tmpIn}" -o "${outPath}"`, (error) => {
            try {
                if (fs.existsSync(tmpIn)) fs.unlinkSync(tmpIn);
            } catch { /* ignore cleanup errors */ }
            if (error) return reject(error);
            try {
                resolve(fs.readFileSync(outPath));
            } catch (e) {
                reject(e);
            }
        });
    });
}

/**
 * Run async jobs concurrently with a maximum of `concurrency` at a time.
 * Returns results in the same order as the input jobs array.
 */
async function runParallel<T>(
    jobs: (() => Promise<T>)[],
    concurrency: number,
): Promise<T[]> {
    const results: T[] = new Array(jobs.length);
    let nextIndex = 0;

    async function worker() {
        while (true) {
            const idx = nextIndex++;
            if (idx >= jobs.length) return;
            results[idx] = await jobs[idx]();
        }
    }

    const workers = Array.from(
        { length: Math.min(concurrency, jobs.length) },
        () => worker(),
    );
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

    if (!fs.existsSync(MANIFEST_FILE)) {
        console.error(`Manifest not found at ${MANIFEST_FILE}. Run build:manifest first.`);
        process.exit(1);
    }

    const hasBrotli = (() => {
        try {
            execSync('brotli --version', { stdio: 'pipe' });
            return true;
        } catch {
            return false;
        }
    })();

    if (!hasBrotli) {
        console.warn(
            "Warning: brotli CLI not found. Install with 'brew install brotli' or 'apt install brotli'.",
        );
        console.warn('Bundles will not be generated.');
        process.exit(0);
    }

    const manifest: Manifest = JSON.parse(
        fs.readFileSync(MANIFEST_FILE, 'utf-8'),
    );

    // ── Define bundle groups ──
    // EVERY non-symlink file must belong to exactly one bundle.

    // Collect all real file paths (skip symlinks)
    const allPaths = Object.keys(manifest.files).filter(
        (p) => !manifest.files[p].symlink,
    );

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

    const prefixGroups: { name: string; prefixes: string[]; outputPath: string }[] = [
        { name: 'usr-include', prefixes: ['/usr/include/'], outputPath: '/usr/include.tar.br' },
        { name: 'emscripten-cache', prefixes: ['/usr/lib/emscripten/cache-lib/'], outputPath: '/usr/lib/emscripten-cache.tar.br' },
        { name: 'emscripten-core', prefixes: ['/usr/lib/emscripten/'], outputPath: '/usr/lib/emscripten-core.tar.br' },
        { name: 'python-runtime', prefixes: pythonPrefixes, outputPath: '/usr/lib/python-runtime.tar.br' },
        { name: 'usr-bin', prefixes: ['/usr/bin/', '/etc/'], outputPath: '/usr/bin.tar.br' },
        { name: 'libcurl', prefixes: ['/usr/lib/libcurl'], outputPath: '/usr/lib/libcurl.tar.br' },
        { name: 'usr-share', prefixes: ['/usr/share/'], outputPath: '/usr/share.tar.br' },
    ];

    for (const { name } of prefixGroups) {
        bundleFiles.set(name, []);
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
        const entries: { path: string; data: Uint8Array; executable?: boolean }[] =
            [];
        for (const filePath of files) {
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

        // Create tar
        const tar = createTar(entries);
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
