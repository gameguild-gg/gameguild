/**
 * Build SDL3 as a static library for Emscripten.
 *
 * Detects the latest SDL3 release from GitHub (or uses SDL3_VERSION env var),
 * downloads the source, cross-compiles with emcmake/emmake, and deploys:
 *   - sysroot/usr/lib/libSDL3.a
 *   - sysroot/usr/include/SDL3/*.h
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
const USERLAND_DIR = path.join(ROOT, 'userland', 'sdl3');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const SYSROOT_INC = path.join(ROOT, 'sysroot', 'usr', 'include');

shell.mkdir('-p', USERLAND_DIR);
shell.mkdir('-p', SYSROOT_LIB);
shell.mkdir('-p', path.join(SYSROOT_INC, 'SDL3'));

// --------------- version detection ---------------

function detectVersion(): string {
    const envVer = process.env.SDL3_VERSION;
    if (envVer) return envVer;

    console.log('Detecting latest SDL3 release...');
    const result = shell.exec(
        'curl -fsSL https://api.github.com/repos/libsdl-org/SDL/releases/latest',
        { silent: true },
    );
    if (result.code !== 0) {
        throw new Error('Failed to query GitHub for latest SDL3 release');
    }
    const tag: string = JSON.parse(result.stdout).tag_name;
    console.log(`  Latest SDL3 release: ${tag}`);
    return tag;
}

// --------------- source management ---------------

function findExistingSourceDir(tag: string): string | null {
    const candidates = [
        path.join(USERLAND_DIR, `SDL-${tag}`),
        path.join(USERLAND_DIR, `sdl3-${tag}`),
    ];
    for (const c of candidates) {
        if (fs.existsSync(path.join(c, 'CMakeLists.txt'))) return c;
    }
    return null;
}

function ensureSource(tag: string): string {
    const existing = findExistingSourceDir(tag);
    if (existing) {
        console.log(`Using existing SDL3 source: ${path.basename(existing)}`);
        return existing;
    }

    const destDir = path.join(USERLAND_DIR, `SDL-${tag}`);
    const tarball = `${tag}.tar.gz`;

    console.log(`Downloading SDL3 ${tag}...`);
    shell.cd(USERLAND_DIR);
    shell.exec(`curl -fSL -o "${tarball}" "https://github.com/libsdl-org/SDL/archive/refs/tags/${tarball}"`);

    shell.rm('-rf', destDir);
    shell.mkdir('-p', destDir);
    shell.exec(`tar xzf "${tarball}" --strip-components=1 -C "${destDir}"`);
    shell.rm('-f', tarball);

    if (!fs.existsSync(path.join(destDir, 'CMakeLists.txt'))) {
        throw new Error(`Extracted SDL3 source is invalid: ${destDir}`);
    }
    console.log(`Extracted SDL3 source to: ${path.basename(destDir)}`);
    return destDir;
}

// --------------- build ---------------

const TAG = detectVersion();
const SOURCE_DIR = ensureSource(TAG);
const BUILD_DIR = path.join(SOURCE_DIR, 'build-wasm');

shell.mkdir('-p', BUILD_DIR);

console.log('Configuring SDL3 with emcmake cmake...');
shell.exec([
    'emcmake cmake',
    `-S "${SOURCE_DIR}"`,
    `-B "${BUILD_DIR}"`,
    '-DCMAKE_BUILD_TYPE=Release',
    '-DSDL_SHARED=OFF',
    '-DSDL_STATIC=ON',
    '-DSDL_TEST=OFF',
    '-DSDL_TESTS=OFF',
    '-DSDL_EXAMPLES=OFF',
    '-DSDL_INSTALL=OFF',
    '-DSDL_DISABLE_INSTALL=ON',
    // Disable subsystems that use emscripten pthread-only APIs
    // (emscripten_asm_const_int_sync_on_main_thread), which cause
    // undefined symbol errors when linking without -s USE_PTHREADS=1.
    '-DSDL_CAMERA=OFF',
    '-DSDL_SENSOR=OFF',
].join(' '));

console.log(`Building SDL3 with ${CONCURRENCY} cores...`);
shell.exec(`emmake make -C "${BUILD_DIR}" -j${CONCURRENCY} SDL3-static`);

// --------------- deploy to sysroot ---------------

// Find the static library (may be libSDL3.a or SDL3.a depending on CMake config)
const libCandidates = [
    path.join(BUILD_DIR, 'libSDL3.a'),
    path.join(BUILD_DIR, 'SDL3.a'),
];
const libPath = libCandidates.find((p) => fs.existsSync(p));
if (!libPath) {
    // Search recursively
    const found = shell.find(BUILD_DIR).filter((f: string) => f.endsWith('libSDL3.a') || f.endsWith('SDL3.a'));
    if (found.length === 0) {
        throw new Error('libSDL3.a not found after build');
    }
    shell.cp('-f', found[0], path.join(SYSROOT_LIB, 'libSDL3.a'));
} else {
    shell.cp('-f', libPath, path.join(SYSROOT_LIB, 'libSDL3.a'));
}
console.log('Deployed libSDL3.a to sysroot/usr/lib/');

// Copy public headers
const includeDir = path.join(SOURCE_DIR, 'include', 'SDL3');
if (fs.existsSync(includeDir)) {
    shell.cp('-f', path.join(includeDir, '*.h'), path.join(SYSROOT_INC, 'SDL3', '/'));
}

// Copy generated headers (SDL_revision.h, SDL_config.h, etc.)
const generatedIncDir = path.join(BUILD_DIR, 'include', 'SDL3');
if (fs.existsSync(generatedIncDir)) {
    shell.cp('-f', path.join(generatedIncDir, '*.h'), path.join(SYSROOT_INC, 'SDL3', '/'));
}
const generatedIncDir2 = path.join(BUILD_DIR, 'include-config-release', 'SDL3');
if (fs.existsSync(generatedIncDir2)) {
    shell.cp('-f', path.join(generatedIncDir2, '*.h'), path.join(SYSROOT_INC, 'SDL3', '/'));
}
console.log('Deployed SDL3 headers to sysroot/usr/include/SDL3/');

console.log('>>> SDL3 build complete.');
