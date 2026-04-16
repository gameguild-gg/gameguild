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

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = process.cwd();

// Ensure shell commands fail on error
shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || 'latest';

// Brotli version to use
const BROTLI_VERSION = process.env.BROTLI_VERSION || '1.1.0';

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
shell.rm('-rf', WASM_BUILD_DIR);
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

// Build the library (Emscripten will produce brotli.js and brotli.wasm)
shell.exec(`emmake make -C "${WASM_BUILD_DIR}" -j${os.cpus().length}`);

// Brotli v1.1.0 produces brotli.js and brotli.wasm directly
const brotliWasmJs = path.join(WASM_BUILD_DIR, 'brotli.js');
const brotliWasmWasm = path.join(WASM_BUILD_DIR, 'brotli.wasm');
if (!fs.existsSync(brotliWasmJs) || !fs.existsSync(brotliWasmWasm)) {
  console.error('ERROR: brotli.js + brotli.wasm not found in build directory');
  process.exit(1);
}

// Rename to standard names
const wasmJs = path.join(CDN_DIR, 'brotli_wasm.js');
const wasmWasm = path.join(CDN_DIR, 'brotli_wasm_bg.wasm');
shell.mv(brotliWasmJs, wasmJs);
shell.mv(brotliWasmWasm, wasmWasm);

// Verify output
console.log('\nBuild complete!');
console.log(`  CLI: ${path.join(BUILD_DIR, 'brotli')}`);
console.log(`  CDN WASM JS: ${wasmJs}`);
console.log(`  CDN WASM: ${wasmWasm}`);
