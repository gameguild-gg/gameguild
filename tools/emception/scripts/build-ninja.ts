/**
 * Build Ninja as a standalone Emscripten WASM module.
 *
 * Ninja has minimal dependencies — just a C++ compiler. We download the
 * source, cross-compile with emcmake/emmake, and produce ninja.wasm + ninja.mjs.
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { setupEmsdk } from './lib/emsdk.ts';

const ROOT = process.cwd();

shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || 'latest';
setupEmsdk(EMSDK_VERSION);

const CONCURRENCY = os.cpus().length;

const USERLAND_DIR = path.join(ROOT, 'userland', 'ninja');
const OUTPUT_DIR = path.join(ROOT, 'build');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const LIBCURL_INC = path.join(ROOT, 'userland', 'libcurl-lite', 'include');
const LIBCURL_A = path.join(OUTPUT_DIR, 'libcurl.a');

/** Common Emscripten flags for standalone tool modules */
const STANDALONE_FLAGS = [
    '-sALLOW_MEMORY_GROWTH=1',
    '-sSTACK_SIZE=2097152',    // 2 MB stack
    '-sFORCE_FILESYSTEM=1',
    '-sMODULARIZE=1',
    '-sEXPORT_ES6=1',
    '-sEXIT_RUNTIME=1',
    '-sINVOKE_RUN=0',
    '-sEXPORTED_FUNCTIONS=_main',
    '-sEXPORTED_RUNTIME_METHODS=FS,callMain',
    '-fwasm-exceptions',
].join(' ');

shell.mkdir('-p', USERLAND_DIR);
shell.mkdir('-p', OUTPUT_DIR);
shell.mkdir('-p', SYSROOT_LIB);

// Detect latest Ninja release from GitHub
function detectNinjaVersion(): string {
    const envVer = process.env.NINJA_VERSION;
    if (envVer) return envVer;

    console.log('Detecting latest Ninja release...');
    const result = shell.exec(
        'curl -fsSL https://api.github.com/repos/ninja-build/ninja/releases/latest',
        { silent: true },
    );
    if (result.code !== 0) {
        throw new Error('Failed to query GitHub for latest Ninja release');
    }
    const data = JSON.parse(result.stdout);
    const tag = (data.tag_name as string).replace(/^v/, '');
    console.log(`  Latest Ninja release: ${tag}`);
    return tag;
}

const NINJA_VERSION = detectNinjaVersion();
const SOURCE_DIR = path.join(USERLAND_DIR, `ninja-${NINJA_VERSION}`);
const BUILD_WASM_DIR = path.join(SOURCE_DIR, 'build-wasm');

// 1. Download source
if (!fs.existsSync(SOURCE_DIR)) {
    console.log(`Downloading Ninja ${NINJA_VERSION}...`);
    shell.cd(USERLAND_DIR);
    const tarball = `v${NINJA_VERSION}.tar.gz`;
    shell.exec(`curl -fSL -o "${tarball}" "https://github.com/ninja-build/ninja/archive/refs/tags/${tarball}"`);
    shell.exec(`tar xzf "${tarball}"`);
    shell.rm(tarball);
}

shell.cd(SOURCE_DIR);

// 2. Apply source patches (TS-based, not .patch files)
function patchSource(relPath: string, needle: string, replacement: string, label: string) {
    const filePath = path.join(SOURCE_DIR, relPath);
    const content = fs.readFileSync(filePath, 'utf8');
    if (content.includes(replacement)) {
        console.log(`  [${label}] already applied — skipping`);
        return;
    }
    if (!content.includes(needle)) {
        throw new Error(`[${label}] needle not found in ${relPath} — upstream may have changed`);
    }
    fs.writeFileSync(filePath, content.replace(needle, replacement));
    console.log(`  [${label}] applied`);
}

// Emscripten has no sched_getaffinity — return 1 (single-threaded WASM)
patchSource(
    'src/util.cc',
    'int GetProcessorCount() {\n#ifdef _WIN32',
    'int GetProcessorCount() {\n#ifdef __EMSCRIPTEN__\n  return 1;  // WASM is single-threaded\n#elif defined(_WIN32)',
    'GetProcessorCount-emscripten',
);

// 3. Build with emcmake
console.log('Cross-compiling Ninja for WASM...');
if (fs.existsSync(BUILD_WASM_DIR)) shell.rm('-rf', BUILD_WASM_DIR);
shell.mkdir('-p', BUILD_WASM_DIR);

const cmakeCmd = [
    'emcmake cmake',
    `-S "${SOURCE_DIR}"`,
    `-B "${BUILD_WASM_DIR}"`,
    '-DCMAKE_BUILD_TYPE=MinSizeRel',
    '-DBUILD_TESTING=OFF',
].join(' ');
console.log(cmakeCmd);
shell.exec(cmakeCmd);

shell.exec(`emmake make -C "${BUILD_WASM_DIR}" -j${CONCURRENCY} ninja`);

// 4. Re-link as standalone module with libcurl-lite
console.log('Linking Ninja as standalone WASM module...');

// Find all .o files from the ninja build
const ninjaObjs = shell.find(BUILD_WASM_DIR)
    .filter(f => f.endsWith('.o') && !f.includes('CMakeFiles/CMakeTmp'));

const toolWasm = path.join(OUTPUT_DIR, 'ninja.wasm');
const toolMjs = path.join(OUTPUT_DIR, 'ninja.mjs');

const linkCmd = [
    'em++',
    ...ninjaObjs.map(o => `"${o}"`),
    fs.existsSync(LIBCURL_A) ? `"${LIBCURL_A}"` : '',
    fs.existsSync(LIBCURL_A) ? `-I "${LIBCURL_INC}"` : '',
    STANDALONE_FLAGS,
    '-Os',
    `-o "${toolMjs}"`,
].filter(Boolean).join(' \\\n    ');

console.log(linkCmd);
shell.exec(linkCmd);

if (!fs.existsSync(toolWasm)) {
    console.error('ERROR: ninja.wasm not generated');
    process.exit(1);
}
console.log(`Created ${toolWasm} + ${toolMjs}`);

// 5. Deploy to sysroot
console.log('Deploying to sysroot...');
for (const ext of ['.wasm', '.mjs']) {
    const src = path.join(OUTPUT_DIR, `ninja${ext}`);
    if (fs.existsSync(src)) shell.cp('-f', src, SYSROOT_LIB);
}

console.log('>>> Ninja build complete.');
