import fs from 'node:fs';
import { cp, mkdir, readdir, rm, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const emceptionRoot = path.resolve(__dirname, '..');

const sourceBuildDir = path.join(emceptionRoot, 'build');
const sourceBuildCdnDir = path.join(sourceBuildDir, 'cdn');
const sourceBuildManifestFile = path.join(sourceBuildCdnDir, 'manifest.json');
const sourcePublicCdnDir = path.join(emceptionRoot, 'public', 'cdn');
const sourcePublicManifestFile = path.join(sourcePublicCdnDir, 'manifest.json');
const targetCdnDir = path.join(emceptionRoot, 'packages', 'core', 'cdn');
const targetManifestFile = path.join(targetCdnDir, 'manifest.json');

const CDN_METADATA_FILES = ['.gitignore', '.npmignore'];
const CDN_METADATA_FILE_SET = new Set(CDN_METADATA_FILES);
const REQUIRED_EXACT_FILES = new Set(['manifest.json', 'brotli_wasm.js', 'brotli_wasm.wasm']);

function isAllowedCdnFile(fileName) {
    return REQUIRED_EXACT_FILES.has(fileName)
        || CDN_METADATA_FILE_SET.has(fileName)
        || fileName.endsWith('.tar.br');
}

async function exists(filePath) {
    try {
        await stat(filePath);
        return true;
    } catch {
        return false;
    }
}

async function resolveStagingSource() {
    // Only build/cdn is a valid source.  public/cdn and packages/core/cdn may
    // contain stale bundles from a previous build and must NEVER be used as a
    // silent fallback — that was the root cause of the v3.5.0 regression where
    // the published tarball was 164 MB instead of ~150 MB.
    if (await exists(sourceBuildCdnDir) && await exists(sourceBuildManifestFile)) {
        return {
            label: 'build/cdn',
            cdnDir: sourceBuildCdnDir,
            manifestFile: sourceBuildManifestFile,
        };
    }

    throw new Error(
        [
            'Unable to stage the emception core CDN.',
            `build/cdn artifacts not found at: ${sourceBuildCdnDir}`,
            'Run `npm run build:cdn:serial` (or `npm run build:all`) first.',
        ].join('\n'),
    );
}

async function copyCdnMetadataFiles() {
    for (const fileName of CDN_METADATA_FILES) {
        const src = path.join(sourcePublicCdnDir, fileName);
        if (fs.existsSync(src)) {
            await cp(src, path.join(targetCdnDir, fileName), { force: true });
        }
    }
}

async function pruneCdnToPublishPayload(rootDir) {
    const entries = await readdir(rootDir, { withFileTypes: true });

    for (const entry of entries) {
        const fullPath = path.join(rootDir, entry.name);

        if (entry.isDirectory()) {
            await pruneCdnToPublishPayload(fullPath);
            const remaining = await readdir(fullPath);
            if (remaining.length === 0) {
                await rm(fullPath, { recursive: true, force: true });
            }
            continue;
        }

        if (!isAllowedCdnFile(entry.name)) {
            await rm(fullPath, { force: true });
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
    if (source.manifestFile !== targetManifestFile) {
        await cp(source.manifestFile, targetManifestFile, { force: true });
    }
    await pruneCdnToPublishPayload(targetCdnDir);

    if (!(await exists(targetManifestFile))) {
        throw new Error(`manifest.json missing after prune: ${path.relative(emceptionRoot, targetManifestFile)}`);
    }

    console.log(
        `[stage-core-cdn] Staged ${path.relative(emceptionRoot, source.cdnDir)} + manifest -> ${path.relative(emceptionRoot, targetCdnDir)}`,
    );
}

main().catch((error) => {
    console.error('[stage-core-cdn] Failed:', error);
    process.exitCode = 1;
});
