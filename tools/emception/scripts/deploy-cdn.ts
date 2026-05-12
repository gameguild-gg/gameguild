/**
 * Copy build artifacts (CDN payload + standalone tool modules + manifest +
 * brotli decompressor) into `public/cdn` for Next.js / Vite static serving.
 */

import fs from 'fs';
import path from 'path';
import shell from 'shelljs';
import { defineBuildScript } from './lib/build-script.ts';
import { paths } from './lib/paths.ts';

const CDN_METADATA_FILES = new Set(['.gitignore', '.npmignore']);
const REQUIRED_EXACT_FILES = new Set(['manifest.json', 'brotli_wasm.js', 'brotli_wasm.wasm']);

function isAllowedCdnFile(fileName: string): boolean {
    return REQUIRED_EXACT_FILES.has(fileName)
        || CDN_METADATA_FILES.has(fileName)
        || fileName.endsWith('.tar.br');
}

function pruneCdnToPublishPayload(rootDir: string, log: (message: string) => void): void {
    if (!fs.existsSync(rootDir)) return;

    const walk = (dir: string): void => {
        const entries = fs.readdirSync(dir, { withFileTypes: true });
        for (const entry of entries) {
            const fullPath = path.join(dir, entry.name);
            if (entry.isDirectory()) {
                walk(fullPath);
                const remaining = fs.readdirSync(fullPath);
                if (remaining.length === 0) {
                    fs.rmdirSync(fullPath);
                    log(`pruned empty directory: ${path.relative(rootDir, fullPath)}`);
                }
                continue;
            }

            if (!isAllowedCdnFile(entry.name)) {
                fs.unlinkSync(fullPath);
                log(`pruned raw file: ${path.relative(rootDir, fullPath)}`);
            }
        }
    };

    walk(rootDir);
}

defineBuildScript({
    label: 'deploy-cdn',
    run: async ({ step, log }) => {
        const P = paths();
        // The Next.js app root is the emception directory itself (not a separate web/ subdir).
        const dest = fs.existsSync(path.join(P.root, 'web'))
            ? path.join(P.root, 'web/public/cdn')
            : P.publicCdn;

        await step(`copy build/cdn → ${dest}`, () => {
            shell.rm('-rf', dest);
            shell.mkdir('-p', dest);
            shell.cp('-r', path.join(P.buildCdn, '*'), dest);
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

        await step('prune CDN payload for publish', () => {
            pruneCdnToPublishPayload(dest, log);
            if (!fs.existsSync(path.join(dest, 'manifest.json'))) {
                throw new Error(`manifest.json missing after prune in ${dest}`);
            }
        });

        log('CDN files deployed.');
    },
});
