/**
 * Copy build artifacts (CDN payload + standalone tool modules + manifest +
 * brotli decompressor) into `public/cdn` for Next.js / Vite static serving.
 */

import fs from 'fs';
import path from 'path';
import shell from 'shelljs';
import { defineBuildScript } from './lib/build-script.ts';
import { paths } from './lib/paths.ts';

const TOOL_NAMES = [
    'clang', 'lld', 'python',
    'wasm-opt', 'wasm-as', 'wasm-ctor-eval', 'wasm-emscripten-finalize', 'wasm-metadce',
    'ninja', 'cmake',
] as const;

defineBuildScript({
    label: 'deploy-cdn',
    run: async ({ step, log }) => {
        const P = paths();
        // The Next.js app root is the emception directory itself (not a separate web/ subdir).
        const dest = fs.existsSync(path.join(P.root, 'web'))
            ? path.join(P.root, 'web/public/cdn')
            : P.publicCdn;

        await step(`copy build/cdn → ${dest}`, () => {
            shell.mkdir('-p', dest);
            shell.cp('-r', path.join(P.buildCdn, '*'), dest);
        });

        await step('copy standalone tool modules', () => {
            const toolDest = path.join(dest, 'usr', 'lib');
            shell.mkdir('-p', toolDest);
            for (const tool of TOOL_NAMES) {
                for (const ext of ['.wasm', '.mjs']) {
                    const src = path.join(P.build, `${tool}${ext}`);
                    if (fs.existsSync(src)) {
                        shell.cp(src, toolDest);
                    } else {
                        log(`WARN: ${tool}${ext} not found at ${src}`);
                    }
                }
            }
        });

        await step('copy manifest.json', () => {
            if (fs.existsSync(P.manifestFile)) {
                shell.cp(P.manifestFile, dest);
            } else {
                log(`WARN: manifest.json not found at ${P.manifestFile}`);
            }
        });

        await step('verify brotli decompressor', () => {
            // Brotli decompressor (browser-side) is built locally by `npm run build:brotli`.
            // Output (`brotli_wasm.js` + `brotli_wasm.wasm`) lives in build/cdn/ and is
            // already shipped to the deploy target by the wildcard copy above.
            const brotliJs = path.join(P.buildCdn, 'brotli_wasm.js');
            const brotliWasm = path.join(P.buildCdn, 'brotli_wasm.wasm');
            if (!fs.existsSync(brotliJs) || !fs.existsSync(brotliWasm)) {
                throw new Error(
                    `Locally-built brotli not found in ${P.buildCdn}. Run \`npm run build:brotli\` first.`,
                );
            }
            // Remove the legacy filename if a previous build left it behind.
            const legacy = path.join(dest, 'brotli_wasm_bg.wasm');
            if (fs.existsSync(legacy)) {
                shell.rm('-f', legacy);
            }
            const deployed = path.join(dest, 'brotli_wasm.wasm');
            if (!fs.existsSync(deployed)) {
                throw new Error(`Brotli wasm missing after deploy: expected ${deployed}`);
            }
        });

        log('CDN files deployed.');
    },
});
