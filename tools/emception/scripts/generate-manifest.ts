/** Assemble the canonical CDN tree and schema-v2 artifact manifest. */

import { createHash } from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { toolchainPaths } from './toolchain/paths.ts';
import { PATCH_SET_VERSION } from './lib/glue-patches.mjs';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { loadToolchainStateSync, lockedVersion, pythonMajorMinor, pythonMajorMinorCompact } from './toolchain/config.ts';
import { serializeToolchainLock } from './toolchain/lock.ts';
import { generateReleaseManifest } from './lib/release-manifest.mjs';

enableBuildKeepalive('generate-manifest');

const ROOT = process.cwd();
const P = toolchainPaths(ROOT);
const packageJson: unknown = JSON.parse(fs.readFileSync(path.join(ROOT, 'package.json'), 'utf8'));
if (!packageJson || typeof packageJson !== 'object' || !('version' in packageJson) || typeof packageJson.version !== 'string') {
  throw new Error('package.json must contain a string version');
}
const { lock } = loadToolchainStateSync(ROOT);
const sha256 = (value: string | Buffer) => createHash('sha256').update(value).digest('hex');
const glueReceipt = path.join(P.receipts, 'glue.json');
if (!fs.existsSync(glueReceipt)) {
  throw new Error(`Verified glue receipt is required before manifest generation: ${glueReceipt}`);
}
const toolchainLockHash = sha256(serializeToolchainLock(lock));
const buildReceiptHash = sha256(fs.readFileSync(glueReceipt));
const sourceProvenance = Object.fromEntries(Object.entries(lock.tools).map(([name, tool]) => {
  const source = tool.source;
  const sha = source.kind === 'archive' || source.kind === 'git-archive'
    ? source.sha256
    : source.contentHash;
  const revision = source.kind === 'git-archive'
    ? source.commit
    : source.kind === 'emsdk-component'
      ? source.revision
      : undefined;
  return [name, { version: tool.version, ...(revision ? { revision } : {}), sha256: sha }];
}));
const python = lockedVersion(lock, 'python');
const toolVersions = {
  emsdk: lockedVersion(lock, 'emsdk'),
  llvm: lockedVersion(lock, 'llvm'),
  binaryen: lockedVersion(lock, 'binaryen'),
  python,
  pythonMajorMinor: pythonMajorMinor(python),
  pythonMajorMinorCompact: pythonMajorMinorCompact(python),
  cmake: lockedVersion(lock, 'cmake'),
  brotli: lockedVersion(lock, 'brotli'),
  imgui: lockedVersion(lock, 'imgui'),
  raylib: lockedVersion(lock, 'raylib'),
  raygui: lockedVersion(lock, 'raygui'),
  physac: lockedVersion(lock, 'physac'),
  allegro: lockedVersion(lock, 'allegro'),
  curlLite: lockedVersion(lock, 'curlLite'),
};

generateReleaseManifest({
  sysroot: process.env.STAGED_SYSPATH ?? P.stagedSysroot,
  outputDir: process.env.OUTPUT_DIR ?? P.releaseCdn,
  manifestFile: process.env.MANIFEST_FILE ?? P.manifestFile,
  baseUrl: process.env.CDN_BASE_URL ?? '/cdn',
  artifactVersion: process.env.ARTIFACT_VERSION ?? packageJson.version,
  runtimeAbi: process.env.RUNTIME_ABI ?? 'emception-browser-v1',
  patchSetVersion: PATCH_SET_VERSION,
  toolVersions,
  toolchainLockHash,
  buildReceiptHash,
  sourceProvenance,
  sourceDateEpoch: Number.parseInt(process.env.SOURCE_DATE_EPOCH ?? '0', 10),
}).then((manifest) => {
  console.log(
    `Manifest v${manifest.schemaVersion}: ${Object.keys(manifest.files).length} files, ` +
      `${Object.keys(manifest.profiles).length} wasm profiles, fingerprint ${manifest.buildFingerprint}.`,
  );
}).catch((error: unknown) => {
  console.error('[generate-manifest] Failed:', error);
  process.exitCode = 1;
});
