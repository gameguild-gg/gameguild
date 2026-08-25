import type { CodingAssignmentContent, FileEncoding } from '@/lib/coding-assignment/types';
import {
  legacyAssessmentToken,
  workspaceStorageKey,
} from '@game-guild/emception-ui/assessment/storage';

/** Workspace seed — shape produced by `publicSeedFiles` and consumed by the
 *  assessment workspace configuration (image seeds carry `encoding: 'base64'`). */
export interface SeedFile {
  path: string;
  content: string;
  encoding: FileEncoding;
}

/** Build the IDE-seedable file list from v1 Data.Files, defensively filtering Public. */
export function publicSeedFiles(assignment: CodingAssignmentContent): Array<{
  path: string;
  content: string;
  encoding: FileEncoding;
  modifiable: boolean;
}> {
  return Object.entries(assignment.Data.Files)
    .filter(([, meta]) => meta.Visibility === 'Public')
    .map(([path, meta]) => ({
      path,
      content: meta.Content,
      encoding: meta.Encoding ?? 'text',
      modifiable: meta.Modifiable,
    }));
}

/**
 * Draft probe for the PRE-MOUNT window: same key pair + parse gate as
 * the vanilla workspace persistence contract, but callable before the lazy
 * assessment editor exists. Its post-mount persistence effect writes initial
 * state, making every probe trivially true (learning #8) — hence this design.
 */
export function hasRestorableDraft(token: string, presetId: string): boolean {
  if (typeof window === 'undefined') return false;
  for (const key of [
    workspaceStorageKey(token, presetId),
    workspaceStorageKey(legacyAssessmentToken(token), presetId),
  ]) {
    const raw = window.localStorage.getItem(key);
    if (raw == null) continue;
    try {
      JSON.parse(raw);
      return true;
    } catch {
      /* corrupt entry under this key — try the next */
    }
  }
  return false;
}

export type SeedLoadMode = 'draft' | 'submission' | 'seed';

/**
 * Pure load-order resolver for the student coding activity.
 * - draft: a parseable localStorage draft exists — the IDE restores itself,
 *   so `files` is empty and the page must NOT call `setFiles`.
 * - submission: union overlay of instructor seed + prior submission
 *   (per-path replace/add; deletions are unrepresentable in the wire shape).
 * - seed: instructor seed only.
 */
export function resolveSeed(input: {
  draftExists: boolean;
  submissionFiles: SeedFile[] | null;
  seedFiles: SeedFile[];
}): { mode: SeedLoadMode; files: SeedFile[] } {
  if (input.draftExists) {
    return { mode: 'draft', files: [] };
  }
  if (input.submissionFiles == null || input.submissionFiles.length === 0) {
    return { mode: 'seed', files: input.seedFiles };
  }
  const byPath = new Map(input.seedFiles.map((file) => [file.path, file]));
  for (const file of input.submissionFiles) {
    byPath.set(file.path, file);
  }
  return { mode: 'submission', files: [...byPath.values()] };
}
