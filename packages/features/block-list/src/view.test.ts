import { describe, expect, it } from 'vitest';
import { blockToView, blocksToViews, type TypedBlockList } from './index';

interface TestBlockDataMap {
  text: { value: string };
  quiz: { question: string };
}

describe('block-list views', () => {
  const blocks: TypedBlockList<TestBlockDataMap> = [
    { id: '1', type: 'text', data: { value: 'Hello' } },
    { id: '2', type: 'quiz', data: { question: 'What is this?' } },
  ];

  it('converts a block to the canonical generic view shape', () => {
    expect(blockToView(blocks[0]!, { version: 3 })).toEqual({
      id: '1',
      type: 'text',
      data: { value: 'Hello' },
      version: 3,
    });
  });

  it('uses the data field for every block type', () => {
    expect(blocksToViews(blocks)).toEqual([
      { id: '1', type: 'text', data: { value: 'Hello' }, version: 1 },
      {
        id: '2',
        type: 'quiz',
        data: { question: 'What is this?' },
        version: 1,
      },
    ]);
  });
});