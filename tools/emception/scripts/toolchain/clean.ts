import { rm } from 'node:fs/promises';

import { toolchainPaths } from './paths.ts';

export type CleanScope = 'artifacts' | 'cache' | 'all';

const CLEAN_SCOPES = new Set<CleanScope>(['artifacts', 'cache', 'all']);

/**
 * Remove only generated Toolchain state.
 *
 * Dependencies, external checkouts outside the Toolchain cache and every
 * versioned overlay are deliberately outside these roots.
 */
export async function cleanToolchain(root: string, scope: CleanScope): Promise<void> {
  if (!CLEAN_SCOPES.has(scope)) {
    throw new Error(`Unknown clean scope: ${scope}`);
  }

  const paths = toolchainPaths(root);

  if (scope === 'cache' || scope === 'all') {
    await rm(paths.cache, { recursive: true, force: true });
  }

  if (scope === 'artifacts' || scope === 'all') {
    await Promise.all([
      rm(paths.artifacts, { recursive: true, force: true }),
      rm(paths.packageCdn, { recursive: true, force: true }),
      rm(paths.compatibilityCdn, { recursive: true, force: true }),
      rm(paths.publicCdn, { recursive: true, force: true }),
    ]);
  }
}
