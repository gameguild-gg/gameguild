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

// Copy CDN files to public/cdn for Next.js static serving.
// The Next.js app root is the emception directory itself (not a separate web/ subdir).
const WEB_NEXT_CDN = fs.existsSync(path.join(ROOT, 'web'))
    ? path.join(ROOT, 'web/public/cdn')
    : path.join(ROOT, 'public/cdn');

console.log(`Copying CDN files to ${WEB_NEXT_CDN}...`);
shell.mkdir('-p', WEB_NEXT_CDN);
shell.cp('-r', path.join(BUILD_DIR, 'cdn/*'), WEB_NEXT_CDN);

// Copy standalone tool modules (.wasm + .mjs)
const toolNames = [
    'clang', 'lld', 'python',
    'wasm-opt', 'wasm-as', 'wasm-ctor-eval', 'wasm-emscripten-finalize', 'wasm-metadce',
    'ninja', 'cmake',
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

// Brotli decompressor (browser-side) is built locally by `npm run build:brotli`
// (see scripts/build-brotli.ts). The output (`brotli_wasm.js` + `brotli_wasm.wasm`)
// lives in build/cdn/ and is already shipped to public/cdn/ by the wildcard copy
// above. We do NOT depend on the npm `brotli-wasm` package.
const brotliJs = path.join(BUILD_DIR, 'cdn', 'brotli_wasm.js');
const brotliWasm = path.join(BUILD_DIR, 'cdn', 'brotli_wasm.wasm');
if (!fs.existsSync(brotliJs) || !fs.existsSync(brotliWasm)) {
    throw new Error(
        `Locally-built brotli not found in ${path.join(BUILD_DIR, 'cdn')}. Run \`npm run build:brotli\` first.`,
    );
}
// Remove the legacy filename if a previous build left it behind.
const legacyBrotliWasm = path.join(WEB_NEXT_CDN, 'brotli_wasm_bg.wasm');
if (fs.existsSync(legacyBrotliWasm)) {
    console.log(`Removing legacy ${legacyBrotliWasm}`);
    shell.rm('-f', legacyBrotliWasm);
}
// Verify the new filename actually landed in the deploy target.
const deployedBrotliWasm = path.join(WEB_NEXT_CDN, 'brotli_wasm.wasm');
if (!fs.existsSync(deployedBrotliWasm)) {
    throw new Error(`Brotli wasm missing after deploy: expected ${deployedBrotliWasm}`);
}
console.log(`Deployed locally-built brotli decompressor from ${path.join(BUILD_DIR, 'cdn')}.`);

console.log('CDN files deployed.');
