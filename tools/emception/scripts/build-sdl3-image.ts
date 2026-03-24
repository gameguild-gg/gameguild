/**
 * Build SDL3_image as a static library for Emscripten.
 *
 * Detects the latest release from GitHub (or uses SDL3_IMAGE_VERSION env var),
 * downloads the source, cross-compiles with emcmake/emmake, and deploys:
 *   - sysroot/usr/lib/libSDL3_image.a
 *   - sysroot/usr/include/SDL3_image/*.h
 *
 * Depends on SDL3 being already built and deployed to the sysroot.
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
const USERLAND_DIR = path.join(ROOT, 'userland', 'sdl3-image');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const SYSROOT_INC = path.join(ROOT, 'sysroot', 'usr', 'include');

shell.mkdir('-p', USERLAND_DIR);
shell.mkdir('-p', SYSROOT_LIB);
shell.mkdir('-p', path.join(SYSROOT_INC, 'SDL3_image'));

// --------------- version detection ---------------

function detectVersion(): string {
    const envVer = process.env.SDL3_IMAGE_VERSION;
    if (envVer) return envVer;

    console.log('Detecting latest SDL3_image release...');
    const result = shell.exec(
        'curl -fsSL https://api.github.com/repos/libsdl-org/SDL_image/releases/latest',
        { silent: true },
    );
    if (result.code !== 0) {
        throw new Error('Failed to query GitHub for latest SDL3_image release');
    }
    const tag: string = JSON.parse(result.stdout).tag_name;
    console.log(`  Latest SDL3_image release: ${tag}`);
    return tag;
}

// --------------- source management ---------------

function ensureSource(tag: string): string {
    const destDir = path.join(USERLAND_DIR, `SDL_image-${tag}`);
    if (fs.existsSync(path.join(destDir, 'CMakeLists.txt'))) {
        console.log(`Using existing SDL3_image source: ${path.basename(destDir)}`);
        return destDir;
    }

    const tarball = `${tag}.tar.gz`;
    console.log(`Downloading SDL3_image ${tag}...`);
    shell.cd(USERLAND_DIR);
    shell.exec(`curl -fSL -o "${tarball}" "https://github.com/libsdl-org/SDL_image/archive/refs/tags/${tarball}"`);

    shell.rm('-rf', destDir);
    shell.mkdir('-p', destDir);
    shell.exec(`tar xzf "${tarball}" --strip-components=1 -C "${destDir}"`);
    shell.rm('-f', tarball);

    if (!fs.existsSync(path.join(destDir, 'CMakeLists.txt'))) {
        throw new Error(`Extracted SDL3_image source is invalid: ${destDir}`);
    }
    console.log(`Extracted SDL3_image source to: ${path.basename(destDir)}`);
    return destDir;
}

// --------------- build ---------------

const TAG = detectVersion();
const SOURCE_DIR = ensureSource(TAG);
const BUILD_DIR = path.join(SOURCE_DIR, 'build-wasm');

shell.mkdir('-p', BUILD_DIR);

console.log('Configuring SDL3_image with emcmake cmake...');
shell.exec([
    'emcmake cmake',
    `-S "${SOURCE_DIR}"`,
    `-B "${BUILD_DIR}"`,
    '-DCMAKE_BUILD_TYPE=Release',
    '-DBUILD_SHARED_LIBS=OFF',
    '-DSDL3IMAGE_SAMPLES=OFF',
    '-DSDL3IMAGE_TESTS=OFF',
    '-DSDL3IMAGE_JPG=ON',
    '-DSDL3IMAGE_PNG=ON',
    '-DSDL3IMAGE_WEBP=OFF',
    '-DSDL3IMAGE_INSTALL=OFF',
    `-DCMAKE_PREFIX_PATH="${path.join(ROOT, 'sysroot', 'usr')}"`,
].join(' '));

console.log(`Building SDL3_image with ${CONCURRENCY} cores...`);
shell.exec(`emmake make -C "${BUILD_DIR}" -j${CONCURRENCY}`);

// --------------- deploy to sysroot ---------------

const found = shell.find(BUILD_DIR).filter((f: string) => /libSDL3_image\.a$/.test(f));
if (found.length === 0) {
    throw new Error('libSDL3_image.a not found after build');
}
shell.cp('-f', found[0], path.join(SYSROOT_LIB, 'libSDL3_image.a'));
console.log('Deployed libSDL3_image.a to sysroot/usr/lib/');

// Copy public headers
const includeDir = path.join(SOURCE_DIR, 'include', 'SDL3_image');
if (fs.existsSync(includeDir)) {
    shell.cp('-f', path.join(includeDir, '*.h'), path.join(SYSROOT_INC, 'SDL3_image', '/'));
}
// Also check top-level include
const altInclude = path.join(SOURCE_DIR, 'include');
const altHeaders = shell.find(altInclude).filter((f: string) => f.endsWith('.h') && f.includes('SDL3_image'));
for (const h of altHeaders) {
    shell.cp('-f', h, path.join(SYSROOT_INC, 'SDL3_image', '/'));
}
console.log('Deployed SDL3_image headers to sysroot/usr/include/SDL3_image/');

console.log('>>> SDL3_image build complete.');
