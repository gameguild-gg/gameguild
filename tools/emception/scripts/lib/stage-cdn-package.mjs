import { cp, mkdir, readFile, readdir, rm, stat } from 'node:fs/promises';
import path from 'node:path';

const REQUIRED_FILES = ['manifest.json', 'brotli_wasm.js', 'brotli_wasm.wasm'];

function isPublishable(fileName) {
  return REQUIRED_FILES.includes(fileName) || fileName.endsWith('.tar.br');
}

async function exists(filePath) {
  try {
    await stat(filePath);
    return true;
  } catch {
    return false;
  }
}

async function prune(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  for (const entry of entries) {
    const absolutePath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      await prune(absolutePath);
      if ((await readdir(absolutePath)).length === 0) await rm(absolutePath, { recursive: true, force: true });
    } else if (!isPublishable(entry.name)) {
      await rm(absolutePath, { force: true });
    }
  }
}

async function summarize(directory) {
  let bundleCount = 0;
  let totalBytes = 0;
  async function walk(current) {
    for (const entry of await readdir(current, { withFileTypes: true })) {
      const absolutePath = path.join(current, entry.name);
      if (entry.isDirectory()) await walk(absolutePath);
      else {
        if (entry.name.endsWith('.tar.br')) bundleCount += 1;
        totalBytes += (await stat(absolutePath)).size;
      }
    }
  }
  await walk(directory);
  return { bundleCount, totalBytes };
}

export async function stageCdnPackage({ sourceCdn, targetCdn }) {
  if (path.resolve(sourceCdn) === path.resolve(targetCdn)) {
    throw new Error('CDN package source and target must differ');
  }
  for (const requiredFile of REQUIRED_FILES) {
    if (!(await exists(path.join(sourceCdn, requiredFile)))) {
      throw new Error(`canonical CDN release is missing ${requiredFile}: ${sourceCdn}`);
    }
  }
  const manifest = JSON.parse(await readFile(path.join(sourceCdn, 'manifest.json'), 'utf8'));
  if (manifest.schemaVersion !== 2) {
    throw new Error(`canonical CDN manifest must use schemaVersion 2: ${sourceCdn}`);
  }

  await rm(targetCdn, { recursive: true, force: true });
  await mkdir(path.dirname(targetCdn), { recursive: true });
  await cp(sourceCdn, targetCdn, { recursive: true, force: true, dereference: true });
  await prune(targetCdn);
  return summarize(targetCdn);
}
