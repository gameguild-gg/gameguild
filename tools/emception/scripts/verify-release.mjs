import { fileURLToPath } from 'node:url';
import path from 'node:path';

import { verifyEmceptionRelease } from './lib/verify-release.mjs';

export async function main(root = process.cwd()) {
  const result = await verifyEmceptionRelease(root);
  console.log(`[verify-release] ${result.packageCount} packages and Toolchain ${result.version} verified.`);
  return result;
}

if (path.resolve(process.argv[1] ?? '') === fileURLToPath(import.meta.url)) {
  main().catch((error) => {
    console.error(`[verify-release] ${error instanceof Error ? error.message : String(error)}`);
    process.exitCode = 1;
  });
}
