import { exec } from 'child_process';
import cliProgress from 'cli-progress';
import * as crypto from 'crypto';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const ROOT = process.cwd();
const OUTPUT_DIR = process.env.OUTPUT_DIR || path.join(ROOT, 'build', 'cdn');
const MANIFEST_FILE = process.env.MANIFEST_FILE || path.join(ROOT, 'build', 'manifest.json');
const BASE_URL = process.env.CDN_BASE_URL || '/cdn';
const SYSPATH = process.env.SYSPATH || path.join(ROOT, 'sysroot');
const SKIP_BROTLI = process.env.SKIP_BROTLI === '1';

// Ensure output directory exists
shell.mkdir('-p', OUTPUT_DIR);
shell.mkdir('-p', path.dirname(MANIFEST_FILE));

console.log('Generating manifest...');
console.log(`Sysroot: ${SYSPATH}`);
console.log(`Output: ${OUTPUT_DIR}`);
console.log(`Manifest: ${MANIFEST_FILE}`);
console.log(`Base URL: ${BASE_URL}`);
console.log(`Skip Brotli: ${SKIP_BROTLI}`);

interface ManifestFile {
    size?: number;
    hash?: string;
    compressed?: string;
    executable?: boolean;
    priority?: string;
    symlink?: string;
}

interface Manifest {
    version: number;
    generated: string;
    baseUrl: string;
    files: Record<string, ManifestFile>;
    bundles: Record<string, any>;
}

const manifest: Manifest = {
    version: 1,
    generated: new Date().toISOString().replace(/\.\d+Z$/, 'Z'), // Format like date -u +%Y-%m-%dT%H:%M:%SZ
    baseUrl: BASE_URL,
    files: {},
    bundles: {},
};

const allEntries: string[] = [];

function scan(dir: string) {
    const list = fs.readdirSync(dir);
    for (const file of list) {
        const fullPath = path.join(dir, file);
        const stat = fs.lstatSync(fullPath);
        if (stat.isDirectory()) {
            scan(fullPath);
        } else {
            allEntries.push(fullPath);
        }
    }
}

if (fs.existsSync(SYSPATH)) {
    console.log('Scanning files...');
    scan(SYSPATH);
    console.log(`Found ${allEntries.length} entries.`);
} else {
    console.error(`Error: Sysroot directory not found at ${SYSPATH}`);
    process.exit(1);
}

const bar = new cliProgress.SingleBar({
    format: 'Processing |' + '{bar}' + '| {percentage}% || {value}/{total} Files || ETA: {eta_formatted}',
    barCompleteChar: '\u2588',
    barIncompleteChar: '\u2591',
    hideCursor: true
}, cliProgress.Presets.shades_classic);

bar.start(allEntries.length, 0);

const concurrency = os.cpus().length;
console.log(`Using ${concurrency} threads for processing.`);

async function processFile(fullPath: string) {
    const stat = fs.lstatSync(fullPath);
    const relPath = '/' + path.relative(SYSPATH, fullPath).replace(/\\/g, '/');

    if (stat.isSymbolicLink()) {
        // Handle symlinks
        const target = fs.readlinkSync(fullPath);
        manifest.files[relPath] = { symlink: target };
    } else if (stat.isFile()) {
        // Handle regular files
        const data = fs.readFileSync(fullPath);
        const hash = crypto.createHash('sha256').update(data).digest('hex');
        const size = data.length;
        const isExecutable = (stat.mode & 0o111) !== 0 || fullPath.endsWith('.wasm');

        let priority = 'normal';
        if (relPath === '/bin/sh' || relPath === '/bin/busybox') {
            priority = 'critical';
        } else if (relPath.startsWith('/usr/bin/')) {
            priority = 'high';
        } else if (relPath.startsWith('/usr/include/')) {
            priority = 'low';
        }

        const fileEntry: ManifestFile = {
            size,
            hash,
            executable: isExecutable,
            priority,
        };

        // Write uncompressed file to CDN (always needed for browser import/fetch)
        const cdnPathRaw = path.join(OUTPUT_DIR, relPath.substring(1));
        shell.mkdir('-p', path.dirname(cdnPathRaw));
        fs.writeFileSync(cdnPathRaw, data);

        // Optionally write brotli-compressed version alongside for future CDN
        // content negotiation (Accept-Encoding: br). We do NOT set
        // fileEntry.compressed so the manifest always references the raw URL —
        // browsers need uncompressed .mjs for import() and .wasm for
        // WebAssembly.instantiateStreaming().
        if (!SKIP_BROTLI && shell.which('brotli')) {
            const cdnPathBr = path.join(OUTPUT_DIR, relPath.substring(1) + '.br');
            await new Promise<void>((resolve) => {
                exec(`brotli -q 11 -f "${fullPath}" -o "${cdnPathBr}"`, (error) => {
                    if (error) {
                        console.warn(`Brotli compression failed for ${relPath}: ${error.message}`);
                    }
                    resolve();
                });
            });
        }

        manifest.files[relPath] = fileEntry;
    }
}

async function main() {
    const pool = new Set<Promise<void>>();

    for (const fullPath of allEntries) {
        const p = processFile(fullPath).then(() => {
            pool.delete(p);
            bar.increment();
        });

        pool.add(p);

        if (pool.size >= concurrency) {
            await Promise.race(pool);
        }
    }

    await Promise.all(pool);

    bar.stop();

    fs.writeFileSync(MANIFEST_FILE, JSON.stringify(manifest, null, 2));
    console.log(`Manifest generated with ${Object.keys(manifest.files).length} entries.`);
}

main().catch(err => {
    console.error(err);
    process.exit(1);
});
