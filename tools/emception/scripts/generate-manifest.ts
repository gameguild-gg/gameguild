import cliProgress from 'cli-progress';
import * as crypto from 'crypto';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';
import { enableBuildKeepalive } from './lib/keepalive.ts';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const ROOT = process.cwd();
const OUTPUT_DIR = process.env.OUTPUT_DIR || path.join(ROOT, 'build', 'cdn');
const MANIFEST_FILE = process.env.MANIFEST_FILE || path.join(ROOT, 'build', 'manifest.json');
const BASE_URL = process.env.CDN_BASE_URL || '/cdn';
const SYSPATH = process.env.SYSPATH || path.join(ROOT, 'sysroot');

enableBuildKeepalive('generate-manifest');

// Ensure output directory exists
shell.mkdir('-p', OUTPUT_DIR);
shell.mkdir('-p', path.dirname(MANIFEST_FILE));

console.log('Generating manifest...');
console.log(`Sysroot: ${SYSPATH}`);
console.log(`Output: ${OUTPUT_DIR}`);
console.log(`Manifest: ${MANIFEST_FILE}`);
console.log(`Base URL: ${BASE_URL}`);

interface ManifestFile {
    size?: number;
    hash?: string;
    executable?: boolean;
    priority?: string;
    symlink?: string;
}

interface Manifest {
    version: number;
    generated: string;
    baseUrl: string;
    toolVersions: {
        pythonMajorMinor: string;
        pythonMajorMinorCompact: string;
    };
    files: Record<string, ManifestFile>;
    bundles: Record<string, any>;
}

// Detect Python version from sysroot directory structure
function detectPythonVersionFromSysroot(syspath: string): { majorMinor: string; compact: string } {
    const libDir = path.join(syspath, 'usr', 'lib');
    if (fs.existsSync(libDir)) {
        for (const entry of fs.readdirSync(libDir)) {
            const match = entry.match(/^python(\d+)\.(\d+)$/);
            if (match) {
                const majorMinor = `${match[1]}.${match[2]}`;
                const compact = `${match[1]}${match[2]}`;
                console.log(`Detected Python version from sysroot: ${majorMinor}`);
                return { majorMinor, compact };
            }
        }
    }
    console.warn('Could not detect Python version from sysroot, defaulting to 3.13');
    return { majorMinor: '3.13', compact: '313' };
}

const pyVer = detectPythonVersionFromSysroot(SYSPATH);

const manifest: Manifest = {
    version: 1,
    generated: new Date().toISOString().replace(/\.\d+Z$/, 'Z'),
    baseUrl: BASE_URL,
    toolVersions: {
        pythonMajorMinor: pyVer.majorMinor,
        pythonMajorMinorCompact: pyVer.compact,
    },
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

// ── P1: Python stdlib exclusion list ──────────────────────────────────────────
// Paths matching any of these patterns are dropped before they enter the CDN
// tree or manifest.  This removes dead weight that is never used in the WASM
// browser runtime: GUI toolkits, pip wheels, HTML docs, duplicate bytecode
// optimization tiers, unused codecs, and test suites.

// Python base prefix (e.g. "/usr/lib/python3.13")
const PYTHON_RE = /^\/usr\/lib\/python\d+\.\d+/;

// Encodings that are required in the browser runtime (keep).
// Everything else under .../encodings/ is dropped.
const ENCODING_ALLOWLIST = new Set([
    '__init__', 'aliases',
    'ascii', 'latin_1', 'mbcs', 'idna', 'unicode_escape',
    // utf_* handled separately by prefix match below
]);

/**
 * Return true if this sysroot-relative path should be excluded from the CDN.
 * Called before any file I/O — pure string comparison, zero file reads.
 */
function shouldExclude(relPath: string): boolean {
    // ── Python-specific exclusions ──
    if (PYTHON_RE.test(relPath)) {
        // Bytecode optimization tiers 1 and 2 — keep only plain .pyc
        if (relPath.endsWith('.opt-1.pyc') || relPath.endsWith('.opt-2.pyc')) return true;

        // GUI and rarely-needed top-level packages
        if (/\/python\d+\.\d+\/(idlelib|tkinter|turtledemo|ensurepip|pydoc_data)\//.test(relPath)) return true;

        // Stdlib test suites
        if (/\/python\d+\.\d+\/(test|tests)\//.test(relPath)) return true;

        // unittest framework
        if (/\/python\d+\.\d+\/unittest\//.test(relPath)) return true;

        // Encodings: drop anything not in the allowlist and not a utf_* codec
        const encMatch = relPath.match(/\/python\d+\.\d+\/encodings\/([^/]+)$/);
        if (encMatch) {
            const basename = encMatch[1].replace(/\.(py|pyc)$/, '');
            if (!ENCODING_ALLOWLIST.has(basename) && !basename.startsWith('utf_')) {
                return true;
            }
        }
    }

    // ── Emscripten vendored test fixtures ──
    if (relPath.startsWith('/usr/lib/emscripten/third_party/ply/test/')) return true;

    return false;
}

async function processFile(fullPath: string) {
    const stat = fs.lstatSync(fullPath);
    const relPath = '/' + path.relative(SYSPATH, fullPath).replace(/\\/g, '/');

    // P1: skip excluded paths before any file I/O
    if (shouldExclude(relPath)) return;

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
