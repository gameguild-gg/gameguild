import * as fs from 'fs';
import * as path from 'path';
import { toolchainPaths } from './toolchain/paths.ts';
import shell from 'shelljs';
import { fileURLToPath } from 'url';
import { getEmsdkDir, setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { loadToolchainStateSync, lockedVersion } from './toolchain/config.ts';
import { copyRuntimeDirectoryContents, copyRuntimeSourceTree } from './lib/runtime-source-tree.ts';

enableBuildKeepalive('populate-sysroot');

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const ROOT = path.resolve(__dirname, '..');
const P = toolchainPaths(ROOT);
const EMSDK_DIR = getEmsdkDir();
const SYSROOT = P.sysroot;
const EMSDK_VERSION = lockedVersion(loadToolchainStateSync(ROOT).lock, 'emsdk');
const VIRTUAL_LINKS_FILE = path.join(SYSROOT, '.emception-symlinks.json');
const virtualLinks: Record<string, string> = {};

function declareToolLink(command: string, moduleName: string): void {
    fs.rmSync(path.join(SYSROOT, 'usr/bin', command), { force: true });
    virtualLinks[`/usr/bin/${command}`] = `/usr/lib/${moduleName}.wasm`;
}

// Ensure we fail on error
shell.config.fatal = true;

console.log('=== Populating sysroot for browser toolchain ===');
console.log(`Project: ${ROOT}`);
console.log(`Sysroot: ${SYSROOT}`);
console.log(`Emsdk version: ${EMSDK_VERSION}`);
console.log('');

// 1. Install Emscripten SDK (using shared lib)
setupEmsdk(EMSDK_VERSION);

// Locate directories
const EMSCRIPTEN_ROOT = path.resolve(EMSDK_DIR, 'upstream', 'emscripten');
const LLVM_DIR = path.resolve(EMSDK_DIR, 'upstream', 'bin');
const BINARYEN_DIR = path.resolve(EMSDK_DIR, 'upstream');

console.log('');
console.log(`>> Emscripten root: ${EMSCRIPTEN_ROOT}`);
console.log(`>> LLVM dir: ${LLVM_DIR}`);
console.log('');

// 2. Ensure sysroot directories exist
shell.mkdir('-p', path.join(SYSROOT, 'usr/bin'));
shell.mkdir('-p', path.join(SYSROOT, 'usr/lib/emscripten'));
shell.mkdir('-p', path.join(SYSROOT, 'usr/lib/binaryen'));
shell.mkdir('-p', path.join(SYSROOT, 'usr/include'));
shell.mkdir('-p', path.join(SYSROOT, 'etc'));
shell.mkdir('-p', path.join(SYSROOT, 'tmp'));
shell.mkdir('-p', path.join(SYSROOT, 'home/user'));

// 3. Copy Emscripten toolchain driver scripts
console.log('>> Copying Emscripten driver scripts...');
if (fs.existsSync(path.join(EMSCRIPTEN_ROOT, 'tools'))) {
    copyRuntimeSourceTree(path.join(EMSCRIPTEN_ROOT, 'tools'), path.join(SYSROOT, 'usr/lib/emscripten/tools'));
}
if (fs.existsSync(path.join(EMSCRIPTEN_ROOT, 'src'))) {
    copyRuntimeSourceTree(path.join(EMSCRIPTEN_ROOT, 'src'), path.join(SYSROOT, 'usr/lib/emscripten/src'));
}
// third_party contains pure-Python dependencies required at runtime (e.g. leb128, ply)
if (fs.existsSync(path.join(EMSCRIPTEN_ROOT, 'third_party'))) {
    console.log('>> Copying Emscripten third_party dependencies...');
    copyRuntimeSourceTree(path.join(EMSCRIPTEN_ROOT, 'third_party'), path.join(SYSROOT, 'usr/lib/emscripten/third_party'));
}

// Core Python driver files + data files required by emcc
const pythonFiles = [
    'emcc.py', 'embuilder.py', 'emmake.py', 'emcmake.py', 'emrun.py', 'emconfigure.py',
    'emranlib.py', 'emar.py', '__init__.py', 'shared.py', 'settings.py', 'cache.py',
    'config.py', 'diagnostics.py', 'feature_matrix.py', 'filelock.py',
    'tempfiles.py', 'response_file.py', 'utils.py',
    'emscripten-version.txt',
];

pythonFiles.forEach(f => {
    const src = path.join(EMSCRIPTEN_ROOT, f);
    if (fs.existsSync(src)) {
        shell.cp(src, path.join(SYSROOT, 'usr/lib/emscripten/'));
    }
});

// Patch filelock.py: in the browser WASM environment, fcntl is unavailable
// so the module falls back to SoftFileLock and emits a noisy warning.
// Suppress it since soft locks work fine for our single-threaded context.
const filelockPath = path.join(SYSROOT, 'usr/lib/emscripten/tools/filelock.py');
if (fs.existsSync(filelockPath)) {
    let filelockSrc = fs.readFileSync(filelockPath, 'utf-8');
    filelockSrc = filelockSrc.replace(
        'warnings.warn("only soft file lock is available")',
        'pass  # suppressed: soft file lock is fine in browser WASM',
    );
    fs.writeFileSync(filelockPath, filelockSrc);
    console.log('>> Patched filelock.py to suppress soft lock warning.');
}

// Patch building.py: when compiler.mjs is skipped in the browser toolchain,
// external_symbols is empty, which causes lld_flags_for_executable to skip
// both the #STUB library and --import-undefined, making wasm-ld fail on
// emscripten JS library symbols (emscripten_sleep, emscripten_get_now, etc.).
// Always add --import-undefined when external_symbols is empty so wasm-ld
// treats unmapped JS-library imports as host imports from the JS glue.
const buildingPath = path.join(SYSROOT, 'usr/lib/emscripten/tools/building.py');
if (fs.existsSync(buildingPath)) {
    let buildingSrc = fs.readFileSync(buildingPath, 'utf-8');
    buildingSrc = buildingSrc.replace(
        '  if not settings.ERROR_ON_UNDEFINED_SYMBOLS:\n    cmd.append(\'--import-undefined\')',
        '  if not external_symbols or not settings.ERROR_ON_UNDEFINED_SYMBOLS:\n    cmd.append(\'--import-undefined\')',
    );
    fs.writeFileSync(buildingPath, buildingSrc);
    console.log('>> Patched building.py: --import-undefined when external_symbols is empty.');
}

// system/include directory — copy to both /usr/include/ (traditional Unix
// location) AND /usr/lib/emscripten/system/include/ (where emcc's
// ensure_sysroot / install_system_headers expects them under EMSCRIPTEN_ROOT).
if (fs.existsSync(path.join(EMSCRIPTEN_ROOT, 'system/include'))) {
    console.log('>> Copying system headers...');
    copyRuntimeDirectoryContents(path.join(EMSCRIPTEN_ROOT, 'system/include'), path.join(SYSROOT, 'usr/include'));
    // Emscripten also looks for system/include under EMSCRIPTEN_ROOT
    shell.mkdir('-p', path.join(SYSROOT, 'usr/lib/emscripten/system'));
    copyRuntimeSourceTree(path.join(EMSCRIPTEN_ROOT, 'system/include'), path.join(SYSROOT, 'usr/lib/emscripten/system/include'));
}

// system/lib directory
if (fs.existsSync(path.join(EMSCRIPTEN_ROOT, 'system/lib'))) {
    console.log('>> Copying system libraries...');
    shell.mkdir('-p', path.join(SYSROOT, 'usr/lib/emscripten/system'));
    copyRuntimeSourceTree(path.join(EMSCRIPTEN_ROOT, 'system/lib'), path.join(SYSROOT, 'usr/lib/emscripten/system/lib'));
}

// 4. Copy Emscripten cache (precompiled system .a libraries)
console.log('>> Building and copying Emscripten cache...');
try {
    // We need to run embuilder from EMSCRIPTEN_ROOT or add it to path.
    // It is a python script. We can run it with python.
    // Or just run 'emcc --clear-cache' and let it rebuild?
    // The original script ran `embuilder build ...`
    // We can try to run it via shell.exec using the env we set.
    shell.exec(`python3 "${path.join(EMSCRIPTEN_ROOT, 'embuilder.py')}" build libc libc++ libc++abi libcompiler_rt libdlmalloc libemmalloc libsockets libhtml5 libfetch libal libGL`, { silent: false });
} catch (e) {
    console.warn('embuilder failed, but continuing (cache might be incomplete)', e);
}

const CACHE_DIR = path.join(EMSCRIPTEN_ROOT, 'cache');
if (fs.existsSync(path.join(CACHE_DIR, 'sysroot'))) {
    console.log('>> Copying cached sysroot libraries...');
    if (fs.existsSync(path.join(CACHE_DIR, 'sysroot/lib'))) {
        copyRuntimeDirectoryContents(
            path.join(CACHE_DIR, 'sysroot/lib'),
            path.join(SYSROOT, 'usr/lib/emscripten/cache-lib'),
        );
    }
    if (fs.existsSync(path.join(CACHE_DIR, 'sysroot/include'))) {
        copyRuntimeDirectoryContents(path.join(CACHE_DIR, 'sysroot/include'), path.join(SYSROOT, 'usr/include'));
    }
}

// 5. Copy Binaryen tools info
if (fs.existsSync(path.join(BINARYEN_DIR, 'lib/binaryen'))) {
    console.log('>> Copying Binaryen support files...');
    copyRuntimeDirectoryContents(path.join(BINARYEN_DIR, 'lib/binaryen'), path.join(SYSROOT, 'usr/lib/binaryen'));
}

// 5b. Copy clang resource-dir (builtin headers like stddef.h, stdarg.h, *intrin.h).
// These are required when invoking `clang -cc1` directly because cc1 mode
// bypasses the driver's auto-resource-dir detection. Without these, libc++
// headers fail to compile (they include <stddef.h> etc. from clang builtins).
// We auto-detect the major version (e.g. 23) under upstream/lib/clang/.
const CLANG_RESOURCE_ROOT = path.join(EMSDK_DIR, 'upstream/lib/clang');
if (fs.existsSync(CLANG_RESOURCE_ROOT)) {
    const versions = fs.readdirSync(CLANG_RESOURCE_ROOT).filter(v => /^\d+$/.test(v));
    for (const ver of versions) {
        const srcInclude = path.join(CLANG_RESOURCE_ROOT, ver, 'include');
        if (!fs.existsSync(srcInclude)) continue;
        const destDir = path.join(SYSROOT, 'usr/lib/clang', ver, 'include');
        console.log(`>> Copying clang ${ver} resource-dir headers to ${destDir}...`);
        copyRuntimeDirectoryContents(srcInclude, destDir);
    }
} else {
    console.warn(`>> WARN: clang resource-dir not found at ${CLANG_RESOURCE_ROOT}`);
}

// 6. Create shell wrappers for /usr/bin
console.log('>> Creating shell wrappers...');

function createWrapper(tool: string, importName: string) {
    const content = `#!/usr/bin/env python3
import sys
sys.path.insert(0, '/usr/lib/emscripten')
import ${importName}
sys.exit(${importName}.run(sys.argv))
`;
    const dest = path.join(SYSROOT, 'usr/bin', tool);
    fs.writeFileSync(dest, content);
    shell.chmod('+x', dest);
}

createWrapper('emcc', 'emcc');
createWrapper('em++', 'emcc');
createWrapper('emar', 'emar');
createWrapper('emranlib', 'emranlib');

// Virtual links for LLVM tools. They are materialized by the release manifest,
// so building the sysroot never depends on host symlink privileges.
// In the micro-kernel architecture, tools are standalone .wasm modules.
// clang++ and wasm-ld are aliases handled by the TypeScript tool runner,
// so they don't need separate symlinks on disk.
const llvmTools = ['clang', 'lld', 'llvm-ar', 'llvm-nm', 'llvm-objcopy', 'llc'];
llvmTools.forEach(tool => declareToolLink(tool, tool));
// clang++ -> clang, wasm-ld -> lld (aliases)
const llvmAliases: ReadonlyArray<readonly [string, string]> = [['clang++', 'clang'], ['wasm-ld', 'lld']];
for (const [alias, target] of llvmAliases) {
    declareToolLink(alias, target);
}

const binaryenTools = ['wasm-opt', 'wasm-as', 'wasm-emscripten-finalize', 'wasm-metadce', 'wasm-ctor-eval'];
binaryenTools.forEach(tool => declareToolLink(tool, tool));
declareToolLink('python3', 'python');

// 6b. Virtual links for build tools (cmake, curl)
const buildTools = ['cmake', 'curl'];
buildTools.forEach(tool => {
    const wasmPath = path.join(SYSROOT, 'usr/lib', `${tool}.wasm`);
    if (fs.existsSync(wasmPath)) {
        declareToolLink(tool, tool);
    }
});
fs.writeFileSync(VIRTUAL_LINKS_FILE, `${JSON.stringify(virtualLinks, null, 2)}\n`);

// 7. Write emscripten.config
const configContent = `import os

EMSCRIPTEN_ROOT = '/usr/lib/emscripten'
LLVM_ROOT = '/usr/bin'
BINARYEN_ROOT = '/usr'
NODE_JS = '/usr/bin/node'
PYTHON = '/usr/bin/python3'

CACHE = os.path.expanduser('~/.emscripten_cache')
FROZEN_CACHE = True
COMPILER_OPTS = []
`;
fs.writeFileSync(path.join(SYSROOT, 'etc/emscripten.config'), configContent);

// Pre-populate the SDL3 port cache marker so -sUSE_SDL=3 works under
// FROZEN_CACHE=True.  Without this file emscripten tries to download SDL3
// at browser runtime and fails because the cache is read-only.
// The path alias in tool-runner.ts maps
//   /home/user/.emscripten_cache/ports  →  /usr/lib/emscripten_ports
const sdl3PortPy = path.join(SYSROOT, 'usr/lib/emscripten/tools/ports/sdl3.py');
if (fs.existsSync(sdl3PortPy)) {
    const src = fs.readFileSync(sdl3PortPy, 'utf-8');
    const m = src.match(/^VERSION\s*=\s*['"](.*?)['"]/m);
    if (m) {
        const portUrl = `https://github.com/libsdl-org/SDL/archive/release-${m[1]}.zip`;
        const portDir = path.join(SYSROOT, 'usr/lib/emscripten_ports/sdl3');
        shell.mkdir('-p', portDir);
        fs.writeFileSync(path.join(portDir, '.emscripten_url'), portUrl);
        console.log(`>> SDL3 port cache marker written (${portUrl})`);
    } else {
        console.warn('>> Warning: could not parse VERSION from sdl3.py — port cache marker not written.');
        console.warn('   -sUSE_SDL=3 will fail at runtime with FROZEN_CACHE error.');
    }
} else {
    console.warn(`>> Warning: sdl3.py not found at ${sdl3PortPy} — port cache marker not written.`);
}

// Ensure libSDL3.a is present in the emscripten cache-lib path.
// tool-runner.ts aliases /home/user/.emscripten_cache/sysroot/lib → /usr/lib/emscripten/cache-lib
// so cache.py's FROZEN_CACHE check resolves libSDL3.a here.
// build-sdl3.ts should have already done this; we repeat it here as a safety net for
// incremental builds where only populate-sysroot.ts is re-run.
const libSdl3Src = path.join(SYSROOT, 'usr/lib/libSDL3.a');
const libSdl3CacheDir = path.join(SYSROOT, 'usr/lib/emscripten/cache-lib/wasm32-emscripten');
const libSdl3Dst = path.join(libSdl3CacheDir, 'libSDL3.a');
if (fs.existsSync(libSdl3Src)) {
    shell.mkdir('-p', libSdl3CacheDir);
    shell.cp('-f', libSdl3Src, libSdl3CacheDir);
    console.log(`>> Copied libSDL3.a to cache-lib path: ${path.relative(SYSROOT, libSdl3Dst)}`);
} else {
    console.warn(`>> Warning: libSDL3.a not found at ${libSdl3Src} — FROZEN_CACHE check for SDL3 may fail.`);
    console.warn('   Run build:sdl3 first if you need SDL3 support.');
}

console.log('');
console.log('=== Sysroot population complete ===');
console.log('');
