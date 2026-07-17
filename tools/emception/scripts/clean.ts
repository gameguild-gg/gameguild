/**
 * Remove all generated build artifacts.
 *
 * Resolves paths relative to this script's location (not `process.cwd()`) so
 * `npm run clean` works from any directory.
 */

import * as fs from 'fs';
import * as path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';
import { defineBuildScript } from './lib/build-script.ts';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SCRIPT_ROOT = path.resolve(__dirname, '..');

defineBuildScript({
    label: 'clean',
    nonFatalShell: true, // legacy behaviour: best-effort cleanup, never bail mid-run
    run: async ({ step, log }) => {
        await step('main outputs', () => {
            shell.rm('-rf', path.join(SCRIPT_ROOT, 'build'));
            // Don't delete dist entirely — it may contain the script that's currently running.
            const dist = path.join(SCRIPT_ROOT, 'dist');
            if (fs.existsSync(dist)) {
                for (const f of fs.readdirSync(dist)) {
                    if (f !== 'scripts') shell.rm('-rf', path.join(dist, f));
                }
            }
            shell.rm('-rf', path.join(SCRIPT_ROOT, 'packages', 'core', 'cdn'));
            for (const dir of ['sysroot', 'sysroot-staging', 'tools', 'playwright-report', 'test-results']) {
                shell.rm('-rf', path.join(SCRIPT_ROOT, dir));
            }
        });

        await step('userland', () => {
            const userlandTrash: readonly string[] = [
                'allegro',
                'allegro/allegro-*',
                'binaryen/build-wasm', 'binaryen/build-native', 'binaryen/binaryen-*', 'binaryen/version_*',
                'cmake/cmake-*', 'cmake/CMake-*',
                'cpython/build-wasm', 'cpython/build-native', 'cpython/cpython-*', 'cpython/sysroot-staging', 'cpython/v*',
                'imgui/imgui-*',
                'llvm/build-wasm', 'llvm/build-native', 'llvm/llvm-project-*', 'llvm/gh-actions-bin',
                'raylib/raylib-*', 'raylib/raygui-*', 'raylib/physac-*', 'raylib/rlights-*',
                'runtime/build',
            ];
            for (const rel of userlandTrash) {
                shell.rm('-rf', path.join(SCRIPT_ROOT, 'userland', rel));
            }
            // brotli: only remove extracted upstream source dirs, not hand-written brotli-wrapper.c
            const brotliDir = path.join(SCRIPT_ROOT, 'userland', 'brotli');
            if (fs.existsSync(brotliDir)) {
                for (const entry of fs.readdirSync(brotliDir)) {
                    if (entry.startsWith('brotli-') && fs.statSync(path.join(brotliDir, entry)).isDirectory()) {
                        shell.rm('-rf', path.join(brotliDir, entry));
                    }
                }
            }
        });

        await step('web app artifacts', () => {
            for (const dir of ['.next', 'playwright-report', 'test-results']) {
                shell.rm('-rf', path.join(SCRIPT_ROOT, dir));
            }
            // Clean CDN from root workspace
            shell.rm('-rf', path.join(SCRIPT_ROOT, 'public/cdn'));
            // Clean CDN from all demo apps
            const appDirs = [
                'apps/ide-react',
                'apps/ide-next',
                'apps/run-react',
                'apps/run-webcomponent',
            ];
            for (const appDir of appDirs) {
                shell.rm('-rf', path.join(SCRIPT_ROOT, appDir, 'public/cdn'));
            }
        });

        await step('node_modules', () => {
            shell.rm('-rf', path.join(SCRIPT_ROOT, 'node_modules'));
        });

        log('Clean complete.');
    },
});
