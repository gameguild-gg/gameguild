/**
 * Build Dear ImGui as a static library for Emscripten.
 *
 * Uses the exact release in toolchain.lock.json,
 * downloads the source, compiles core + SDL3 backend with emcc, and deploys:
 *   - sysroot/usr/lib/libimgui.a
 *   - sysroot/usr/include/imgui/*.h
 *
 * ImGui has no CMake build system, so we compile directly with emcc + emar.
 * Depends on SDL3 being already built and deployed to the sysroot.
 */

import { spawnSync } from 'node:child_process';
import fs from 'fs';
import path from 'path';
import { toolchainPaths } from './toolchain/paths.ts';
import shell from 'shelljs';
import { setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { loadToolchainStateSync, lockedVersion } from './toolchain/config.ts';
import { ensureLockedSource } from './toolchain/sources.ts';

enableBuildKeepalive('build-imgui');

const ROOT = process.cwd();
const P = toolchainPaths(ROOT);
shell.config.fatal = true;

const { lock } = loadToolchainStateSync(ROOT);
const EMSDK_VERSION = lockedVersion(lock, 'emsdk');
setupEmsdk(EMSDK_VERSION);

function runEmscriptenTool(tool: 'emcc' | 'emar', args: readonly string[]): void {
    let executable: string = tool;
    let invocationArgs = [...args];

    if (process.platform === 'win32') {
        const python = process.env.EMSDK_PYTHON;
        if (!python) throw new Error('EMSDK_PYTHON is missing after Emscripten activation.');
        executable = python;
        invocationArgs = [path.join(P.emsdk, 'upstream', 'emscripten', `${tool}.py`), ...args];
    }

    const result = spawnSync(executable, invocationArgs, { env: process.env, stdio: 'inherit' });
    if (result.error) throw result.error;
    if (result.status !== 0) throw new Error(`${tool} exited with status ${result.status}.`);
}

const SOURCE_ROOT = path.join(P.sources, 'imgui');
const BUILD_DIR = path.join(P.builds, 'imgui');
const SYSROOT_LIB = path.join(P.sysroot, 'usr', 'lib');
const SYSROOT_INC = path.join(P.sysroot, 'usr', 'include');

shell.mkdir('-p', SOURCE_ROOT);
shell.mkdir('-p', BUILD_DIR);
shell.mkdir('-p', SYSROOT_LIB);
shell.mkdir('-p', path.join(SYSROOT_INC, 'imgui'));

// --------------- version detection ---------------

function detectVersion(): string {
    const tag = lockedVersion(lock, 'imgui');
    console.log(`Using ImGui release: ${tag}`);
    return tag;
}

// --------------- source management ---------------

function ensureSource(tag: string): string {
    const destDir = path.join(SOURCE_ROOT, `imgui-${tag}`);
    return ensureLockedSource(ROOT, lock, 'imgui', destDir, 'imgui.h');
}

// --------------- build ---------------

const TAG = detectVersion();
const SOURCE_DIR = ensureSource(TAG);

// Core ImGui source files
const coreFiles = [
    'imgui.cpp',
    'imgui_demo.cpp',
    'imgui_draw.cpp',
    'imgui_tables.cpp',
    'imgui_widgets.cpp',
];

// SDL3 backend files
const backendFiles = [
    'backends/imgui_impl_sdl3.cpp',
    'backends/imgui_impl_sdlrenderer3.cpp',
    'backends/imgui_impl_opengl3.cpp',
];

// Compile flags
const CXXFLAGS = [
    '-Os',
    '-DNDEBUG',               // Disable IM_ASSERT → no __assert_fail import in libimgui.a
    // No -fwasm-exceptions: incompatible with -mno-reference-types (Asyncify).
    `-I${SOURCE_DIR}`,
    `-I${path.join(SOURCE_DIR, 'backends')}`,
    '-isystem',
    SYSROOT_INC,  // SDL3 plus copied libc headers are system headers
];

// Compile all source files to object files
const objectFiles: string[] = [];

console.log('Compiling ImGui core...');
for (const src of coreFiles) {
    const srcPath = path.join(SOURCE_DIR, src);
    const objName = path.basename(src, '.cpp') + '.o';
    const objPath = path.join(BUILD_DIR, objName);
    runEmscriptenTool('emcc', [...CXXFLAGS, '-c', srcPath, '-o', objPath]);
    objectFiles.push(objPath);
}

console.log('Compiling ImGui SDL3 backends...');
for (const src of backendFiles) {
    const srcPath = path.join(SOURCE_DIR, src);
    if (!fs.existsSync(srcPath)) {
        console.warn(`  Warning: ${src} not found, skipping`);
        continue;
    }
    const objName = path.basename(src, '.cpp') + '.o';
    const objPath = path.join(BUILD_DIR, objName);
    runEmscriptenTool('emcc', [...CXXFLAGS, '-c', srcPath, '-o', objPath]);
    objectFiles.push(objPath);
}

// Create static archive
console.log('Creating libimgui.a...');
const archivePath = path.join(BUILD_DIR, 'libimgui.a');
runEmscriptenTool('emar', ['rcs', archivePath, ...objectFiles]);

// --------------- deploy to sysroot ---------------

shell.cp('-f', archivePath, path.join(SYSROOT_LIB, 'libimgui.a'));
console.log('Deployed libimgui.a to sysroot/usr/lib/');

// Copy core headers
const coreHeaders = [
    'imgui.h',
    'imconfig.h',
    'imgui_internal.h',
    'imstb_rectpack.h',
    'imstb_textedit.h',
    'imstb_truetype.h',
];
for (const h of coreHeaders) {
    const hPath = path.join(SOURCE_DIR, h);
    if (fs.existsSync(hPath)) {
        shell.cp('-f', hPath, path.join(SYSROOT_INC, 'imgui', '/'));
    }
}

// Copy backend headers
const backendHeaders = [
    'backends/imgui_impl_sdl3.h',
    'backends/imgui_impl_sdlrenderer3.h',
    'backends/imgui_impl_opengl3.h',
    'backends/imgui_impl_opengl3_loader.h',
];
for (const h of backendHeaders) {
    const hPath = path.join(SOURCE_DIR, h);
    if (fs.existsSync(hPath)) {
        shell.cp('-f', hPath, path.join(SYSROOT_INC, 'imgui', '/'));
    }
}
console.log('Deployed ImGui headers to sysroot/usr/include/imgui/');

// Clean up object files
for (const obj of objectFiles) {
    shell.rm('-f', obj);
}

console.log('>>> ImGui build complete.');
