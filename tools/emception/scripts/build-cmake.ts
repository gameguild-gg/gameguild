/**
 * Build CMake as a standalone Emscripten WASM module.
 *
 * CMake is linked against libcurl-lite so file(DOWNLOAD), FetchContent,
 * and ExternalProject_Add route HTTP through the browser's fetch() API.
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

const USERLAND_DIR = path.join(ROOT, 'userland', 'cmake');
const OUTPUT_DIR = path.join(ROOT, 'build');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const LIBCURL_INC = path.join(ROOT, 'userland', 'libcurl-lite', 'include');
const LIBCURL_A = path.join(OUTPUT_DIR, 'libcurl.a');

/** Common Emscripten flags for standalone tool modules */
const STANDALONE_FLAGS = [
    '-sALLOW_MEMORY_GROWTH=1',
    '-sSTACK_SIZE=4194304',    // 4 MB stack — CMake can do deep recursion
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

// Detect latest CMake release
function detectCMakeVersion(): string {
    const envVer = process.env.CMAKE_VERSION;
    if (envVer) return envVer;

    console.log('Detecting latest CMake release...');
    const result = shell.exec(
        'curl -fsSL https://api.github.com/repos/Kitware/CMake/releases/latest',
        { silent: true },
    );
    if (result.code !== 0) {
        throw new Error('Failed to query GitHub for latest CMake release');
    }
    const data = JSON.parse(result.stdout);
    const tag = (data.tag_name as string).replace(/^v/, '');
    console.log(`  Latest CMake release: ${tag}`);
    return tag;
}

const CMAKE_VERSION = detectCMakeVersion();
const SOURCE_DIR = path.join(USERLAND_DIR, `cmake-${CMAKE_VERSION}`);
const BUILD_WASM_DIR = path.join(SOURCE_DIR, 'build-wasm');

// 1. Download source
if (!fs.existsSync(SOURCE_DIR)) {
    console.log(`Downloading CMake ${CMAKE_VERSION}...`);
    shell.cd(USERLAND_DIR);
    const tarball = `v${CMAKE_VERSION}.tar.gz`;
    shell.exec(`curl -fSL -o "${tarball}" "https://github.com/Kitware/CMake/archive/refs/tags/${tarball}"`);
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

// Emscripten libuv: add platform-specific stubs (posix-poll, posix-hrtime, no-fsevents, etc.)
// Without this, libuv fails to link because Emscripten doesn't match any platform block.
patchSource(
    'Utilities/cmlibuv/CMakeLists.txt',
    'if(CMAKE_SYSTEM_NAME STREQUAL "AIX")',
    `if(CMAKE_SYSTEM_NAME STREQUAL "Emscripten")
  list(APPEND uv_headers
    include/uv/posix.h
    )
  list(APPEND uv_defines
    _GNU_SOURCE
    )
  list(APPEND uv_sources
    src/unix/no-fsevents.c
    src/unix/no-proctitle.c
    src/unix/posix-hrtime.c
    src/unix/posix-poll.c
    )
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "AIX")`,
    'cmlibuv-emscripten-platform',
);

// Emscripten libuv: include posix.h for uv__loop_s fields (poll_fds etc.)
// Without this, the uv_loop_s struct misses platform-specific fields and posix-poll.c fails.
patchSource(
    'Utilities/cmlibuv/include/uv/unix.h',
    `#elif defined(__CYGWIN__) || \\
      defined(__MSYS__)   || \\
      defined(__HAIKU__)  || \\
      defined(__QNX__)    || \\
      defined(__GNU__)
# include "posix.h"`,
    `#elif defined(__EMSCRIPTEN__)
# include "posix.h"
#elif defined(__CYGWIN__) || \\
      defined(__MSYS__)   || \\
      defined(__HAIKU__)  || \\
      defined(__QNX__)    || \\
      defined(__GNU__)
# include "posix.h"`,
    'cmlibuv-unix-h-emscripten',
);

// 3. Build with emcmake
console.log('Cross-compiling CMake for WASM...');
if (fs.existsSync(BUILD_WASM_DIR)) shell.rm('-rf', BUILD_WASM_DIR);
shell.mkdir('-p', BUILD_WASM_DIR);

// CMake needs to find our libcurl-lite
const curlFlags = fs.existsSync(LIBCURL_A)
    ? [
        `-DCURL_INCLUDE_DIR="${LIBCURL_INC}"`,
        `-DCURL_LIBRARY="${LIBCURL_A}"`,
        '-DCMAKE_USE_SYSTEM_CURL=ON',
    ]
    : ['-DCMAKE_USE_SYSTEM_CURL=OFF'];

const cmakeCmd = [
    'emcmake cmake',
    `-S "${SOURCE_DIR}"`,
    `-B "${BUILD_WASM_DIR}"`,
    '-DCMAKE_BUILD_TYPE=MinSizeRel',
    '-DBUILD_TESTING=OFF',
    '-DCMAKE_USE_OPENSSL=OFF',
    ...curlFlags,
].join(' ');
console.log(cmakeCmd);
shell.exec(cmakeCmd);

// Build only the cmake executable (not ctest, cpack, etc.)
shell.exec(`emmake make -C "${BUILD_WASM_DIR}" -j${CONCURRENCY} cmake`);

// 4. Re-link as standalone module
console.log('Linking CMake as standalone WASM module...');

const toolWasm = path.join(OUTPUT_DIR, 'cmake.wasm');
const toolMjs = path.join(OUTPUT_DIR, 'cmake.mjs');

// Find the CMake libraries produced by the build.
const cmakeLibs = shell.find(BUILD_WASM_DIR)
    .filter(f => f.endsWith('.a') && !f.includes('CMakeTmp'));

// Find the main object files — these contain main() and are NOT in any .a
const cmakeMainObjs = shell.find(path.join(BUILD_WASM_DIR, 'Source', 'CMakeFiles', 'cmake.dir'))
    .filter(f => f.endsWith('.o'));

// Emscripten's sysroot zlib — CMake uses it instead of bundled cmzlib
const EMSDK_ZLIB = path.join(
    ROOT, 'tools', 'emsdk', 'upstream', 'emscripten', 'cache', 'sysroot',
    'lib', 'wasm32-emscripten', 'libz.a',
);

const linkCmd = [
    'em++',
    // Main object files (contain main())
    ...cmakeMainObjs.map(o => `"${o}"`),
    // Static libraries
    ...cmakeLibs.map(o => `"${o}"`),
    fs.existsSync(LIBCURL_A) ? `"${LIBCURL_A}"` : '',
    fs.existsSync(EMSDK_ZLIB) ? `"${EMSDK_ZLIB}"` : '-lz',
    STANDALONE_FLAGS,
    '-Os',
    `-o "${toolMjs}"`,
].filter(Boolean).join(' \\\n    ');

console.log(linkCmd);
shell.exec(linkCmd);

if (!fs.existsSync(toolWasm)) {
    console.error('ERROR: cmake.wasm not generated');
    process.exit(1);
}
console.log(`Created ${toolWasm} + ${toolMjs}`);

// 5. Deploy to sysroot
console.log('Deploying to sysroot...');
for (const ext of ['.wasm', '.mjs']) {
    const src = path.join(OUTPUT_DIR, `cmake${ext}`);
    if (fs.existsSync(src)) shell.cp('-f', src, SYSROOT_LIB);
}

console.log('>>> CMake build complete.');
