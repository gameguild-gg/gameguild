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
            for (const dir of ['sysroot', 'sysroot-staging', 'tools', 'playwright-report', 'test-results']) {
                shell.rm('-rf', path.join(SCRIPT_ROOT, dir));
            }
        });

        await step('userland', () => {
            const userlandTrash: readonly string[] = [
                'binaryen/build-wasm', 'binaryen/build-native', 'binaryen/binaryen-*', 'binaryen/version_*',
                'cpython/build-wasm', 'cpython/build-native', 'cpython/cpython-*', 'cpython/sysroot-staging', 'cpython/v*',
                'llvm/build-wasm', 'llvm/build-native', 'llvm/llvm-project-*', 'llvm/gh-actions-bin',
                'raylib/build-wasm', 'raylib/raylib-*', 'raylib/raygui-*', 'raylib/physac-*', 'raylib/rlights-*',
                'runtime/build',
            ];
            for (const rel of userlandTrash) {
                shell.rm('-rf', path.join(SCRIPT_ROOT, 'userland', rel));
            }
        });

        await step('web app artifacts', () => {
            for (const dir of ['.next', 'playwright-report', 'test-results']) {
                shell.rm('-rf', path.join(SCRIPT_ROOT, dir));
            }
            const cdnDir = path.join(SCRIPT_ROOT, 'public/cdn');
            if (fs.existsSync(cdnDir)) {
                shell.rm('-rf', path.join(cdnDir, 'etc'));
                shell.rm('-rf', path.join(cdnDir, 'usr'));
                shell.rm('-f', path.join(cdnDir, 'manifest.json'));
            }
        });

        await step('node_modules', () => {
            shell.rm('-rf', path.join(SCRIPT_ROOT, 'node_modules'));
        });

        log('Clean complete.');
    },
});
