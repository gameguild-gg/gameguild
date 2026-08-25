import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { stageCdnPackage } from './lib/stage-cdn-package.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

stageCdnPackage({
  sourceCdn: path.join(root, 'artifacts', 'toolchain', 'release', 'cdn'),
  targetCdn: path.join(root, 'packages', 'core', 'cdn'),
}).then((result) => {
  console.log(`[stage-core-cdn] compatibility copy: ${result.bundleCount} bundles, ${result.totalBytes} bytes`);
}).catch((error) => {
  console.error('[stage-core-cdn] Failed:', error);
  process.exitCode = 1;
});
