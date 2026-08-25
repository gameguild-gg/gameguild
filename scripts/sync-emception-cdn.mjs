import { cp, mkdir, rm, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, '..');

const emceptionRoot = path.join(repoRoot, 'tools', 'emception');
const sourceCdnDir = path.join(emceptionRoot, 'artifacts', 'toolchain', 'release', 'cdn');
const wasmerSdkCandidates = [
    path.join(repoRoot, 'node_modules', '@wasmer', 'sdk', 'dist'),
    path.join(repoRoot, 'tools', 'emception', 'node_modules', '@wasmer', 'sdk', 'dist'),
];
const requestedDemoDirs = process.argv.slice(2);
const demoDirs = requestedDemoDirs.length
    ? requestedDemoDirs
    : [
        'demos/emception-ide-next',
        'demos/emception-ide-react',
        'demos/emception-run-react',
        'demos/emception-run-webcomponent',
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
    await stat(path.join(sourceCdnDir, 'manifest.json'));
    return sourceCdnDir;
}

async function syncCdnToDemo(demoDirRelative) {
    const demoDirAbsolute = path.resolve(repoRoot, demoDirRelative);
    const targetCdnDir = path.join(demoDirAbsolute, 'public', 'cdn');
    const targetWasmerSdkDir = path.join(targetCdnDir, 'wasmer-sdk');
    const source = await resolveCdnSource();

    await mkdir(path.dirname(targetCdnDir), { recursive: true });
    await removeDirWithRetries(targetCdnDir);

    await cp(source, targetCdnDir, {
        recursive: true,
        force: true,
        dereference: true,
    });

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

    console.log(`[sync-emception-cdn] Synced ${path.relative(repoRoot, source)} -> ${path.relative(repoRoot, targetCdnDir)}`);
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
