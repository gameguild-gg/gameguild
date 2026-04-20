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
    'clang', 'lld', 'python', 'rustc',
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

// Copy bundled brotli WASM files (needed by the worker for decompressing .tar.br bundles
// when DecompressionStream("br") is not available).
// Use the wasm-bindgen `brotli-wasm` npm package which provides the library API
// (default init + decompress) expected by worker-entry.ts.
// NOTE: The Emscripten build (build-brotli.ts) produces a CLI tool, NOT the library
// module the worker needs. Do NOT use build/cdn/brotli_wasm.* here.
const BROTLI_WASM_PKG = path.resolve(ROOT, '..', '..', '..', 'node_modules', 'brotli-wasm', 'pkg.web');
const brotliCandidates: Array<{ srcCandidates: string[]; dest: string }> = [
    {
        srcCandidates: [
            path.join(BROTLI_WASM_PKG, 'brotli_wasm.js'),
            path.join(ROOT, 'src', 'lib', 'orchestrator', 'loader', 'cdn', 'brotli_wasm.js'),
        ],
        dest: 'brotli_wasm.js',
    },
    {
        srcCandidates: [
            path.join(BROTLI_WASM_PKG, 'brotli_wasm_bg.wasm'),
            path.join(ROOT, 'src', 'lib', 'orchestrator', 'loader', 'cdn', 'brotli_wasm_bg.wasm'),
        ],
        dest: 'brotli_wasm_bg.wasm',
    },
];

for (const { srcCandidates, dest } of brotliCandidates) {
    const existing = srcCandidates.find((p) => fs.existsSync(p));
    if (existing) {
        console.log(`Copying ${existing} to ${WEB_NEXT_CDN}/${dest}...`);
        shell.cp(existing, path.join(WEB_NEXT_CDN, dest));
    } else {
        console.warn(`Warning: no source found for ${dest}. Checked: ${srcCandidates.join(', ')}`);
    }
}

console.log('CDN files deployed.');
