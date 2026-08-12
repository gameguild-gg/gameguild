import { describe, expect, it } from 'vitest';
import {
  blockToPreviewNode,
  blocksToStorage,
  deserializeProject,
  serializeProject,
  storageToBlocks,
} from './block-storage';
import type { BlockArray } from './block-structure';

describe('block storage compatibility adapters', () => {
  const blocks = [
    { id: '1', type: 'quiz', data: { title: 'Question' } },
    { id: '2', type: 'markdown', data: { content: 'Hello' } },
  ] as BlockArray;

  it('preserves the existing project storage shape', () => {
    expect(blocksToStorage(blocks)).toEqual({
      order: [
        ['1', 'quiz'],
        ['2', 'markdown'],
      ],
      blocks: {
        '1': { title: 'Question' },
        '2': { content: 'Hello' },
      },
    });

    expect(storageToBlocks(blocksToStorage(blocks))).toEqual(blocks);
    expect(deserializeProject(serializeProject(blocks))).toEqual(blocks);
  });

  it('normalizes invalid persisted project data', () => {
    expect(deserializeProject('not-json')).toEqual([]);
    expect(deserializeProject('{"order":[["1","quiz"]],"blocks":{}}')).toEqual([]);
  });

  it('uses the canonical data payload for quiz previews', () => {
    expect(blockToPreviewNode(blocks[0]!)).toEqual({
      id: '1',
      type: 'quiz',
      data: { title: 'Question' },
      version: 1,
    });
  });
});