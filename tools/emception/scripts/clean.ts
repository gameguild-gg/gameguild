/**
 * Remove all generated build artifacts.
 *
 * Stdlib-only (`node:fs` / `node:path`) on purpose: this script also deletes
 * `node_modules`, so it MUST still run after `node_modules` is gone. Importing
 * `shelljs` or `defineBuildScript` here would re-introduce the chicken-and-egg
 * crash (`ERR_MODULE_NOT_FOUND: shelljs`) the moment a previous clean left
 * `node_modules` deleted.
 *
 * Paths resolve relative to this script's location (not `process.cwd()`) so
 * `pnpm run clean` works from any directory.
 */

import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SCRIPT_ROOT = path.resolve(__dirname, '..');
const LABEL = 'clean';

/** Recursively remove a path; never throws if it's already gone. */
const rmrf = (p: string): void => {
    fs.rmSync(p, { recursive: true, force: true });
};

/**
 * Remove entries under `parentDir` matching a `prefix-*` / `prefix_*` glob.
 * Only supports a single trailing star in the final segment — that's all the
 * userland trash list needs. Literal patterns (no `*`) are rm'd as-is.
 */
const rmUnder = (parentDir: string, pattern: string): void => {
    if (!pattern.includes('*')) {
        rmrf(path.join(parentDir, pattern));
        return;
    }
    if (!fs.existsSync(parentDir)) return;
    const starIdx = pattern.indexOf('*');
    const prefix = pattern.slice(0, starIdx);
    for (const entry of fs.readdirSync(parentDir)) {
        if (entry.startsWith(prefix)) rmrf(path.join(parentDir, entry));
    }
};

const log = (msg: string): void => console.log(`[${LABEL}] ${msg}`);

const step = async (name: string, fn: () => void | Promise<void>): Promise<void> => {
    log(`▶ ${name}`);
    const start = Date.now();
    try {
        await fn();
        const secs = ((Date.now() - start) / 1000).toFixed(1);
        log(`✓ ${name} (${secs}s)`);
    } catch (err) {
        log(`✗ ${name}`);
        throw err;
    }
};

await step('main outputs', () => {
    rmrf(path.join(SCRIPT_ROOT, 'build'));
    // Preserve `dist/scripts` — it may host the currently-running compiled
    // script when invoked through other tooling. Everything else in dist is
    // regenerated.
    const dist = path.join(SCRIPT_ROOT, 'dist');
    if (fs.existsSync(dist)) {
        for (const f of fs.readdirSync(dist)) {
            if (f !== 'scripts') rmrf(path.join(dist, f));
        }
    }
    rmrf(path.join(SCRIPT_ROOT, 'packages', 'core', 'cdn'));
    for (const dir of ['sysroot', 'sysroot-staging', 'tools', 'playwright-report', 'test-results']) {
        rmrf(path.join(SCRIPT_ROOT, dir));
    }
});

await step('packages', () => {
    const pkgsDir = path.join(SCRIPT_ROOT, 'packages');
    if (!fs.existsSync(pkgsDir)) return;
    for (const pkg of fs.readdirSync(pkgsDir)) {
        const pkgDir = path.join(pkgsDir, pkg);
        if (!fs.statSync(pkgDir).isDirectory()) continue;
        rmrf(path.join(pkgDir, 'dist'));
        rmrf(path.join(pkgDir, 'node_modules'));
        rmrf(path.join(pkgDir, 'tsconfig.tsbuildinfo'));
    }
});

await step('userland', () => {
    const userlandTrash: readonly string[] = [
        'allegro', 'allegro/allegro-*',
        'binaryen/build-wasm', 'binaryen/build-native', 'binaryen/binaryen-*', 'binaryen/version_*',
        'cmake/cmake-*', 'cmake/CMake-*',
        'cpython/build-wasm', 'cpython/build-native', 'cpython/cpython-*', 'cpython/sysroot-staging', 'cpython/v*',
        'imgui/imgui-*',
        'llvm/build-wasm', 'llvm/build-native', 'llvm/llvm-project-*', 'llvm/gh-actions-bin',
        'raylib/raylib-*', 'raylib/raygui-*', 'raylib/physac-*', 'raylib/rlights-*',
        'runtime/build',
    ];
    for (const rel of userlandTrash) {
        const parts = rel.split('/');
        const parentDir = path.join(SCRIPT_ROOT, 'userland', ...parts.slice(0, -1));
        rmUnder(parentDir, parts[parts.length - 1]);
    }
    // brotli: only remove extracted upstream source dirs, not hand-written brotli-wrapper.c
    const brotliDir = path.join(SCRIPT_ROOT, 'userland', 'brotli');
    if (fs.existsSync(brotliDir)) {
        for (const entry of fs.readdirSync(brotliDir)) {
            if (entry.startsWith('brotli-') && fs.statSync(path.join(brotliDir, entry)).isDirectory()) {
                rmrf(path.join(brotliDir, entry));
            }
        }
    }
});

await step('web app artifacts', () => {
    for (const dir of ['.next', 'playwright-report', 'test-results']) {
        rmrf(path.join(SCRIPT_ROOT, dir));
    }
    // Clean CDN from root workspace
    rmrf(path.join(SCRIPT_ROOT, 'public/cdn'));
    // Clean CDN + per-app build state from all demo apps
    const appDirs = ['apps/ide-react', 'apps/ide-next', 'apps/run-react', 'apps/run-webcomponent'];
    for (const appDir of appDirs) {
        rmrf(path.join(SCRIPT_ROOT, appDir, 'public/cdn'));
        rmrf(path.join(SCRIPT_ROOT, appDir, '.next'));
        rmrf(path.join(SCRIPT_ROOT, appDir, 'dist'));
        rmrf(path.join(SCRIPT_ROOT, appDir, 'node_modules'));
    }
});

await step('node_modules', () => {
    rmrf(path.join(SCRIPT_ROOT, 'node_modules'));
});

log('Clean complete.');
