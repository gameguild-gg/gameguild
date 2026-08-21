/** Assemble the canonical CDN tree and schema-v2 artifact manifest. */

import fs from 'node:fs';
import path from 'node:path';
import { pythonMajorMinor, pythonMajorMinorCompact } from './lib/detect-versions.ts';
import { PATCH_SET_VERSION } from './lib/glue-patches.mjs';
import { enableBuildKeepalive } from './lib/keepalive.ts';
import { PINNED } from './lib/pinned-versions.ts';
import { generateReleaseManifest } from './lib/release-manifest.mjs';

enableBuildKeepalive('generate-manifest');

const ROOT = process.cwd();
const packageJson: unknown = JSON.parse(fs.readFileSync(path.join(ROOT, 'package.json'), 'utf8'));
if (!packageJson || typeof packageJson !== 'object' || !('version' in packageJson) || typeof packageJson.version !== 'string') {
  throw new Error('package.json must contain a string version');
}
const python = process.env.PYTHON_VERSION ?? PINNED.PYTHON_VERSION;
const toolVersions = {
  emsdk: process.env.EMSDK_VERSION ?? PINNED.EMSDK_VERSION,
  llvm: process.env.LLVM_VERSION ?? PINNED.LLVM_VERSION,
  binaryen: process.env.BINARYEN_VERSION ?? PINNED.BINARYEN_VERSION,
  python,
  pythonMajorMinor: pythonMajorMinor(python),
  pythonMajorMinorCompact: pythonMajorMinorCompact(python),
  cmake: process.env.CMAKE_VERSION ?? PINNED.CMAKE_VERSION,
  brotli: process.env.BROTLI_VERSION ?? PINNED.BROTLI_VERSION,
  imgui: process.env.IMGUI_VERSION ?? PINNED.IMGUI_VERSION,
  raylib: process.env.RAYLIB_VERSION ?? PINNED.RAYLIB_VERSION,
  raygui: process.env.RAYGUI_VERSION ?? PINNED.RAYGUI_VERSION,
  physac: process.env.PHYSAC_VERSION ?? PINNED.PHYSAC_VERSION,
  allegro: process.env.ALLEGRO_VERSION ?? PINNED.ALLEGRO_VERSION,
  curlLite: process.env.CURL_LITE_VERSION ?? PINNED.CURL_LITE_VERSION,
};

generateReleaseManifest({
  sysroot: process.env.STAGED_SYSPATH ?? path.join(ROOT, 'build', 'stage', 'sysroot'),
  outputDir: process.env.OUTPUT_DIR ?? path.join(ROOT, 'build', 'cdn'),
  manifestFile: process.env.MANIFEST_FILE ?? path.join(ROOT, 'build', 'cdn', 'manifest.json'),
  baseUrl: process.env.CDN_BASE_URL ?? '/cdn',
  artifactVersion: process.env.ARTIFACT_VERSION ?? packageJson.version,
  runtimeAbi: process.env.RUNTIME_ABI ?? 'emception-browser-v1',
  patchSetVersion: PATCH_SET_VERSION,
  toolVersions,
}).then((manifest) => {
  console.log(
    `Manifest v${manifest.schemaVersion}: ${Object.keys(manifest.files).length} files, ` +
      `${Object.keys(manifest.profiles).length} wasm profiles, fingerprint ${manifest.buildFingerprint}.`,
  );
}).catch((error: unknown) => {
  console.error('[generate-manifest] Failed:', error);
  process.exitCode = 1;
});
