import { parseCodingSubmission, normalizeCodingPath } from '@game-guild/coding';
import type { WorkspaceConfig, WorkspaceFile } from '@gameguild/emception-ide';
import type { CodingSurfaceDefinition, CodingSurfaceMode } from './types';

/** Author-only buffers may be edited in memory, never persisted as learner drafts. */
export function createCodingEditorFiles(definition: CodingSurfaceDefinition, config: WorkspaceConfig, mode: CodingSurfaceMode): Record<string, WorkspaceFile> {
  const files: Record<string, WorkspaceFile> = {};
  for (const file of definition.files) {
    if ('visibility' in file && file.visibility === 'private' && mode !== 'author') continue;
    const path = normalizeCodingPath(file.path);
    const configured = config.files[path];
    files[path] = {
      path,
      type: file.encoding === 'base64' ? 'image' : 'text',
      content: file.mutable && configured ? configured.content : file.content,
      readonly: !file.mutable,
    };
  }
  return files;
}

export function restoreCodingEditorFiles(
  initial: Readonly<Record<string, WorkspaceFile>>,
  definition: CodingSurfaceDefinition,
  value: unknown,
): Record<string, WorkspaceFile> {
  const draft = parseCodingSubmission(value);
  const restored = { ...initial };
  const byPath = new Map(definition.files.map((file) => [file.path, file]));
  for (const file of draft.files) {
    const authored = byPath.get(file.path);
    if (authored) {
      if (!authored.mutable || authored.id !== file.fileId || !initial[file.path] || ('visibility' in authored && authored.visibility === 'private')) {
        throw new Error('Local coding draft attempts to replace a protected file.');
      }
    } else if (!definition.allowLearnerCreatedFiles) throw new Error('Additional files are not allowed in this coding draft.');
    if (file.encoding !== 'text') throw new Error('Local coding drafts support text changes only.');
    restored[file.path] = { path: file.path, type: 'text', content: file.content, readonly: false };
  }
  if (Object.keys(restored).length > definition.limits.maxFileCount) throw new Error('Local coding draft exceeds the file limit.');
  return restored;
}
