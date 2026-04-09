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

import path from 'path';
import shell from 'shelljs';
import { setupEmsdk } from './lib/emsdk.ts';

const ROOT = process.cwd();

shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || 'latest';
setupEmsdk(EMSDK_VERSION);

const LIBCURL_DIR = path.join(ROOT, 'userland', 'libcurl-lite');
const SRC_FILE = path.join(LIBCURL_DIR, 'src', 'curl_lite.c');
const HEADER_FILE = path.join(LIBCURL_DIR, 'include', 'curl', 'curl.h');
const OUTPUT_DIR = path.join(ROOT, 'build');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const SYSROOT_INC = path.join(ROOT, 'sysroot', 'usr', 'include');

shell.mkdir('-p', OUTPUT_DIR);
shell.mkdir('-p', SYSROOT_LIB);
shell.mkdir('-p', path.join(SYSROOT_INC, 'curl'));

// Compile to object file
console.log('Compiling libcurl-lite...');
const objFile = path.join(OUTPUT_DIR, 'curl_lite.o');
shell.exec([
    'emcc',
    `-I "${path.join(LIBCURL_DIR, 'include')}"`,
    '-Os',
    '-fwasm-exceptions',
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
