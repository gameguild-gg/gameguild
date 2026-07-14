/**
 * Build Brotli for both Node.js CLI and WebAssembly browser decompression.
 *
 * Downloads the latest Brotli release, compiles the CLI natively (using gcc),
 * and generates the WASM module for browser use via Emscripten.
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';
import { setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { PINNED } from './lib/pinned-versions.ts';

enableBuildKeepalive('build-brotli');

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = process.cwd();

// Ensure shell commands fail on error
shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION;

// Brotli version to use
const BROTLI_VERSION = process.env.BROTLI_VERSION || PINNED.BROTLI_VERSION;

// Directories
const USERLAND_DIR = path.join(ROOT, 'userland', 'brotli');
const SOURCE_DIR = path.join(USERLAND_DIR, `brotli-v${BROTLI_VERSION}`);
const BUILD_DIR = path.join(ROOT, 'build');
const CDN_DIR = path.join(BUILD_DIR, 'cdn');

// Setup EMSDK first
setupEmsdk(EMSDK_VERSION);

// 1. Download Brotli source if not already present
if (!fs.existsSync(SOURCE_DIR)) {
  console.log(`Downloading Brotli v${BROTLI_VERSION}...`);
  shell.mkdir('-p', USERLAND_DIR);
  shell.cd(USERLAND_DIR);
  const tarball = `v${BROTLI_VERSION}.tar.gz`;
  shell.exec(`curl -fSL -o "${tarball}" "https://github.com/google/brotli/archive/refs/tags/v${BROTLI_VERSION}.tar.gz"`);
  shell.exec(`tar xzf "${tarball}"`);
  shell.rm(tarball);
}

// Rename the extracted directory (brotli-<version> -> brotli-v<version>)
const extractedDir = path.join(USERLAND_DIR, `brotli-${BROTLI_VERSION}`);
if (fs.existsSync(extractedDir) && !fs.existsSync(SOURCE_DIR)) {
  shell.mv(extractedDir, SOURCE_DIR);
}

shell.cd(SOURCE_DIR);

// 2. Create build directories
shell.mkdir('-p', path.join(SOURCE_DIR, 'build-cli'));
shell.mkdir('-p', BUILD_DIR);
shell.mkdir('-p', CDN_DIR);

// 3. Build Brotli CLI for Node.js (native compilation with gcc)
console.log('Building Brotli CLI (native, for Node.js)...');

const CLI_BUILD_DIR = path.join(SOURCE_DIR, 'build-cli');
shell.rm('-rf', CLI_BUILD_DIR);
shell.mkdir('-p', CLI_BUILD_DIR);
shell.cd(SOURCE_DIR);

// Configure with CMake
const cmakeCmd = `cmake -S "${SOURCE_DIR}" -B "${CLI_BUILD_DIR}" \
    -DCMAKE_BUILD_TYPE=MinSizeRel \
    -DBUILD_SHARED_LIBS=OFF \
    -DBROTLI_BUILD_CLI=ON \
    -DBROTLI_BUILD_TESTS=OFF`;

console.log(cmakeCmd);
shell.exec(cmakeCmd);
shell.cd(CLI_BUILD_DIR);

// Build the CLI
shell.exec(`make -C "${CLI_BUILD_DIR}" -j${os.cpus().length}`);

// Copy the CLI to build/brotli
const cliOutput = path.join(CLI_BUILD_DIR, 'brotli');
if (fs.existsSync(cliOutput)) {
  shell.cp('-f', cliOutput, path.join(BUILD_DIR, 'brotli'));
  shell.exec(`chmod +x "${path.join(BUILD_DIR, 'brotli')}"`);
  console.log(`Copied Brotli CLI to ${BUILD_DIR}/brotli`);
} else {
  console.error('ERROR: Brotli CLI not found in build directory');
  process.exit(1);
}

// 4. Build Brotli WASM for browser decompression
console.log('Building Brotli WASM (browser)...');

const WASM_BUILD_DIR = path.join(SOURCE_DIR, 'build-wasm');
// shelljs.rm -rf chokes on some emcc-produced files; use the real /bin/rm.
shell.exec(`rm -rf "${WASM_BUILD_DIR}"`);
shell.mkdir('-p', WASM_BUILD_DIR);
shell.cd(SOURCE_DIR);

// Configure with CMake - disable CLI, enable library build only
const wasmCmakeCmd = `emcmake cmake -S "${SOURCE_DIR}" -B "${WASM_BUILD_DIR}" \
    -DCMAKE_BUILD_TYPE=MinSizeRel \
    -DBUILD_SHARED_LIBS=OFF \
    -DBROTLI_BUILD_CLI=OFF \
    -DBROTLI_BUILD_TESTS=OFF \
    -DBROTLI_BUILD_BROTLI_CMD=OFF`;

console.log(wasmCmakeCmd);
shell.exec(wasmCmakeCmd);
shell.cd(WASM_BUILD_DIR);

// Build only the static libraries (libbrotlidec.a + libbrotlicommon.a).
// Brotli's CMakeLists with BROTLI_BUILD_CLI=OFF + BROTLI_BUILD_BROTLI_CMD=OFF
// does NOT produce a working browser-loadable .js/.wasm pair (no MODULARIZE,
// no exported wrappers), so we link our own wrapper manually below.
shell.exec(`emmake make -C "${WASM_BUILD_DIR}" -j${os.cpus().length} brotlidec brotlicommon`);

const libDec = path.join(WASM_BUILD_DIR, 'libbrotlidec.a');
const libCommon = path.join(WASM_BUILD_DIR, 'libbrotlicommon.a');
if (!fs.existsSync(libDec) || !fs.existsSync(libCommon)) {
  console.error(`ERROR: brotli static libs not found:\n  ${libDec}\n  ${libCommon}`);
  process.exit(1);
}

// 5. Link our wrapper into a MODULARIZE'd ES module that exposes
//    brotli_decompress_buffer / brotli_free_buffer / brotli_get_last_error_message.
//    Output: brotli_wasm.mjs + brotli_wasm.wasm (renamed to .js after link).
console.log('Linking brotli wrapper into MODULARIZE-d ES module...');

const wrapperSrc = path.join(USERLAND_DIR, 'brotli-wrapper.c');
if (!fs.existsSync(wrapperSrc)) {
  console.error(`ERROR: missing brotli wrapper source: ${wrapperSrc}`);
  process.exit(1);
}

const includeDir = path.join(SOURCE_DIR, 'c', 'include');
const wasmMjs = path.join(CDN_DIR, 'brotli_wasm.mjs');
const wasmJs = path.join(CDN_DIR, 'brotli_wasm.js');
const wasmWasm = path.join(CDN_DIR, 'brotli_wasm.wasm');

// Clean any stale artefacts from previous builds.
shell.rm('-f', wasmMjs, wasmJs, wasmWasm);

const linkFlags = [
  '-O3',
  '-sMODULARIZE=1',
  '-sEXPORT_ES6=1',
  '-sEXPORT_NAME=createBrotli',
  '-sFILESYSTEM=0',
  '-sINVOKE_RUN=0',
  '-sEXIT_RUNTIME=0',
  '-sALLOW_MEMORY_GROWTH=1',
  '-sENVIRONMENT=web,worker',
  // The Worker entry passes `wasmBinary` directly, so the factory should not
  // try to fetch the .wasm itself. Keep locateFile support anyway.
  '-sEXPORTED_RUNTIME_METHODS=cwrap,HEAPU8,HEAPU32',
  '-sEXPORTED_FUNCTIONS=_brotli_decompress_buffer,_brotli_free_buffer,_brotli_get_last_error_message,_malloc,_free',
].join(' ');

const linkCmd = `emcc "${wrapperSrc}" -I "${includeDir}" "${libDec}" "${libCommon}" ${linkFlags} -o "${wasmMjs}"`;
console.log(linkCmd);
shell.exec(linkCmd);

// emcc with `-o foo.mjs` writes both `foo.mjs` and `foo.wasm`. Rename .mjs -> .js
// to keep the existing CDN/manifest/consumer naming unchanged. Browsers happily
// `import()` an ES module regardless of the file extension; the MIME type from
// our static server is `text/javascript` either way.
if (!fs.existsSync(wasmMjs) || !fs.existsSync(wasmWasm)) {
  console.error(`ERROR: emcc did not produce expected outputs:\n  ${wasmMjs}\n  ${wasmWasm}`);
  process.exit(1);
}
shell.mv(wasmMjs, wasmJs);

// Verify output
console.log('\nBuild complete!');
console.log(`  CLI: ${path.join(BUILD_DIR, 'brotli')}`);
console.log(`  CDN WASM JS: ${wasmJs}`);
console.log(`  CDN WASM: ${wasmWasm}`);
