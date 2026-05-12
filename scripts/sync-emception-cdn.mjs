import { cp, mkdir, rm, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, '..');

const emceptionRoot = path.join(repoRoot, 'tools', 'emception');
const buildDir = path.join(emceptionRoot, 'build');
const sourceBuildCdnDir = path.join(buildDir, 'cdn');
const sourceManifestFile = path.join(buildDir, 'manifest.json');
const sourcePublicCdnDir = path.join(emceptionRoot, 'public', 'cdn');
const wasmerSdkCandidates = [
    path.join(repoRoot, 'node_modules', '@wasmer', 'sdk', 'dist'),
    path.join(repoRoot, 'tools', 'emception', 'node_modules', '@wasmer', 'sdk', 'dist'),
];
const standaloneToolNames = [
    'clang', 'lld', 'python',
    'wasm-opt', 'wasm-as', 'wasm-ctor-eval', 'wasm-emscripten-finalize', 'wasm-metadce',
    'ninja', 'cmake',
];

const requestedDemoDirs = process.argv.slice(2);
const demoDirs = requestedDemoDirs.length
    ? requestedDemoDirs
    : [
        'tools/emception/apps/ide-next',
        'tools/emception/apps/ide-react',
        'tools/emception/apps/run-react',
        'tools/emception/apps/run-webcomponent',
    ];

async function sleep(ms) {
    await new Promise((resolve) => setTimeout(resolve, ms));
}

async function removeDirWithRetries(dirPath, maxRetries = 5) {
    for (let attempt = 0; attempt <= maxRetries; attempt++) {
        try {
            await rm(dirPath, { recursive: true, force: true, maxRetries: 3, retryDelay: 50 });
            return;
        } catch (error) {
            const isRetriable = error && (error.code === 'ENOTEMPTY' || error.code === 'EBUSY' || error.code === 'EPERM');
            if (!isRetriable || attempt === maxRetries) {
                throw error;
            }
            await sleep(100 * (attempt + 1));
        }
    }
}

async function exists(pathToCheck) {
    try {
        await stat(pathToCheck);
        return true;
    } catch {
        return false;
    }
}

async function resolveCdnSource() {
    if ((await exists(sourceBuildCdnDir)) && (await exists(sourceManifestFile))) {
        return { mode: 'build', dir: sourceBuildCdnDir };
    }

    await stat(sourcePublicCdnDir);
    return { mode: 'public', dir: sourcePublicCdnDir };
}

async function copyStandaloneToolModules(targetCdnDir) {
    const toolDest = path.join(targetCdnDir, 'usr', 'lib');
    await mkdir(toolDest, { recursive: true });

    for (const tool of standaloneToolNames) {
        for (const ext of ['.wasm', '.mjs']) {
            const src = path.join(buildDir, `${tool}${ext}`);
            if (await exists(src)) {
                await cp(src, path.join(toolDest, `${tool}${ext}`), {
                    force: true,
                    dereference: true,
                });
            }
        }
    }
}

async function syncCdnToDemo(demoDirRelative) {
    const demoDirAbsolute = path.resolve(repoRoot, demoDirRelative);
    const targetCdnDir = path.join(demoDirAbsolute, 'public', 'cdn');
    const targetWasmerSdkDir = path.join(targetCdnDir, 'wasmer-sdk');
    const source = await resolveCdnSource();

    await mkdir(path.dirname(targetCdnDir), { recursive: true });
    await removeDirWithRetries(targetCdnDir);

    if (source.mode === 'build') {
        await cp(source.dir, targetCdnDir, {
            recursive: true,
            force: true,
            dereference: true,
        });
        await copyStandaloneToolModules(targetCdnDir);
        await cp(sourceManifestFile, path.join(targetCdnDir, 'manifest.json'), {
            force: true,
        });
    } else {
        await cp(source.dir, targetCdnDir, {
            recursive: true,
            force: true,
            dereference: true,
        });
    }

    let resolvedWasmerSdkSource = null;
    for (const candidate of wasmerSdkCandidates) {
        try {
            await stat(candidate);
            resolvedWasmerSdkSource = candidate;
            break;
        } catch {
            // try next candidate
        }
    }

    if (resolvedWasmerSdkSource) {
        await mkdir(targetWasmerSdkDir, { recursive: true });
        await removeDirWithRetries(targetWasmerSdkDir);
        await cp(resolvedWasmerSdkSource, targetWasmerSdkDir, {
            recursive: true,
            force: true,
            dereference: true,
        });
        console.log(
            `[sync-emception-cdn] Synced ${path.relative(repoRoot, resolvedWasmerSdkSource)} -> ${path.relative(repoRoot, targetWasmerSdkDir)}`,
        );
    } else {
        console.warn('[sync-emception-cdn] @wasmer/sdk dist not found; skipping wasmer-sdk sync');
    }

    console.log(`[sync-emception-cdn] Synced ${path.relative(repoRoot, source.dir)} (${source.mode}) -> ${path.relative(repoRoot, targetCdnDir)}`);
}

async function main() {
    await resolveCdnSource();

    for (const demoDir of demoDirs) {
        await syncCdnToDemo(demoDir);
    }
}

main().catch((error) => {
    console.error('[sync-emception-cdn] Failed:', error);
    process.exitCode = 1;
});
