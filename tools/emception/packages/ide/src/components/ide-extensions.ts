import type { IdeController, IdeExtension } from './ide-types.js';

/** Validate extension identities before any lifecycle hook can run. */
export function validateIdeExtensions(extensions: readonly IdeExtension[] | undefined): readonly IdeExtension[] {
  const values = extensions ?? [];
  const ids = new Set<string>();
  for (const extension of values) {
    if (!extension.id) throw new Error('IDE extension id must not be empty');
    if (ids.has(extension.id)) throw new Error(`Duplicate IDE extension id: ${extension.id}`);
    ids.add(extension.id);
  }
  return values;
}

/** Activate extensions in declaration order and dispose them in reverse order. */
export function activateIdeExtensions(extensions: readonly IdeExtension[], controller: IdeController): () => void {
  const cleanups: Array<() => void> = [];
  for (const extension of extensions) {
    const cleanup = extension.onReady?.(controller);
    if (typeof cleanup === 'function') cleanups.push(cleanup);
  }

  return () => {
    const errors: unknown[] = [];
    for (const cleanup of [...cleanups].reverse()) {
      try {
        cleanup();
      } catch (error) {
        errors.push(error);
      }
    }
    if (errors.length > 0) throw new AggregateError(errors, 'IDE extension cleanup failed');
  };
}
