import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { stageCdnPackage } from './lib/stage-cdn-package.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

stageCdnPackage({
  sourceCdn: path.join(root, 'artifacts', 'toolchain', 'release', 'cdn'),
  targetCdn: path.join(root, 'packages', 'toolchain', 'cdn'),
}).then((result) => {
  console.log(`[stage-toolchain-cdn] ${result.bundleCount} bundles, ${result.totalBytes} bytes`);
}).catch((error) => {
  console.error('[stage-toolchain-cdn] Failed:', error);
  process.exitCode = 1;
});
