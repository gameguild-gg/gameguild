import * as fs from 'fs';
import * as path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';
import { enableBuildKeepalive } from './lib/keepalive.ts';

enableBuildKeepalive('clean');

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const ROOT = path.resolve(__dirname, '..');

console.log('Cleaning build artifacts...');

// 1. Main build outputs
shell.rm('-rf', path.join(ROOT, 'build'));
// Don't delete dist entirely as it might contain the running script
if (fs.existsSync(path.join(ROOT, 'dist'))) {
    const files = fs.readdirSync(path.join(ROOT, 'dist'));
    for (const file of files) {
        if (file !== 'scripts') {
            shell.rm('-rf', path.join(ROOT, 'dist', file));
        }
    }
}
shell.rm('-rf', path.join(ROOT, 'sysroot'));
shell.rm('-rf', path.join(ROOT, 'sysroot-staging'));
shell.rm('-rf', path.join(ROOT, 'tools'));
shell.rm('-rf', path.join(ROOT, 'playwright-report'));
shell.rm('-rf', path.join(ROOT, 'test-results'));


// 2. Userland build artifacts
console.log('Cleaning userland...');

// Binaryen
shell.rm('-rf', path.join(ROOT, 'userland/binaryen/build-wasm'));
shell.rm('-rf', path.join(ROOT, 'userland/binaryen/build-native'));
shell.rm('-rf', path.join(ROOT, 'userland/binaryen/binaryen-*'));
shell.rm('-rf', path.join(ROOT, 'userland/binaryen/version_*'));

// CPython
shell.rm('-rf', path.join(ROOT, 'userland/cpython/build-wasm'));
shell.rm('-rf', path.join(ROOT, 'userland/cpython/build-native'));
shell.rm('-rf', path.join(ROOT, 'userland/cpython/cpython-*'));
shell.rm('-rf', path.join(ROOT, 'userland/cpython/sysroot-staging'));
shell.rm('-rf', path.join(ROOT, 'userland/cpython/v*'));

// LLVM
shell.rm('-rf', path.join(ROOT, 'userland/llvm/build-wasm'));
shell.rm('-rf', path.join(ROOT, 'userland/llvm/build-native'));
shell.rm('-rf', path.join(ROOT, 'userland/llvm/llvm-project-*'));
shell.rm('-rf', path.join(ROOT, 'userland/llvm/gh-actions-bin'));

// Runtime
shell.rm('-rf', path.join(ROOT, 'userland/runtime/build'));


// 3. Web app build artifacts and test outputs
console.log('Cleaning web app artifacts...');
shell.rm('-rf', path.join(ROOT, '.next'));
shell.rm('-rf', path.join(ROOT, 'playwright-report'));
shell.rm('-rf', path.join(ROOT, 'test-results'));

// CDN generated contents (keep the .gitignore)
const cdnDir = path.join(ROOT, 'public/cdn');
if (fs.existsSync(cdnDir)) {
    shell.rm('-rf', path.join(cdnDir, 'etc'));
    shell.rm('-rf', path.join(cdnDir, 'usr'));
    shell.rm('-f', path.join(cdnDir, 'manifest.json'));
}

// 4. Common / Emscripten caches if needed (optional)
// Clean ports cache if desired, but skipping for now as per original script.

shell.rm('-rf', path.join(ROOT, 'node_modules'));

console.log('Clean complete.');
