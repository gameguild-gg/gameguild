import fs from 'fs';
import path from 'path';
import shell from 'shelljs';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT = process.cwd();
const BUILD_DIR = path.join(ROOT, 'build');

// Ensure shell commands fail on error
shell.config.fatal = true;

// Copy CDN files to web/public/cdn for local development
const WEB_NEXT_CDN = path.join(ROOT, 'web/public/cdn');
if (fs.existsSync(path.join(ROOT, 'web'))) {
    console.log(`Copying CDN files to ${WEB_NEXT_CDN}...`);
    shell.mkdir('-p', WEB_NEXT_CDN);
    shell.cp('-r', path.join(BUILD_DIR, 'cdn/*'), WEB_NEXT_CDN);

    // Copy standalone tool modules (.wasm + .mjs)
    const toolNames = [
        'clang', 'lld', 'python',
        'wasm-opt', 'wasm-as', 'wasm-ctor-eval', 'wasm-emscripten-finalize', 'wasm-metadce',
    ];
    const toolDest = path.join(WEB_NEXT_CDN, 'usr', 'lib');
    shell.mkdir('-p', toolDest);
    for (const tool of toolNames) {
        for (const ext of ['.wasm', '.mjs']) {
            const src = path.join(BUILD_DIR, `${tool}${ext}`);
            if (fs.existsSync(src)) {
                console.log(`Copying ${tool}${ext} to ${toolDest}...`);
                shell.cp(src, toolDest);
            } else {
                console.warn(`Warning: ${tool}${ext} not found at ${src}`);
            }
        }
    }

    // Copy manifest.json
    const manifestSrc = path.join(BUILD_DIR, 'manifest.json');
    if (fs.existsSync(manifestSrc)) {
        console.log(`Copying manifest.json to ${WEB_NEXT_CDN}...`);
        shell.cp(manifestSrc, WEB_NEXT_CDN);
    } else {
        console.warn(`Warning: manifest.json not found at ${manifestSrc}`);
    }

    console.log('CDN files deployed.');
} else {
    console.log('web directory not found, skipping CDN deployment.');
}
