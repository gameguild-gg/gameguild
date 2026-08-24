import type { WorkspaceAPI } from '../types.js';

/** A temporary file written to a workspace for one scoped operation. */
export interface WorkspaceOverlayFile {
  readonly path: string;
  readonly content: Uint8Array | string;
}

/** The minimum workspace surface required for a transactional overlay. */
export type WorkspaceOverlayTarget = Pick<WorkspaceAPI, 'readFile' | 'writeFile' | 'deleteFile'>;

interface WorkspaceOverlaySnapshot {
  readonly file: WorkspaceOverlayFile;
  readonly original: Uint8Array | null;
}

/**
 * Runs an operation with temporary workspace files and restores the exact
 * previous contents afterwards. Files that did not exist before the operation
 * are removed during cleanup.
 */
export async function withWorkspaceOverlay<T>(
  workspace: WorkspaceOverlayTarget,
  files: readonly WorkspaceOverlayFile[],
  operation: () => Promise<T> | T,
): Promise<T> {
  assertUniquePaths(files);

  const snapshots = await Promise.all(files.map(async (file): Promise<WorkspaceOverlaySnapshot> => ({
    file,
    original: cloneBytes(await workspace.readFile(file.path)),
  })));
  const applied: WorkspaceOverlaySnapshot[] = [];
  let result!: T;
  let operationError: unknown;
  let hasOperationError = false;

  try {
    for (const snapshot of snapshots) {
      await workspace.writeFile(snapshot.file.path, snapshot.file.content);
      applied.push(snapshot);
    }
    result = await operation();
  } catch (error) {
    operationError = error;
    hasOperationError = true;
  }

  const cleanupErrors: unknown[] = [];
  for (const snapshot of [...applied].reverse()) {
    try {
      if (snapshot.original === null) {
        if (await workspace.readFile(snapshot.file.path) !== null) {
          await workspace.deleteFile(snapshot.file.path);
        }
      } else {
        await workspace.writeFile(snapshot.file.path, snapshot.original);
      }
    } catch (error) {
      cleanupErrors.push(error);
    }
  }

  if (hasOperationError && cleanupErrors.length > 0) {
    throw new AggregateError([operationError, ...cleanupErrors], 'Workspace overlay operation and cleanup failed');
  }
  if (hasOperationError) throw operationError;
  if (cleanupErrors.length > 0) {
    throw new AggregateError(cleanupErrors, 'Workspace overlay cleanup failed');
  }

  return result;
}

function assertUniquePaths(files: readonly WorkspaceOverlayFile[]): void {
  const paths = new Set<string>();
  for (const file of files) {
    if (!file.path) throw new Error('Workspace overlay file path must not be empty');
    if (paths.has(file.path)) throw new Error(`Workspace overlay contains duplicate path: ${file.path}`);
    paths.add(file.path);
  }
}

function cloneBytes(bytes: Uint8Array | null): Uint8Array | null {
  return bytes === null ? null : new Uint8Array(bytes);
}
