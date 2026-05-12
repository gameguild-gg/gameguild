/**
 * Build Binaryen tools as standalone Emscripten WASM modules.
 *
 * Each tool (wasm-opt, wasm-as, etc.) is compiled as a standalone module
 * that statically links libbinaryen.a. No SIDE_MODULE, no shared libraries.
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';
import { standaloneFlags } from './lib/emcc-flags.ts';
import { setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { PINNED } from './lib/pinned-versions.ts';

enableBuildKeepalive('build-binaryen');

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = process.cwd();

// Ensure shell commands fail on error
shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION;

// Setup EMSDK first
setupEmsdk(EMSDK_VERSION);

const BINARYEN_VERSION = process.env.BINARYEN_VERSION || PINNED.BINARYEN_VERSION;

const USERLAND_DIR = path.join(ROOT, 'userland', 'binaryen');
const SOURCE_DIR = path.join(USERLAND_DIR, `binaryen-version_${BINARYEN_VERSION}`);
const PATCHES_DIR = path.join(USERLAND_DIR, 'patches');
const BUILD_WASM_DIR = path.join(SOURCE_DIR, 'build-wasm');
const OUTPUT_DIR = path.join(ROOT, 'build');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const CONCURRENCY = os.cpus().length;

// 4 MB stack — binaryen tools do deep recursion on ASTs.
const STANDALONE_FLAGS = standaloneFlags({ stackSize: 4 * 1024 * 1024, asyncifyStackSize: 65536 });

shell.mkdir('-p', USERLAND_DIR);
shell.mkdir('-p', OUTPUT_DIR);
shell.mkdir('-p', SYSROOT_LIB);

const GITHUB_AUTH = process.env.GITHUB_TOKEN
    ? `-H "Authorization: token ${process.env.GITHUB_TOKEN}"`
    : '';

// 1. Download Binaryen source
// Validate the source dir by checking for CMakeLists.txt — a missing file
// indicates an incomplete or corrupt cache entry that must be re-downloaded.
const isBinaryenSourceValid = fs.existsSync(path.join(SOURCE_DIR, 'CMakeLists.txt'));
if (!isBinaryenSourceValid) {
    if (fs.existsSync(SOURCE_DIR)) {
        console.log(`Removing incomplete Binaryen source dir: ${path.basename(SOURCE_DIR)}`);
        shell.rm('-rf', SOURCE_DIR);
    }
    console.log(`Downloading Binaryen ${BINARYEN_VERSION}...`);
    shell.cd(USERLAND_DIR);
    const tarball = `version_${BINARYEN_VERSION}.tar.gz`;
    shell.exec(
        `curl -fSL --http1.1 --retry 8 --retry-all-errors --retry-delay 2 ${GITHUB_AUTH} -o "${tarball}" "https://github.com/WebAssembly/binaryen/archive/refs/tags/${tarball}" || ` +
        `curl -fSL --http1.1 --retry 8 --retry-all-errors --retry-delay 2 ${GITHUB_AUTH} -o "${tarball}" "https://codeload.github.com/WebAssembly/binaryen/tar.gz/refs/tags/version_${BINARYEN_VERSION}"`
    );
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
    }
}

// 3. Build binaryen as a static library (no SIDE_MODULE)
console.log('Cross-compiling libbinaryen.a (static library)...');

if (fs.existsSync(BUILD_WASM_DIR)) {
    console.log('Cleaning build-wasm directory...');
    shell.rm('-rf', BUILD_WASM_DIR);
}
shell.mkdir('-p', BUILD_WASM_DIR);

// Configure CMake — static build, no SIDE_MODULE
if (!fs.existsSync(path.join(BUILD_WASM_DIR, 'Makefile'))) {
    const cmakeCmd = `emcmake cmake -S . -B "${BUILD_WASM_DIR}" \
    -DCMAKE_BUILD_TYPE=MinSizeRel \
    -DBUILD_TESTS=OFF \
    -DBYN_ENABLE_LTO=OFF \
    -DBUILD_SHARED_LIBS=OFF \
    -DEMSCRIPTEN_ENABLE_WASM_EH=OFF`;

    console.log(cmakeCmd);
    shell.exec(cmakeCmd);
}

// Build static library
shell.exec(`emmake make -C "${BUILD_WASM_DIR}" -j${CONCURRENCY} binaryen`);

// 4. Find the static library
const staticLib = path.join(BUILD_WASM_DIR, 'lib', 'libbinaryen.a');
if (!fs.existsSync(staticLib)) {
    console.error('ERROR: libbinaryen.a not found in build-wasm/lib/');
    process.exit(1);
}
console.log(`Static library: ${staticLib}`);

// 5. Build each tool as a standalone Emscripten module
console.log('Building Binaryen tools as standalone WASM modules...');
const TOOLS = ['wasm-opt', 'wasm-as', 'wasm-ctor-eval', 'wasm-emscripten-finalize', 'wasm-metadce'];

// wasm-opt needs the fuzzing sources (TranslateToFuzzReader etc.)
const FUZZING_DIR = path.join(SOURCE_DIR, 'src', 'tools', 'fuzzing');
const FUZZING_SOURCES = fs.existsSync(FUZZING_DIR)
    ? fs.readdirSync(FUZZING_DIR).filter(f => f.endsWith('.cpp')).map(f => `"${path.join(FUZZING_DIR, f)}"`)
    : [];

// Map tool -> extra source files required
const TOOL_EXTRA_SOURCES: Record<string, string[]> = {
    'wasm-opt': FUZZING_SOURCES,
};

for (const tool of TOOLS) {
    const toolSrc = path.join(SOURCE_DIR, 'src', 'tools', `${tool}.cpp`);
    if (!fs.existsSync(toolSrc)) {
        console.warn(`WARNING: ${toolSrc} not found, skipping`);
        continue;
    }

    const toolWasm = path.join(OUTPUT_DIR, `${tool}.wasm`);
    const toolMjs = path.join(OUTPUT_DIR, `${tool}.mjs`);
    console.log(`Building ${tool} (standalone)...`);

    const extraSources = TOOL_EXTRA_SOURCES[tool] || [];
    const extraIncludes = extraSources.length > 0 ? [`-I "${FUZZING_DIR}"`] : [];

    const cmdParts = [
        `em++ "${toolSrc}"`,
        ...extraSources,
        `"${staticLib}"`,
        `-I "${path.join(SOURCE_DIR, 'src')}"`,
        `-I "${path.join(BUILD_WASM_DIR, 'src')}"`,
        `-I "${path.join(SOURCE_DIR, 'third_party', 'FP16', 'include')}"`,
        ...extraIncludes,
        STANDALONE_FLAGS,
        '-std=c++20',
        '-Os',
        `-o "${toolMjs}"`,
    ];
    const cmd = cmdParts.join(' \\\n    ');

    console.log(cmd);
    shell.exec(cmd);

    if (!fs.existsSync(toolWasm)) {
        console.error(`ERROR: ${toolWasm} not generated`);
        process.exit(1);
    }
    console.log(`Created ${toolWasm} + ${toolMjs}`);
}

// 6. Deploy to sysroot (no libbinaryen.so.wasm — each tool is self-contained)
console.log('Deploying to sysroot...');
for (const tool of TOOLS) {
    for (const ext of ['.wasm', '.mjs']) {
        const src = path.join(OUTPUT_DIR, `${tool}${ext}`);
        if (fs.existsSync(src)) {
            shell.cp('-f', src, SYSROOT_LIB);
        }
    }
}

console.log('>>> Binaryen build complete.');
