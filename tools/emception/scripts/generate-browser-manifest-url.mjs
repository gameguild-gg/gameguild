import { mkdir, readFile, rename, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

async function packageVersion(filename) {
  const value = JSON.parse(await readFile(filename, 'utf8'));
  if (!value || typeof value.version !== 'string' || value.version.length === 0) {
    throw new Error(`${filename} must contain a version`);
  }
  return value.version;
}

export async function generateBrowserManifestUrl(root = process.cwd()) {
  const browserRoot = path.join(root, 'packages', 'browser');
  const browserVersion = await packageVersion(path.join(browserRoot, 'package.json'));
  const toolchainVersion = await packageVersion(path.join(root, 'packages', 'toolchain', 'package.json'));
  if (browserVersion !== toolchainVersion) {
    throw new Error(`Browser ${browserVersion} does not match Toolchain ${toolchainVersion}`);
  }

  const output = path.join(browserRoot, 'src', 'generated', 'toolchain-manifest-url.ts');
  const temporary = `${output}.tmp-${process.pid}`;
  const url = `https://cdn.jsdelivr.net/npm/@gameguild/emception-toolchain@${toolchainVersion}/cdn/manifest.json`;
  const source = [
    '/** Generated from package versions by scripts/generate-browser-manifest-url.mjs. */',
    `export const DEFAULT_MANIFEST_URL = ${JSON.stringify(url)};`,
    '',
  ].join('\n');
  await mkdir(path.dirname(output), { recursive: true });
  await writeFile(temporary, source, { encoding: 'utf8', flag: 'wx' });
  try {
    await rename(temporary, output);
  } catch (error) {
    await rm(temporary, { force: true });
    throw error;
  }
  return { output, url, version: toolchainVersion };
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const workspaceRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
  generateBrowserManifestUrl(workspaceRoot).then(({ url }) => console.log(`[browser] ${url}`)).catch((error) => {
    console.error(error instanceof Error ? error.message : String(error));
    process.exitCode = 1;
  });
}
