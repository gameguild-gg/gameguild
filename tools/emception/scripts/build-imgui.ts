/**
 * Build Dear ImGui as a static library for Emscripten.
 *
 * Detects the latest release from GitHub (or uses IMGUI_VERSION env var),
 * downloads the source, compiles core + SDL3 backend with emcc, and deploys:
 *   - sysroot/usr/lib/libimgui.a
 *   - sysroot/usr/include/imgui/*.h
 *
 * ImGui has no CMake build system, so we compile directly with emcc + emar.
 * Depends on SDL3 being already built and deployed to the sysroot.
 */

import fs from 'fs';
import path from 'path';
import shell from 'shelljs';
import { setupEmsdk } from './lib/emsdk.ts';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { PINNED } from './lib/pinned-versions.ts';

enableBuildKeepalive('build-imgui');

const ROOT = process.cwd();
shell.config.fatal = true;

const EMSDK_VERSION = process.env.EMSDK_VERSION || PINNED.EMSDK_VERSION;
setupEmsdk(EMSDK_VERSION);

const USERLAND_DIR = path.join(ROOT, 'userland', 'imgui');
const BUILD_DIR = path.join(ROOT, 'build', 'imgui');
const SYSROOT_LIB = path.join(ROOT, 'sysroot', 'usr', 'lib');
const SYSROOT_INC = path.join(ROOT, 'sysroot', 'usr', 'include');

shell.mkdir('-p', USERLAND_DIR);
shell.mkdir('-p', BUILD_DIR);
shell.mkdir('-p', SYSROOT_LIB);
shell.mkdir('-p', path.join(SYSROOT_INC, 'imgui'));

// --------------- version detection ---------------

function detectVersion(): string {
    const envVer = process.env.IMGUI_VERSION;
    if (envVer) return envVer;

    console.log('Detecting latest ImGui release...');
    const url = 'https://api.github.com/repos/ocornut/imgui/releases/latest';

    const execCurl = (extraArgs = '') =>
        shell.exec(`curl -fsSL ${extraArgs} ${url}`, {
            silent: true,
            fatal: false,
        });

    let result = process.env.GITHUB_TOKEN
        ? execCurl(`-H "Authorization: Bearer ${process.env.GITHUB_TOKEN}"`)
        : execCurl();

    if (result.code !== 0 && process.env.GITHUB_TOKEN) {
        console.warn('  Authenticated GitHub API call failed; retrying without token...');
        result = execCurl();
    }

    if (result.code !== 0) {
        console.warn(`  GitHub API unavailable (exit ${result.code}), using pinned ${PINNED.IMGUI_VERSION}`);
        return PINNED.IMGUI_VERSION;
    }
    const tag: string = JSON.parse(result.stdout).tag_name;
    console.log(`  Latest ImGui release: ${tag}`);
    return tag;
}

// --------------- source management ---------------

function ensureSource(tag: string): string {
    const destDir = path.join(USERLAND_DIR, `imgui-${tag}`);
    if (fs.existsSync(path.join(destDir, 'imgui.h'))) {
        console.log(`Using existing ImGui source: ${path.basename(destDir)}`);
        return destDir;
    }

    const tarball = `${tag}.tar.gz`;
    console.log(`Downloading ImGui ${tag}...`);
    shell.cd(USERLAND_DIR);
    shell.exec(`curl -fSL -o "${tarball}" "https://github.com/ocornut/imgui/archive/refs/tags/${tarball}"`);

    shell.rm('-rf', destDir);
    shell.mkdir('-p', destDir);
    shell.exec(`tar xzf "${tarball}" --strip-components=1 -C "${path.basename(destDir)}"`);
    shell.rm('-f', tarball);

    if (!fs.existsSync(path.join(destDir, 'imgui.h'))) {
        throw new Error(`Extracted ImGui source is invalid: ${destDir}`);
    }
    console.log(`Extracted ImGui source to: ${path.basename(destDir)}`);
    return destDir;
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
    `-I"${SOURCE_DIR}"`,
    `-I"${SOURCE_DIR}/backends"`,
    `-I"${path.join(SYSROOT_INC)}"`,  // For SDL3/SDL.h
].join(' ');

// Compile all source files to object files
const objectFiles: string[] = [];

console.log('Compiling ImGui core...');
for (const src of coreFiles) {
    const srcPath = path.join(SOURCE_DIR, src);
    const objName = path.basename(src, '.cpp') + '.o';
    const objPath = path.join(BUILD_DIR, objName);
    shell.exec(`emcc ${CXXFLAGS} -c "${srcPath}" -o "${objPath}"`);
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
    shell.exec(`emcc ${CXXFLAGS} -c "${srcPath}" -o "${objPath}"`);
    objectFiles.push(objPath);
}

// Create static archive
console.log('Creating libimgui.a...');
const archivePath = path.join(BUILD_DIR, 'libimgui.a');
shell.exec(`emar rcs "${archivePath}" ${objectFiles.map((f) => `"${f}"`).join(' ')}`);

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
