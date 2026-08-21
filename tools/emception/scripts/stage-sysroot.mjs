import { createHash } from 'node:crypto';
import { cp, lstat, mkdir, readFile, readdir, readlink, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { toolchainPaths } from './toolchain/paths.ts';

async function collectSnapshot(root) {
  const entries = [];

  async function walk(directory) {
    const children = await readdir(directory, { withFileTypes: true });
    for (const child of children.sort((left, right) => left.name.localeCompare(right.name))) {
      const absolutePath = path.join(directory, child.name);
      const relativePath = path.relative(root, absolutePath).replaceAll('\\', '/');
      if (child.isDirectory()) {
        await walk(absolutePath);
      } else if (child.isSymbolicLink()) {
        const stats = await lstat(absolutePath);
        const target = await readlink(absolutePath);
        entries.push({ path: relativePath, size: stats.size, hash: `symlink:${target}` });
      } else if (child.isFile()) {
        const data = await readFile(absolutePath);
        entries.push({
          path: relativePath,
          size: data.byteLength,
          hash: createHash('sha256').update(data).digest('hex'),
        });
      }
    }
  }

  await walk(root);
  return entries;
}

export async function stageSysroot({ source, target, receipt }) {
  try {
    const stats = await lstat(source);
    if (!stats.isDirectory()) {
      throw new Error(`working sysroot is not a directory: ${source}`);
    }
  } catch (error) {
    if (error instanceof Error && error.message.startsWith('working sysroot')) {
      throw error;
    }
    throw new Error(`working sysroot does not exist: ${source}`, { cause: error });
  }

  const sourceSnapshot = await collectSnapshot(source);
  if (sourceSnapshot.length === 0) {
    throw new Error(`working sysroot is empty: ${source}`);
  }

  await rm(target, { recursive: true, force: true });
  await mkdir(path.dirname(target), { recursive: true });
  await cp(source, target, { recursive: true, force: true, dereference: false, preserveTimestamps: true });

  const stagedSnapshot = await collectSnapshot(target);
  const fingerprint = createHash('sha256')
    .update(JSON.stringify(stagedSnapshot))
    .digest('hex');
  const result = {
    schemaVersion: 1,
    generatedAt: new Date().toISOString(),
    source: path.resolve(source),
    target: path.resolve(target),
    fileCount: stagedSnapshot.length,
    totalBytes: stagedSnapshot.reduce((total, entry) => total + entry.size, 0),
    fingerprint,
  };

  await mkdir(path.dirname(receipt), { recursive: true });
  await writeFile(receipt, `${JSON.stringify(result, null, 2)}\n`);
  return result;
}

async function main() {
  const root = process.cwd();
  const canonical = toolchainPaths(root);
  const result = await stageSysroot({
    source: process.env.SYSPATH ?? canonical.sysroot,
    target: process.env.STAGED_SYSPATH ?? canonical.stagedSysroot,
    receipt: process.env.SYSROOT_RECEIPT ?? path.join(canonical.receipts, 'sysroot.json'),
  });
  console.log(`[stage-sysroot] ${result.fileCount} files, fingerprint ${result.fingerprint}`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch((error) => {
    console.error('[stage-sysroot] Failed:', error);
    process.exitCode = 1;
  });
}
