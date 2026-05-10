import fs from 'node:fs';
import { cp, mkdir, rm, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const emceptionRoot = path.resolve(__dirname, '..');

const sourceBuildDir = path.join(emceptionRoot, 'build');
const sourceBuildCdnDir = path.join(sourceBuildDir, 'cdn');
const sourceBuildManifestFile = path.join(sourceBuildDir, 'manifest.json');
const sourcePublicCdnDir = path.join(emceptionRoot, 'public', 'cdn');
const sourcePublicManifestFile = path.join(sourcePublicCdnDir, 'manifest.json');
const targetCdnDir = path.join(emceptionRoot, 'packages', 'core', 'cdn');
const targetManifestFile = path.join(targetCdnDir, 'manifest.json');

const TOOL_NAMES = [
    'clang', 'lld', 'python',
    'wasm-opt', 'wasm-as', 'wasm-ctor-eval', 'wasm-emscripten-finalize', 'wasm-metadce',
    'ninja', 'cmake',
];

const CDN_METADATA_FILES = ['.gitignore', '.npmignore'];

async function exists(filePath) {
    try {
        await stat(filePath);
        return true;
    } catch {
        return false;
    }
}

async function resolveStagingSource() {
    const candidates = [
        {
            label: 'build/cdn',
            cdnDir: sourceBuildCdnDir,
            manifestFile: sourceBuildManifestFile,
        },
        {
            label: 'public/cdn',
            cdnDir: sourcePublicCdnDir,
            manifestFile: sourcePublicManifestFile,
        },
        {
            label: 'packages/core/cdn',
            cdnDir: targetCdnDir,
            manifestFile: targetManifestFile,
        },
    ];

    for (const candidate of candidates) {
        if (await exists(candidate.cdnDir) && await exists(candidate.manifestFile)) {
            if (candidate.cdnDir !== sourceBuildCdnDir) {
                console.log(
                    `[stage-core-cdn] ${path.relative(emceptionRoot, sourceBuildCdnDir)} unavailable; reusing ${path.relative(emceptionRoot, candidate.cdnDir)}.`,
                );
            }
            return candidate;
        }
    }

    throw new Error(
        [
            'Unable to stage the emception core CDN.',
            'Expected one of these artifact sets to exist:',
            `- ${path.relative(emceptionRoot, sourceBuildCdnDir)} + ${path.relative(emceptionRoot, sourceBuildManifestFile)}`,
            `- ${path.relative(emceptionRoot, sourcePublicCdnDir)} + ${path.relative(emceptionRoot, sourcePublicManifestFile)}`,
            `- ${path.relative(emceptionRoot, targetCdnDir)} + ${path.relative(emceptionRoot, targetManifestFile)}`,
        ].join('\n'),
    );
}

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
    const source = await resolveStagingSource();

    if (source.cdnDir !== targetCdnDir) {
        await rm(targetCdnDir, { recursive: true, force: true });
        await mkdir(path.dirname(targetCdnDir), { recursive: true });
        await cp(source.cdnDir, targetCdnDir, {
            recursive: true,
            force: true,
            dereference: true,
        });
    }

    await copyCdnMetadataFiles();
    await copyStandaloneToolModules();
    if (source.manifestFile !== targetManifestFile) {
        await cp(source.manifestFile, targetManifestFile, { force: true });
    }

    console.log(
        `[stage-core-cdn] Staged ${path.relative(emceptionRoot, source.cdnDir)} + manifest -> ${path.relative(emceptionRoot, targetCdnDir)}`,
    );
}

main().catch((error) => {
    console.error('[stage-core-cdn] Failed:', error);
    process.exitCode = 1;
});
