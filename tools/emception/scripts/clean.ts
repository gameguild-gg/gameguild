/** @deprecated Use `pnpm toolchain clean <artifacts|cache|all>`. */

import { cleanToolchain } from './toolchain/clean.ts';

await cleanToolchain(process.cwd(), 'all');
console.log('[clean] Removed generated Toolchain cache and artifacts.');
