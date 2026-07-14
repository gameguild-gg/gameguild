/**
 * Build CPython as a standalone Emscripten WASM module.
 *
 * Produces python.wasm — a self-contained module that statically links
 * libpython.a. No SIDE_MODULE, no shared libraries, no libc_stubs.
 *
 * The Python version is detected from emsdk's bundled Python unless
 * overridden via the PYTHON_VERSION environment variable.
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';
import { pythonMajorMinor } from './lib/detect-versions.ts';
import { setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { PINNED } from './lib/pinned-versions.ts';

enableBuildKeepalive('build-cpython');

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = process.cwd();

// Ensure shell commands fail on error
shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION;

// Setup EMSDK first
setupEmsdk(EMSDK_VERSION);

const PYTHON_VERSION = process.env.PYTHON_VERSION || PINNED.PYTHON_VERSION;
const PYTHON_MM = pythonMajorMinor(PYTHON_VERSION);   // e.g. "3.13"
const CONCURRENCY = os.cpus().length;

/** Common Emscripten flags for standalone tool modules */
const STANDALONE_FLAGS = [
    '-sALLOW_MEMORY_GROWTH=1',
    '-sFORCE_FILESYSTEM=1',
    '-sMODULARIZE=1',
    '-sEXPORT_ES6=1',
    '-sEXIT_RUNTIME=1',
    '-sINVOKE_RUN=0',
    '-sEXPORTED_RUNTIME_METHODS=FS,callMain',
    '-sSTACK_SIZE=2097152',  // 2 MB — CPython import chain needs deep stack
    // Emscripten ports needed by CPython built-in modules
    '-sUSE_ZLIB=1',     // binascii, zlib modules (crc32, deflate, etc.)
    '-sUSE_BZIP2=1',    // _bz2 module
    '-sUSE_SQLITE3=1',  // _sqlite3 module
    // Asyncify: instrument the binary so async JS imports (FS hooks,
    // subprocess dispatch) transparently suspend/resume the WASM stack.
    // Works in ALL browsers (Chrome, Safari, Firefox) — unlike JSPI.
    '-sASYNCIFY',
    '-sASYNCIFY_STACK_SIZE=65536',    // 64 KB
    `-sASYNCIFY_IMPORTS=${JSON.stringify([
        '__syscall_openat', '__syscall_stat64', '__syscall_lstat64',
        '__syscall_faccessat', '__syscall_readlinkat', '__syscall_newfstatat',
        '__emscripten_system',
    ])}`,
    // Disable reference-types — incompatible with asyncify instrumentation
    '-mno-reference-types',
].join(' ');

const USERLAND_DIR = path.join(ROOT, 'userland', 'cpython');
const SOURCE_DIR = path.join(USERLAND_DIR, `cpython-${PYTHON_VERSION}`);
const PATCHES_DIR = path.join(USERLAND_DIR, 'patches');
const BUILD_NATIVE_DIR = path.join(SOURCE_DIR, 'build-native');
const BUILD_WASM_DIR = path.join(SOURCE_DIR, 'build-wasm');
const SYSROOT_STAGING = path.join(USERLAND_DIR, 'sysroot-staging');
const OUTPUT_DIR = path.join(ROOT, 'build', 'cdn');

// Create userland dir if not exists
shell.mkdir('-p', USERLAND_DIR);

const GITHUB_AUTH = process.env.GITHUB_TOKEN
    ? `-H "Authorization: token ${process.env.GITHUB_TOKEN}"`
    : '';

// 1. Download CPython source
// Validate by checking configure script — absence means incomplete cache entry.
const isCPythonSourceValid = fs.existsSync(path.join(SOURCE_DIR, 'configure'));
if (!isCPythonSourceValid) {
    if (fs.existsSync(SOURCE_DIR)) {
        console.log(`Removing incomplete CPython source dir: ${path.basename(SOURCE_DIR)}`);
        shell.rm('-rf', SOURCE_DIR);
    }
    console.log(`Downloading CPython ${PYTHON_VERSION}...`);
    shell.cd(USERLAND_DIR);
    const tarball = `v${PYTHON_VERSION}.tar.gz`;
    shell.exec(`curl -fSL ${GITHUB_AUTH} -o "${tarball}" "https://github.com/python/cpython/archive/refs/tags/${tarball}"`);
    shell.exec(`tar xzf "${tarball}"`);
    shell.rm(tarball);
}

shell.cd(SOURCE_DIR);

// 2. Apply patches
if (fs.existsSync(PATCHES_DIR)) {
    const files = fs.readdirSync(PATCHES_DIR);
    const patches = files.filter(f => f.endsWith('.patch')).map(f => path.join(PATCHES_DIR, f));

    if (patches.length > 0) {
        for (const patch of patches) {
            console.log(`Applying patch: ${patch}`);
            shell.exec(`patch -p1 -N < "${patch}" || true`);
        }
    } else {
        console.log('No patches found.');
    }
}

// 3. Build native Python (needed for cross-compilation bootstrapping)
console.log('Building native Python...');
shell.mkdir('-p', BUILD_NATIVE_DIR);
shell.cd(BUILD_NATIVE_DIR);

if (!fs.existsSync(path.join(BUILD_NATIVE_DIR, 'Makefile'))) {
    shell.exec(`../configure --prefix="${path.join(BUILD_NATIVE_DIR, 'install')}"`);
}
shell.exec(`make -j${CONCURRENCY}`);
shell.exec('make install');

// 4. Cross-compile to WASM (standard build, no SIDE_MODULE flags)
console.log('Cross-compiling CPython to WASM...');
shell.mkdir('-p', BUILD_WASM_DIR);
shell.cd(BUILD_WASM_DIR);

// CPython deepfreeze emits very large generated string initializers that trigger
// a noisy Clang warning (-Wunterminated-string-initialization). This is known
// and benign for generated bytecode blobs; suppress it to keep CI logs readable.
const DEEPFREEZE_WARNING_SUPPRESS = '-Wno-unterminated-string-initialization';
if (!(process.env.CFLAGS || '').includes(DEEPFREEZE_WARNING_SUPPRESS)) {
    process.env.CFLAGS = `${process.env.CFLAGS || ''} ${DEEPFREEZE_WARNING_SUPPRESS}`.trim();
}

if (!fs.existsSync(path.join(BUILD_WASM_DIR, 'Makefile'))) {
    const configSite = path.join(SOURCE_DIR, 'Tools', 'wasm', 'config.site-wasm32-emscripten');

    // Set environment variables for configure
    process.env.CONFIG_SITE = configSite;
    process.env.ac_cv_file__dev_ptmx = 'no';
    process.env.ac_cv_file__dev_ptc = 'no';
    process.env.ac_cv_func_memfd_create = 'no';

    const configureCmd = `emconfigure ../configure \
    --host=wasm32-unknown-emscripten \
    --build=${shell.exec('../config.guess', { silent: true }).stdout.trim()} \
    --with-emscripten-target=browser \
    --with-build-python="${path.join(BUILD_NATIVE_DIR, 'install', 'bin', 'python3')}" \
    --prefix=/usr \
    --disable-ipv6 \
    --disable-test-modules`;

    console.log(configureCmd);
    shell.exec(configureCmd);
}

// Build WASM
shell.exec(`emmake make -j${CONCURRENCY}`);

// 5. Install to sysroot staging area
console.log('Installing to sysroot-staging...');
shell.mkdir('-p', SYSROOT_STAGING);

shell.config.fatal = false;
const installCmd = `emmake make install DESTDIR="${SYSROOT_STAGING}" V=1`;
console.log(installCmd);
if (shell.exec(installCmd).code !== 0) {
    console.warn('make install failed, checking if critical files exist...');
}
shell.config.fatal = true;

// 6. Deploy sysroot-staging to sysroot
// The Python stdlib files in sysroot/usr/lib/pythonX.Y/ are served via
// brotli-compressed tar bundles (generated by build:bundles), so there
// is no need for a separate stdlib zip file.
const stagingUsr = path.join(SYSROOT_STAGING, 'usr');
const SYSROOT_USR = path.join(ROOT, 'sysroot', 'usr');
if (fs.existsSync(stagingUsr)) {
    // Remove stale Python-versioned entries left by a previous build with a
    // different Python version (e.g. CI cache restored from before the emsdk
    // version pin changed). Without this, old pydoc3.X / idle3.X / python3.X/
    // / libpython3.X.a / include/python3.X/ survive alongside the new version
    // and end up in the generated manifest.
    if (fs.existsSync(SYSROOT_USR)) {
        const staleDirs = ['bin', 'lib', 'include'] as const;
        const removed: string[] = [];
        for (const subdir of staleDirs) {
            const dir = path.join(SYSROOT_USR, subdir);
            if (!fs.existsSync(dir)) continue;
            for (const entry of fs.readdirSync(dir)) {
                const m = entry.match(/^(?:pydoc|idle|python|libpython)(3\.\d+)/);
                if (m && m[1] !== PYTHON_MM) {
                    shell.rm('-rf', path.join(dir, entry));
                    removed.push(`usr/${subdir}/${entry}`);
                }
            }
        }
        if (removed.length > 0) {
            console.log(`Removed ${removed.length} stale Python artifacts (expected python${PYTHON_MM}):`);
            for (const p of removed) console.log(`  - ${p}`);
        }
    }
    shell.mkdir('-p', SYSROOT_USR);
    shell.cp('-r', path.join(stagingUsr, '*'), SYSROOT_USR);
}

// 8. Build python.wasm — standalone module that statically links libpython
console.log('Building python.wasm (standalone module)...');
const BUILD_DIR = path.join(ROOT, 'build');
shell.mkdir('-p', BUILD_DIR);

const libpythonA = path.join(BUILD_WASM_DIR, `libpython${PYTHON_MM}.a`);
const pythonDriverObj = path.join(BUILD_WASM_DIR, 'Programs', 'python.o');

if (!fs.existsSync(libpythonA)) {
    console.error(`ERROR: libpython${PYTHON_MM}.a not found at ${libpythonA}`);
    process.exit(1);
}
if (!fs.existsSync(pythonDriverObj)) {
    console.error(`ERROR: python.o not found at ${pythonDriverObj}`);
    process.exit(1);
}

// Find additional static libraries built by CPython (libmpdec, libexpat, etc.)
const extraLibs: string[] = [];
const mpdecLib = path.join(BUILD_WASM_DIR, 'Modules', '_decimal', 'libmpdec', 'libmpdec.a');
if (fs.existsSync(mpdecLib)) extraLibs.push(mpdecLib);
const expatLib = path.join(BUILD_WASM_DIR, 'Modules', 'expat', 'libexpat.a');
if (fs.existsSync(expatLib)) extraLibs.push(expatLib);
console.log(`Extra static libraries: ${extraLibs.map(l => path.basename(l)).join(', ') || '(none)'}`);

// Find HACL crypto object files (MD5, SHA1, SHA2, SHA3, Blake2, etc.)
// Some of these may already be in libpython.a — linking duplicates causes
// "duplicate symbol" errors.  Use `emar t` to list the archive members and
// only link HACL .o files that are NOT already inside the archive.
const haclDir = path.join(BUILD_WASM_DIR, 'Modules', '_hacl');
const haclObjects: string[] = [];
if (fs.existsSync(haclDir)) {
    // List object files already in libpython.a
    const arList = shell.exec(`emar t "${libpythonA}"`, { silent: true }).stdout;
    const arMembers = new Set(arList.split('\n').map(l => l.trim()).filter(Boolean));

    for (const f of fs.readdirSync(haclDir)) {
        if (f.endsWith('.o') && !arMembers.has(f)) {
            haclObjects.push(path.join(haclDir, f));
        }
    }
}
if (haclObjects.length > 0) {
    console.log(`HACL crypto objects (not in archive): ${haclObjects.map(o => path.basename(o)).join(', ')}`);
} else {
    console.log('All HACL crypto objects are already in libpython.a');
}

const pythonMjs = path.join(BUILD_DIR, 'python.mjs');
const pythonWasm = path.join(BUILD_DIR, 'python.wasm');

const cmdParts = [
    `em++`,
    STANDALONE_FLAGS,
    `"${pythonDriverObj}"`,
    `-Wl,--whole-archive`,
    `"${libpythonA}"`,
    ...extraLibs.map(l => `"${l}"`),
    `-Wl,--no-whole-archive`,
    ...haclObjects.map(o => `"${o}"`),
    `-O2`,
    `-o "${pythonMjs}"`,
];
const cmd = cmdParts.join(' \\\n    ');

console.log(cmd);
shell.exec(cmd);
console.log(`Created ${pythonWasm} + ${pythonMjs}`);

// NOTE: Post-processing patches (ENV merge, systemCallback, Asyncify
// hooks) are applied by scripts/patch-glue.ts
// which runs as the `patch:glue` step in the build:all pipeline.
// This keeps patching logic centralized and applies to ALL .mjs files.

// 9. Deploy to sysroot
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
shell.mkdir('-p', SYSROOT_LIB);

for (const ext of ['.wasm', '.mjs']) {
    const src = path.join(BUILD_DIR, `python${ext}`);
    const dst = path.join(SYSROOT_LIB, `python${ext}`);
    if (fs.existsSync(src)) {
        console.log(`Copying python${ext} to sysroot...`);
        shell.cp('-f', src, dst);
    } else {
        console.error(`ERROR: python${ext} missing in build directory`);
        process.exit(1);
    }
}

console.log('>>> CPython build complete.');
