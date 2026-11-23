import { codingContractFixture } from '@game-guild/coding/testing';
import { describe, expect, it } from 'vitest';
import { createCodingEditorFiles, restoreCodingEditorFiles } from './editor-files';
import { captureCodingSubmission } from './session';
import { workspaceConfigFromDefinition } from './workspace';

describe('offline editor privacy and recovery', () => {
  it.each(['learner', 'author-test', 'grader'] as const)('does not load private files into the %s editor', (mode) => {
    const definition = structuredClone(codingContractFixture.authoring);
    const files = createCodingEditorFiles(definition, workspaceConfigFromDefinition(definition), mode);
    for (const privateFile of definition.files.filter((file) => file.visibility === 'private')) {
      expect(files[privateFile.path]).toBeUndefined();
    }
  });

  it('keeps private author files for saving but excludes them from draft deltas', () => {
    const definition = codingContractFixture.authoring;
    const files = createCodingEditorFiles(definition, workspaceConfigFromDefinition(definition), 'author');
    const privateFile = definition.files.find((file) => file.visibility === 'private');
    expect(privateFile).toBeDefined();
    expect(Object.values(files).map((file) => file.path)).toContain(privateFile?.path);
    expect(captureCodingSubmission(definition, Object.values(files)).files).toEqual([]);
  });

  it('strictly restores authorized deltas without trusting stored read-only flags', () => {
    const definition = codingContractFixture.learnerDelivery;
    const files = createCodingEditorFiles(definition, workspaceConfigFromDefinition(definition), 'learner');
    const mutable = definition.files.find((file) => file.mutable);
    if (!mutable) throw new Error('Fixture needs an editable starter.');
    const draft = { schemaVersion: 1, files: [{ fileId: mutable.id, path: mutable.path, encoding: 'text', content: 'edited' }] };
    expect(restoreCodingEditorFiles(files, definition, draft)[mutable.path].content).toBe('edited');
    expect(() => restoreCodingEditorFiles(files, definition, { ...draft, unexpected: 'secret' })).toThrow();
  });

  it('rejects protected-file replacement instead of silently discarding a failed recovery', () => {
    const definition = structuredClone(codingContractFixture.learnerDelivery);
    definition.files[0].mutable = false;
    const files = createCodingEditorFiles(definition, workspaceConfigFromDefinition(definition), 'learner');
    expect(() =>
      restoreCodingEditorFiles(files, definition, {
        schemaVersion: 1,
        files: [{ fileId: definition.files[0].id, path: definition.files[0].path, encoding: 'text', content: 'changed' }],
      }),
    ).toThrow();
  });
});
