import { cp, mkdir, rm, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, '..');

const sourceCdnDir = path.join(repoRoot, 'tools', 'emception', 'public', 'cdn');

const requestedDemoDirs = process.argv.slice(2);
const demoDirs = requestedDemoDirs.length
    ? requestedDemoDirs
    : ['demos/emception-next', 'demos/emception-react'];

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

async function syncCdnToDemo(demoDirRelative) {
    const demoDirAbsolute = path.resolve(repoRoot, demoDirRelative);
    const targetCdnDir = path.join(demoDirAbsolute, 'public', 'cdn');

    await mkdir(path.dirname(targetCdnDir), { recursive: true });
    await removeDirWithRetries(targetCdnDir);
    await cp(sourceCdnDir, targetCdnDir, {
        recursive: true,
        force: true,
        dereference: true,
    });

    console.log(
        `[sync-emception-cdn] Synced ${path.relative(repoRoot, sourceCdnDir)} -> ${path.relative(repoRoot, targetCdnDir)}`,
    );
}

async function main() {
    await stat(sourceCdnDir);

    for (const demoDir of demoDirs) {
        await syncCdnToDemo(demoDir);
    }
}

main().catch((error) => {
    console.error('[sync-emception-cdn] Failed:', error);
    process.exitCode = 1;
});
