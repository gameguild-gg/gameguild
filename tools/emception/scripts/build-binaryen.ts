/**
 * Build Binaryen tools as standalone Emscripten WASM modules.
 *
 * Each tool (wasm-opt, wasm-as, etc.) is compiled as a standalone module
 * that statically links libbinaryen.a. No SIDE_MODULE, no shared libraries.
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import { toolchainPaths } from './toolchain/paths.ts';
import shell from 'shelljs';
import { fileURLToPath } from 'url';
import { standaloneFlags } from './lib/emcc-flags.ts';
import { setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { loadToolchainStateSync, lockedVersion } from './toolchain/config.ts';
import { ensureLockedSource } from './toolchain/sources.ts';

enableBuildKeepalive('build-binaryen');

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

const BINARYEN_VERSION = lockedVersion(lock, 'binaryen');

const SOURCE_ROOT = path.join(P.sources, 'binaryen');
const SOURCE_DIR = path.join(SOURCE_ROOT, `binaryen-${BINARYEN_VERSION}`);
const PATCHES_DIR = path.join(P.overlays, 'binaryen', 'patches');
const BUILD_WASM_DIR = path.join(P.builds, 'binaryen', 'wasm');
const OUTPUT_DIR = P.tools;
const SYSROOT_LIB = path.join(P.sysroot, 'usr', 'lib');
const CONCURRENCY = os.cpus().length;

// 4 MB stack — binaryen tools do deep recursion on ASTs.
const STANDALONE_FLAGS = standaloneFlags({ stackSize: 4 * 1024 * 1024, asyncifyStackSize: 65536 });

function linkArtifactsAreCurrent(
    outputs: readonly string[],
    inputs: readonly string[],
    signatureFile: string,
    signature: string,
): boolean {
    const previousSignature = fs.existsSync(signatureFile)
        ? fs.readFileSync(signatureFile, 'utf8')
        : undefined;
    if (previousSignature !== undefined && previousSignature !== signature) {
        return false;
    }
    if (outputs.some(output => !fs.existsSync(output) || fs.statSync(output).size === 0)) {
        return false;
    }
    const oldestOutput = Math.min(...outputs.map(output => fs.statSync(output).mtimeMs));
    return inputs.every(input => fs.existsSync(input) && fs.statSync(input).mtimeMs <= oldestOutput);
}

shell.mkdir('-p', SOURCE_ROOT);
shell.mkdir('-p', BUILD_WASM_DIR);
shell.mkdir('-p', OUTPUT_DIR);
shell.mkdir('-p', SYSROOT_LIB);

ensureLockedSource(ROOT, lock, 'binaryen', SOURCE_DIR, 'CMakeLists.txt');

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

shell.mkdir('-p', BUILD_WASM_DIR);

// Configure CMake — static build, no SIDE_MODULE
const cmakeCmd = `emcmake cmake -S . -B "${BUILD_WASM_DIR}" \
    -DCMAKE_BUILD_TYPE=MinSizeRel \
    -DBUILD_TESTS=OFF \
    -DBYN_ENABLE_LTO=OFF \
    -DBUILD_SHARED_LIBS=OFF \
    -DEMSCRIPTEN_ENABLE_WASM_EH=OFF`;

console.log(cmakeCmd);
shell.exec(cmakeCmd);

// Build static library
shell.exec(`cmake --build "${BUILD_WASM_DIR}" --parallel ${CONCURRENCY} --target binaryen`);

// 4. Find the static library
const staticLib = path.join(BUILD_WASM_DIR, 'lib', 'libbinaryen.a');
if (!fs.existsSync(staticLib)) {
    console.error('ERROR: libbinaryen.a not found in build-wasm/lib/');
    process.exit(1);
}
console.log(`Static library: ${staticLib}`);

// 5. Build each tool as a standalone Emscripten module
console.log('Building Binaryen tools as standalone WASM modules...');
const TOOLS: readonly string[] = ['wasm-opt', 'wasm-as', 'wasm-ctor-eval', 'wasm-emscripten-finalize', 'wasm-metadce'];

// wasm-opt needs the fuzzing sources (TranslateToFuzzReader etc.)
const FUZZING_DIR = path.join(SOURCE_DIR, 'src', 'tools', 'fuzzing');
const FUZZING_SOURCES = fs.existsSync(FUZZING_DIR)
    ? fs.readdirSync(FUZZING_DIR).filter(f => f.endsWith('.cpp')).map(f => path.join(FUZZING_DIR, f))
    : [];

// Map tool -> extra source files required
const TOOL_EXTRA_SOURCES: Readonly<Record<string, readonly string[]>> = {
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
    const extraSources = TOOL_EXTRA_SOURCES[tool] || [];
    const extraIncludes = extraSources.length > 0 ? [`-I "${FUZZING_DIR}"`] : [];
    const inputs = [toolSrc, ...extraSources, staticLib];
    const signatureFile = path.join(OUTPUT_DIR, `${tool}-link.signature`);
    const signature = JSON.stringify({
        schemaVersion: 1,
        tool,
        binaryenVersion: BINARYEN_VERSION,
        emsdkVersion: EMSDK_VERSION,
        standaloneFlags: STANDALONE_FLAGS,
        languageStandard: 'c++20',
        optimization: 'Os',
        extraIncludes,
        extraSources,
    });

    if (linkArtifactsAreCurrent([toolMjs, toolWasm], inputs, signatureFile, signature)) {
        fs.writeFileSync(signatureFile, signature);
        console.log(`${tool} link artifacts are current.`);
        continue;
    }

    console.log(`Building ${tool} (standalone)...`);

    const cmdParts = [
        `em++ "${toolSrc}"`,
        ...extraSources.map(source => `"${source}"`),
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
    const cmd = cmdParts.join(' ');

    console.log(cmd);
    shell.exec(cmd);

    if (!fs.existsSync(toolWasm)) {
        console.error(`ERROR: ${toolWasm} not generated`);
        process.exit(1);
    }
    fs.writeFileSync(signatureFile, signature);
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
