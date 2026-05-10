import fs from 'node:fs';
import { cp, mkdir, rm, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const emceptionRoot = path.resolve(__dirname, '..');

const sourceBuildDir = path.join(emceptionRoot, 'build');
const sourceCdnDir = path.join(sourceBuildDir, 'cdn');
const sourceManifestFile = path.join(sourceBuildDir, 'manifest.json');
const sourcePublicCdnDir = path.join(emceptionRoot, 'public', 'cdn');
const targetCdnDir = path.join(emceptionRoot, 'packages', 'core', 'cdn');

const TOOL_NAMES = [
    'clang', 'lld', 'python',
    'wasm-opt', 'wasm-as', 'wasm-ctor-eval', 'wasm-emscripten-finalize', 'wasm-metadce',
    'ninja', 'cmake',
];

const CDN_METADATA_FILES = ['.gitignore', '.npmignore'];

async function copyStandaloneToolModules() {
    const toolDest = path.join(targetCdnDir, 'usr', 'lib');
    await mkdir(toolDest, { recursive: true });

    for (const tool of TOOL_NAMES) {
        for (const ext of ['.wasm', '.mjs']) {
            const src = path.join(sourceBuildDir, `${tool}${ext}`);
            if (fs.existsSync(src)) {
                await cp(src, path.join(toolDest, `${tool}${ext}`), { force: true });
            }
        }
    }
}

async function copyCdnMetadataFiles() {
    for (const fileName of CDN_METADATA_FILES) {
        const src = path.join(sourcePublicCdnDir, fileName);
        if (fs.existsSync(src)) {
            await cp(src, path.join(targetCdnDir, fileName), { force: true });
        }
    }
}

async function main() {
    await stat(sourceCdnDir);
    await stat(sourceManifestFile);

    await rm(targetCdnDir, { recursive: true, force: true });
    await mkdir(path.dirname(targetCdnDir), { recursive: true });
    await cp(sourceCdnDir, targetCdnDir, {
        recursive: true,
        force: true,
        dereference: true,
    });

    await copyCdnMetadataFiles();
    await copyStandaloneToolModules();
    await cp(sourceManifestFile, path.join(targetCdnDir, 'manifest.json'), { force: true });

    console.log(
        `[stage-core-cdn] Staged ${path.relative(emceptionRoot, sourceCdnDir)} + manifest -> ${path.relative(emceptionRoot, targetCdnDir)}`,
    );
}

main().catch((error) => {
    console.error('[stage-core-cdn] Failed:', error);
    process.exitCode = 1;
});
