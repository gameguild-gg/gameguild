// @emception/browser — Web Worker + IDB + OffscreenCanvas adapter.
// Phase 0 skeleton. Real createEmception() lands in Phase 0.2 (migrated from tools/emception/src).

export type { EmceptionAPI, RunOptions, ToolResult, WorkspaceOptions } from '@emception/core';

export const DEFAULT_MANIFEST_URL =
  'https://cdn.jsdelivr.net/npm/@emception/sysroot@0.20.0/manifest.json';

export async function createEmception(_opts?: unknown): Promise<never> {
  throw new Error(
    '@emception/browser: createEmception() not yet implemented. ' +
      'Phase 0.2 will migrate the working implementation from tools/emception/src/.'
  );
}
