/**
 * assert-no-duplicate-hashes.ts
 *
 * P0 CI assertion: parse the generated manifest.json and assert that no
 * SHA-256 hash appears more than once among non-symlink file entries.
 *
 * Exit 0 = assertion passes (zero duplicates).
 * Exit 1 = duplicate hashes found; prints the offending paths.
 *
 * Usage:
 *   tsx scripts/assert-no-duplicate-hashes.ts
 *   tsx scripts/assert-no-duplicate-hashes.ts --manifest path/to/manifest.json
 */

import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = process.cwd();

// Accept --manifest <path> override
let manifestPath = process.env.MANIFEST_FILE || path.join(ROOT, 'build', 'manifest.json');
const manifestFlagIdx = process.argv.indexOf('--manifest');
if (manifestFlagIdx !== -1 && process.argv[manifestFlagIdx + 1]) {
    manifestPath = process.argv[manifestFlagIdx + 1];
}

console.log(`Checking manifest: ${manifestPath}`);

if (!fs.existsSync(manifestPath)) {
    console.error(`ERROR: manifest not found at ${manifestPath}`);
    process.exit(1);
}

interface ManifestFile {
    size?: number;
    hash?: string;
    executable?: boolean;
    priority?: string;
    symlink?: string;
    bundle?: string;
}

interface Manifest {
    files: Record<string, ManifestFile>;
    bundles?: Record<string, unknown>;
}

const manifest: Manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf-8'));

// Collect non-symlink entries
const hashToPath = new Map<string, string[]>();
let totalFiles = 0;
let symlinkCount = 0;

for (const [filePath, entry] of Object.entries(manifest.files)) {
    if (entry.symlink) {
        symlinkCount++;
        continue;
    }
    if (!entry.hash) continue;
    totalFiles++;
    const existing = hashToPath.get(entry.hash) ?? [];
    existing.push(filePath);
    hashToPath.set(entry.hash, existing);
}

// Find duplicates
const duplicates: Array<{ hash: string; paths: string[] }> = [];
for (const [hash, paths] of hashToPath) {
    if (paths.length > 1) {
        duplicates.push({ hash, paths });
    }
}

const uniqueHashes = hashToPath.size;
console.log(`\nManifest summary:`);
console.log(`  Total file entries:   ${totalFiles + symlinkCount}`);
console.log(`  Non-symlink files:    ${totalFiles}`);
console.log(`  Symlink entries:      ${symlinkCount}`);
console.log(`  Unique SHA-256 hashes: ${uniqueHashes}`);
console.log(`  Duplicate hashes:     ${duplicates.length}`);

if (duplicates.length > 0) {
    console.error(`\nFAIL: ${duplicates.length} duplicate SHA-256 hash(es) found among non-symlink entries:`);
    for (const { hash, paths } of duplicates) {
        console.error(`\n  Hash: ${hash.slice(0, 16)}...`);
        for (const p of paths) {
            console.error(`    ${p}`);
        }
    }
    console.error(`\nRun 'npm run build:bundles' to regenerate bundles with deduplication enabled.`);
    process.exit(1);
}

console.log(`\nPASS: Duplicate hashes: 0`);
