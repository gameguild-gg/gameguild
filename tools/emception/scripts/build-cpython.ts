/**
 * Build CPython as a standalone Emscripten WASM module.
 *
 * Produces python.wasm — a self-contained module that statically links
 * libpython.a. No SIDE_MODULE, no shared libraries, no libc_stubs.
 *
 * The Python and helper-tool versions come only from toolchain.lock.json.
 */

import fs from 'fs';
import { execFileSync, spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import os from 'os';
import path from 'path';
import { toolchainPaths } from './toolchain/paths.ts';
import shell from 'shelljs';
import { fileURLToPath } from 'url';
import { setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { loadToolchainStateSync, lockedTool, lockedVersion, pythonMajorMinor } from './toolchain/config.ts';
import { ensureLockedSource } from './toolchain/sources.ts';

enableBuildKeepalive('build-cpython');

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = process.cwd();
const P = toolchainPaths(ROOT);

// Ensure shell commands fail on error
shell.config.fatal = true;

const { lock } = loadToolchainStateSync(ROOT);
const EMSDK_VERSION = lockedVersion(lock, 'emsdk');

// Setup EMSDK first
setupEmsdk(EMSDK_VERSION);

const PYTHON_VERSION = lockedVersion(lock, 'python');
const PYTHON_MM = pythonMajorMinor(PYTHON_VERSION);   // e.g. "3.13"
const CONCURRENCY = Number(process.env.EMCEPTION_BUILD_CONCURRENCY || os.cpus().length);

function quotePosix(value: string): string {
    return `'${value.replaceAll('\\', '/').replaceAll("'", `'\\''`)}'`;
}

function sha256(filePath: string): string {
    return createHash('sha256').update(fs.readFileSync(filePath)).digest('hex');
}

function downloadPinned(url: string, destination: string, expectedHash: string): void {
    if (!fs.existsSync(destination)) {
        shell.exec(`curl -fSL -o "${destination}" "${url}"`);
    }
    const actualHash = sha256(destination);
    if (actualHash !== expectedHash) {
        throw new Error(`Checksum mismatch for ${destination}: expected ${expectedHash}, got ${actualHash}`);
    }
}

function resolveGitBash(): string {
    const candidates = [
        process.env.GIT_BASH_PATH,
        process.env.OMO_CODEX_GIT_BASH_PATH,
        process.env.ProgramFiles && path.join(process.env.ProgramFiles, 'Git', 'bin', 'bash.exe'),
        process.env.LOCALAPPDATA && path.join(process.env.LOCALAPPDATA, 'Programs', 'Git', 'bin', 'bash.exe'),
    ].filter((candidate): candidate is string => Boolean(candidate));
    const gitBash = candidates.find(candidate => fs.existsSync(candidate));
    if (!gitBash) throw new Error('Git Bash is required to build CPython on Windows; set GIT_BASH_PATH');
    return gitBash;
}

function lockedArchive(name: 'zstdWindows' | 'msys2Make') {
    const tool = lockedTool(lock, name);
    if (tool.source.kind !== 'archive') throw new Error(`${name} must use an archive source`);
    return { version: tool.version, ...tool.source };
}

function ensureWindowsMake(): string {
    const cacheDir = path.join(P.downloads, 'windows-build-tools');
    const packageDir = path.join(cacheDir, 'package');
    const makeExecutable = path.join(packageDir, 'mingw64', 'bin', 'mingw32-make.exe');
    if (fs.existsSync(makeExecutable)) return makeExecutable;

    fs.mkdirSync(packageDir, { recursive: true });
    const zstd = lockedArchive('zstdWindows');
    const zstdVersion = zstd.version;
    const zstdArchive = path.join(cacheDir, `zstd-v${zstdVersion}-win64.zip`);
    downloadPinned(
        zstd.url,
        zstdArchive,
        zstd.sha256,
    );
    execFileSync('C:\\Windows\\System32\\tar.exe', ['-xf', zstdArchive, '-C', cacheDir]);
    const zstdExecutable = path.join(cacheDir, `zstd-v${zstdVersion}-win64`, 'zstd.exe');

    const make = lockedArchive('msys2Make');
    const makeVersion = make.version;
    const makeArchive = path.join(cacheDir, `mingw-w64-x86_64-make-${makeVersion}-any.pkg.tar.zst`);
    downloadPinned(
        make.url,
        makeArchive,
        make.sha256,
    );
    const makeTar = path.join(cacheDir, 'make.pkg.tar');
    execFileSync(zstdExecutable, ['-d', '-f', makeArchive, '-o', makeTar], { stdio: 'inherit' });
    execFileSync('C:\\Windows\\System32\\tar.exe', ['-xf', makeTar, '-C', packageDir]);
    if (!fs.existsSync(makeExecutable)) throw new Error(`GNU Make was not extracted to ${makeExecutable}`);
    return makeExecutable;
}

function runPosix(command: string, cwd: string, fatal = true): { stdout: string; status: number } {
    const bash = resolveGitBash();
    const result = spawnSync(bash, ['-lc', command], {
        cwd,
        env: { ...process.env, SHELL: bash, MSYS2_ARG_CONV_EXCL: '*' },
        encoding: 'utf8',
        stdio: ['inherit', 'pipe', 'pipe'],
    });
    if (result.stdout) process.stdout.write(result.stdout);
    if (result.stderr) process.stderr.write(result.stderr);
    if (fatal && result.status !== 0) {
        throw new Error(`POSIX command failed with exit ${result.status}: ${command}`);
    }
    return { stdout: result.stdout.trim(), status: result.status ?? 1 };
}

function windowsMakeOverrides(): string {
    const emscriptenDir = path.join(process.env.EMSDK ?? '', 'upstream', 'emscripten');
    const emcc = path.join(emscriptenDir, 'emcc.bat').replaceAll('\\', '/');
    const emxx = path.join(emscriptenDir, 'em++.bat').replaceAll('\\', '/');
    const emar = path.join(emscriptenDir, 'emar.bat').replaceAll('\\', '/');
    return [
        `CC=${emcc}`,
        `CXX=${emxx}`,
        `AR=${emar}`,
        `LDSHARED=${emcc} $(PY_LDFLAGS)`,
        `BLDSHARED=${emcc} $(PY_CORE_LDFLAGS)`,
    ].map(quotePosix).join(' ');
}

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

const SOURCE_ROOT = path.join(P.sources, 'cpython');
const SOURCE_DIR = path.join(SOURCE_ROOT, `cpython-${PYTHON_VERSION}`);
const PATCHES_DIR = path.join(P.overlays, 'cpython', 'patches');
const BUILD_NATIVE_DIR = path.join(P.builds, 'cpython', 'native');
const BUILD_WASM_DIR = path.join(P.builds, 'cpython', 'wasm');
const SYSROOT_STAGING = path.join(P.builds, 'cpython', 'sysroot-staging');

ensureLockedSource(ROOT, lock, 'python', SOURCE_DIR, 'configure');

shell.cd(SOURCE_DIR);

// 2. Apply patches
if (fs.existsSync(PATCHES_DIR)) {
    const files = fs.readdirSync(PATCHES_DIR);
    const patches = files.filter(f => f.endsWith('.patch')).map(f => path.join(PATCHES_DIR, f));

    if (patches.length > 0) {
        for (const patch of patches) {
            console.log(`Applying patch: ${patch}`);
            const runPatch = (args: string): number => {
                const command = `patch ${args} -p1 < ${quotePosix(patch)}`;
                if (process.platform === 'win32') {
                    return runPosix(command, SOURCE_DIR, false).status;
                }
                const fatal = shell.config.fatal;
                shell.config.fatal = false;
                const status = shell.exec(command).code;
                shell.config.fatal = fatal;
                return status;
            };

            if (runPatch('--dry-run --batch --forward') === 0) {
                if (runPatch('--batch --forward') !== 0) {
                    throw new Error(`Failed to apply CPython patch: ${patch}`);
                }
            } else if (runPatch('--dry-run --batch --reverse') === 0) {
                console.log(`Patch already applied: ${path.basename(patch)}`);
            } else {
                throw new Error(`CPython patch does not match ${PYTHON_VERSION}: ${patch}`);
            }
        }
    } else {
        console.log('No patches found.');
    }
}

// 3. Build native Python (needed for cross-compilation bootstrapping)
let buildPython = path.join(BUILD_NATIVE_DIR, 'install', 'bin', 'python3');
const windowsMake = process.platform === 'win32' ? ensureWindowsMake() : null;
if (process.platform === 'win32') {
    const emsdkPython = process.env.EMSDK_PYTHON;
    if (!emsdkPython || !fs.existsSync(emsdkPython)) {
        throw new Error('EMSDK_PYTHON is required to bootstrap CPython on Windows');
    }
    const version = execFileSync(emsdkPython, ['--version'], { encoding: 'utf8' }).trim();
    if (!version.startsWith(`Python ${PYTHON_MM}.`)) {
        throw new Error(`CPython ${PYTHON_VERSION} requires a Python ${PYTHON_MM} host, got ${version}`);
    }
    buildPython = emsdkPython;
    console.log(`Using EMSDK host ${version}: ${buildPython}`);
} else {
    console.log('Building native Python...');
    shell.mkdir('-p', BUILD_NATIVE_DIR);
    shell.cd(BUILD_NATIVE_DIR);
    if (!fs.existsSync(path.join(BUILD_NATIVE_DIR, 'Makefile'))) {
        shell.exec(`"${path.join(SOURCE_DIR, 'configure')}" --prefix="${path.join(BUILD_NATIVE_DIR, 'install')}"`);
    }
    shell.exec(`make -j${CONCURRENCY}`);
    shell.exec('make install');
}

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

    const buildTriple = process.platform === 'win32'
        ? runPosix('./config.guess', SOURCE_DIR).stdout
        : shell.exec(`"${path.join(SOURCE_DIR, 'config.guess')}"`, { silent: true }).stdout.trim();
    const configureArgs = [
        quotePosix(path.join(SOURCE_DIR, 'configure')),
        '--host=wasm32-unknown-emscripten',
        `--build=${buildTriple}`,
        '--with-emscripten-target=browser',
        quotePosix(`--with-build-python=${buildPython}`),
        '--prefix=/usr',
        '--disable-ipv6',
        '--disable-test-modules',
    ].join(' ');
    const configureCmd = process.platform === 'win32'
        ? [
            quotePosix(process.env.EMSDK_PYTHON ?? ''),
            quotePosix(path.join(process.env.EMSDK ?? '', 'upstream', 'emscripten', 'emconfigure.py')),
            quotePosix(resolveGitBash()),
            configureArgs,
        ].join(' ')
        : `emconfigure ${configureArgs}`;

    console.log(configureCmd);
    if (process.platform === 'win32') runPosix(configureCmd, BUILD_WASM_DIR);
    else shell.exec(configureCmd);
}

// Build WASM
if (process.platform === 'win32') {
    const emmake = path.join(process.env.EMSDK ?? '', 'upstream', 'emscripten', 'emmake.py');
    runPosix(
        `${quotePosix(process.env.EMSDK_PYTHON ?? '')} ${quotePosix(emmake)} ${quotePosix(windowsMake ?? '')} -j${CONCURRENCY} ${windowsMakeOverrides()}`,
        BUILD_WASM_DIR,
    );
} else {
    shell.exec(`emmake make -j${CONCURRENCY}`);
}

// 5. Install to sysroot staging area
console.log('Installing to sysroot-staging...');
shell.mkdir('-p', SYSROOT_STAGING);

const installCmd = process.platform === 'win32'
    ? `${quotePosix(process.env.EMSDK_PYTHON ?? '')} ${quotePosix(path.join(process.env.EMSDK ?? '', 'upstream', 'emscripten', 'emmake.py'))} ${quotePosix(windowsMake ?? '')} install ${quotePosix(`DESTDIR=${SYSROOT_STAGING}`)} V=1 ${windowsMakeOverrides()}`
    : `emmake make install DESTDIR="${SYSROOT_STAGING}" V=1`;
console.log(installCmd);
if (process.platform === 'win32') runPosix(installCmd, BUILD_WASM_DIR);
else shell.exec(installCmd);

// 6. Deploy sysroot-staging to sysroot
// The Python stdlib files in sysroot/usr/lib/pythonX.Y/ are served via
// brotli-compressed tar bundles (generated by build:bundles), so there
// is no need for a separate stdlib zip file.
const stagingUsr = path.join(SYSROOT_STAGING, 'usr');
const SYSROOT_USR = path.join(P.sysroot, 'usr');
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
    for (const entry of fs.readdirSync(stagingUsr)) {
        fs.cpSync(path.join(stagingUsr, entry), path.join(SYSROOT_USR, entry), {
            recursive: true,
            force: true,
        });
    }
}

// 8. Build python.wasm — standalone module that statically links libpython
console.log('Building python.wasm (standalone module)...');
const BUILD_DIR = P.tools;
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
const cmd = cmdParts.join(' ');

console.log(cmd);
shell.exec(cmd);
console.log(`Created ${pythonWasm} + ${pythonMjs}`);

// NOTE: Post-processing patches (ENV merge, systemCallback, Asyncify
// hooks) are applied by scripts/patch-glue.ts
// which runs as the `patch:glue` step in the build:all pipeline.
// This keeps patching logic centralized and applies to ALL .mjs files.

// 9. Deploy to sysroot
const SYSROOT_LIB = path.join(P.sysroot, 'usr', 'lib');
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
