import { describe, expect, it } from 'vitest';
import {
  findBlock,
  hasBlockType,
  insertBlock,
  isNumericBlockId,
  moveBlock,
  nextBlockId,
  removeBlock,
  updateBlock,
  type Block,
  type TypedBlockList,
} from './index';

interface TestBlockDataMap {
  text: { value: string };
  quiz: { question: string };
}

type TestBlockList = TypedBlockList<TestBlockDataMap>;

describe('block-list operations', () => {
  const blocks: TestBlockList = [
    { id: '1', type: 'text', data: { value: 'A' } },
    { id: '2', type: 'quiz', data: { question: 'Q' } },
  ];

  it('generates the next numeric block id without recycling ids', () => {
    expect(nextBlockId(blocks)).toBe('3');
    expect(nextBlockId([...blocks, { id: '10', type: 'text', data: { value: 'B' } }])).toBe('11');
    expect(nextBlockId([{ id: 'draft', type: 'text', data: { value: 'A' } }])).toBe('1');
  });

  it('detects numeric block ids', () => {
    expect(isNumericBlockId('1')).toBe(true);
    expect(isNumericBlockId('0')).toBe(true);
    expect(isNumericBlockId('1.5')).toBe(false);
    expect(isNumericBlockId('draft')).toBe(false);
  });

  it('inserts blocks with bounded indexes', () => {
    const inserted = insertBlock(blocks, 1, { id: '3', type: 'text', data: { value: 'B' } });
    expect(inserted.map((block) => block.id)).toEqual(['1', '3', '2']);

    expect(insertBlock(blocks, -10, { id: '4', type: 'text', data: { value: 'C' } })[0]?.id).toBe('4');
    expect(insertBlock(blocks, 99, { id: '5', type: 'text', data: { value: 'D' } }).at(-1)?.id).toBe('5');
  });

  it('updates block data by id', () => {
    const updated = updateBlock(blocks, '1', { value: 'Updated' });
    expect(updated[0]).toEqual({ id: '1', type: 'text', data: { value: 'Updated' } });
    expect(updated[1]).toBe(blocks[1]);
  });

  it('removes, moves, finds, and checks block types', () => {
    expect(removeBlock(blocks, '1').map((block) => block.id)).toEqual(['2']);
    expect(moveBlock(blocks, 0, 1).map((block) => block.id)).toEqual(['2', '1']);
    expect(moveBlock(blocks, 99, 0)).toEqual(blocks);
    expect(findBlock(blocks, '2')).toEqual(blocks[1]);
    expect(findBlock(blocks, 'missing')).toBeNull();
    expect(hasBlockType(blocks, 'quiz')).toBe(true);
    expect(hasBlockType(blocks, 'text')).toBe(true);
  });

  it('keeps generic block usage available', () => {
    const genericBlocks: Block[] = [{ id: '1', type: 'custom', data: { any: true } }];
    expect(nextBlockId(genericBlocks)).toBe('2');
  });
});
