/**
 * Build libcurl-lite as a static archive for Emscripten WASM modules.
 *
 * Produces:
 *   - build/libcurl.a          (static archive)
 *   - sysroot/usr/lib/libcurl.a
 *   - sysroot/usr/include/curl/curl.h
 *
 * Consumers (cmake, ninja, curl CLI) link against -lcurl at build time.
 */

import fs from 'fs';
import path from 'path';
import { toolchainPaths } from './toolchain/paths.ts';
import shell from 'shelljs';
import { setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { PINNED } from './lib/pinned-versions.ts';

enableBuildKeepalive('build-libcurl-lite');

const ROOT = process.cwd();
const P = toolchainPaths(ROOT);

shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION;
const CURL_LITE_VERSION = process.env.CURL_LITE_VERSION || PINNED.CURL_LITE_VERSION;
setupEmsdk(EMSDK_VERSION);

const LIBCURL_DIR = path.join(P.overlays, 'libcurl-lite');
const SRC_FILE = path.join(LIBCURL_DIR, 'src', 'curl_lite.c');
const HEADER_FILE = path.join(LIBCURL_DIR, 'include', 'curl', 'curl.h');
const OUTPUT_DIR = P.tools;
const SYSROOT_LIB = path.join(P.sysroot, 'usr', 'lib');
const SYSROOT_INC = path.join(P.sysroot, 'usr', 'include');

shell.mkdir('-p', OUTPUT_DIR);
shell.mkdir('-p', SYSROOT_LIB);
shell.mkdir('-p', path.join(SYSROOT_INC, 'curl'));

// Patch version constants in curl.h to match the pinned curl version.
{
    const [major, minor, patch] = CURL_LITE_VERSION.split('.').map(Number);
    const versionNum = `0x${major.toString(16).padStart(2, '0')}${minor.toString(16).padStart(2, '0')}${patch.toString(16).padStart(2, '0')}`;
    let header = fs.readFileSync(HEADER_FILE, 'utf8');
    header = header
        .replace(/#define LIBCURL_VERSION "[^"]*"/, `#define LIBCURL_VERSION "${CURL_LITE_VERSION}-lite"`)
        .replace(/#define LIBCURL_VERSION_NUM 0x[0-9a-fA-F]+/, `#define LIBCURL_VERSION_NUM ${versionNum}`)
        .replace(/#define LIBCURL_VERSION_MAJOR \d+/, `#define LIBCURL_VERSION_MAJOR ${major}`)
        .replace(/#define LIBCURL_VERSION_MINOR \d+/, `#define LIBCURL_VERSION_MINOR ${minor}`)
        .replace(/#define LIBCURL_VERSION_PATCH \d+/, `#define LIBCURL_VERSION_PATCH ${patch}`);
    fs.writeFileSync(HEADER_FILE, header);
    console.log(`Patched curl.h to version ${CURL_LITE_VERSION}-lite`);
}

// Compile to object file
console.log('Compiling libcurl-lite...');
const objFile = path.join(OUTPUT_DIR, 'curl_lite.o');
shell.exec([
    'emcc',
    `-I "${path.join(LIBCURL_DIR, 'include')}"`,
    '-Os',
    // No -fwasm-exceptions: incompatible with -mno-reference-types (Asyncify).
    '-c',
    `"${SRC_FILE}"`,
    `-o "${objFile}"`,
].join(' '));

// Create static archive
console.log('Creating libcurl.a...');
const archiveFile = path.join(OUTPUT_DIR, 'libcurl.a');
shell.exec(`emar rcs "${archiveFile}" "${objFile}"`);

// Deploy to sysroot
console.log('Deploying to sysroot...');
shell.cp('-f', archiveFile, SYSROOT_LIB);
shell.cp('-f', HEADER_FILE, path.join(SYSROOT_INC, 'curl', 'curl.h'));

// Clean up object file
shell.rm('-f', objFile);

console.log('>>> libcurl-lite build complete.');
