/**
 * Build SDL3_mixer as a static library for Emscripten.
 *
 * Detects the latest release from GitHub (or uses SDL3_MIXER_VERSION env var),
 * downloads the source, cross-compiles with emcmake/emmake, and deploys:
 *   - sysroot/usr/lib/libSDL3_mixer.a
 *   - sysroot/usr/include/SDL3_mixer/*.h
 *
 * Depends on SDL3 being already built and deployed to the sysroot.
 */

import fs from 'fs';
import os from 'os';
import path from 'path';
import shell from 'shelljs';
import { ensureSdl3CmakeVersionShim, setupEmsdk } from './lib/emsdk.ts';

const ROOT = process.cwd();
shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || 'latest';
setupEmsdk(EMSDK_VERSION);
ensureSdl3CmakeVersionShim();

const CONCURRENCY = os.cpus().length;
const USERLAND_DIR = path.join(ROOT, 'userland', 'sdl3-mixer');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const SYSROOT_INC = path.join(ROOT, 'sysroot', 'usr', 'include');

shell.mkdir('-p', USERLAND_DIR);
shell.mkdir('-p', SYSROOT_LIB);
shell.mkdir('-p', path.join(SYSROOT_INC, 'SDL3_mixer'));

// --------------- version detection ---------------

function detectVersion(): string {
    const envVer = process.env.SDL3_MIXER_VERSION;
    if (envVer) return envVer;

    console.log('Detecting latest SDL3_mixer release...');
    const result = shell.exec(
        'curl -fsSL https://api.github.com/repos/libsdl-org/SDL_mixer/releases/latest',
        { silent: true },
    );
    if (result.code !== 0) {
        throw new Error('Failed to query GitHub for latest SDL3_mixer release');
    }
    const tag: string = JSON.parse(result.stdout).tag_name;
    console.log(`  Latest SDL3_mixer release: ${tag}`);
    return tag;
}

// --------------- source management ---------------

function ensureSource(tag: string): string {
    const destDir = path.join(USERLAND_DIR, `SDL_mixer-${tag}`);
    if (fs.existsSync(path.join(destDir, 'CMakeLists.txt'))) {
        console.log(`Using existing SDL3_mixer source: ${path.basename(destDir)}`);
        return destDir;
    }

    const tarball = `${tag}.tar.gz`;
    console.log(`Downloading SDL3_mixer ${tag}...`);
    shell.cd(USERLAND_DIR);
    shell.exec(`curl -fSL -o "${tarball}" "https://github.com/libsdl-org/SDL_mixer/archive/refs/tags/${tarball}"`);

    shell.rm('-rf', destDir);
    shell.mkdir('-p', destDir);
    shell.exec(`tar xzf "${tarball}" --strip-components=1 -C "${destDir}"`);
    shell.rm('-f', tarball);

    if (!fs.existsSync(path.join(destDir, 'CMakeLists.txt'))) {
        throw new Error(`Extracted SDL3_mixer source is invalid: ${destDir}`);
    }
    console.log(`Extracted SDL3_mixer source to: ${path.basename(destDir)}`);
    return destDir;
}

// --------------- build ---------------

const TAG = detectVersion();
const SOURCE_DIR = ensureSource(TAG);
const BUILD_DIR = path.join(SOURCE_DIR, 'build-wasm');

shell.mkdir('-p', BUILD_DIR);

console.log('Configuring SDL3_mixer with emcmake cmake...');
shell.exec([
    'emcmake cmake',
    `-S "${SOURCE_DIR}"`,
    `-B "${BUILD_DIR}"`,
    '-DCMAKE_BUILD_TYPE=Release',
    '-DBUILD_SHARED_LIBS=OFF',
    '-DSDL3MIXER_SAMPLES=OFF',
    '-DSDL3MIXER_CMD=OFF',
    '-DSDL3MIXER_INSTALL=OFF',
    `-DCMAKE_PREFIX_PATH="${path.join(ROOT, 'sysroot', 'usr')}"`,
].join(' '));

console.log(`Building SDL3_mixer with ${CONCURRENCY} cores...`);
shell.exec(`emmake make -C "${BUILD_DIR}" -j${CONCURRENCY}`);

// --------------- deploy to sysroot ---------------

const found = shell.find(BUILD_DIR).filter((f: string) => /libSDL3_mixer\.a$/.test(f));
if (found.length === 0) {
    throw new Error('libSDL3_mixer.a not found after build');
}
shell.cp('-f', found[0], path.join(SYSROOT_LIB, 'libSDL3_mixer.a'));
console.log('Deployed libSDL3_mixer.a to sysroot/usr/lib/');

// Copy public headers
const includeDir = path.join(SOURCE_DIR, 'include', 'SDL3_mixer');
if (fs.existsSync(includeDir)) {
    shell.cp('-f', path.join(includeDir, '*.h'), path.join(SYSROOT_INC, 'SDL3_mixer', '/'));
}
const altHeaders = shell.find(path.join(SOURCE_DIR, 'include')).filter(
    (f: string) => f.endsWith('.h') && f.includes('SDL3_mixer'),
);
for (const h of altHeaders) {
    shell.cp('-f', h, path.join(SYSROOT_INC, 'SDL3_mixer', '/'));
}
console.log('Deployed SDL3_mixer headers to sysroot/usr/include/SDL3_mixer/');

console.log('>>> SDL3_mixer build complete.');
