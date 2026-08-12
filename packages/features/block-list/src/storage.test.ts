import { describe, expect, it } from 'vitest';
import {
  blocksToStorage,
  deserializeBlockList,
  isBlock,
  isBlockStorage,
  normalizeBlockStorage,
  serializeBlockList,
  storageToBlocks,
  type TypedBlockList,
} from './index';

interface TestBlockDataMap {
  text: { value: string };
  image: { src: string };
}

type TestBlocks = TypedBlockList<TestBlockDataMap>;

describe('block-list storage', () => {
  const blocks: TestBlocks = [
    { id: '1', type: 'text', data: { value: 'Hello' } },
    { id: '2', type: 'image', data: { src: '/image.png' } },
  ];

  it('roundtrips block lists through storage', () => {
    const storage = blocksToStorage(blocks);

    expect(storage).toEqual({
      order: [
        ['1', 'text'],
        ['2', 'image'],
      ],
      blocks: {
        '1': { value: 'Hello' },
        '2': { src: '/image.png' },
      },
    });
    expect(storageToBlocks(storage)).toEqual(blocks);
  });

  it('serializes and deserializes block lists', () => {
    expect(deserializeBlockList(serializeBlockList(blocks))).toEqual(blocks);
    expect(deserializeBlockList('not-json')).toEqual([]);
    expect(deserializeBlockList(null)).toEqual([]);
  });

  it('normalizes unknown storage without knowing concrete block types', () => {
    const normalized = normalizeBlockStorage({
      order: [
        ['1', 'text'],
        ['missing', 'text'],
        [1, 'text'],
        ['2', 'image'],
      ],
      blocks: {
        '1': { value: 'Hello' },
        '2': { src: '/image.png' },
        orphan: { value: 'Skipped' },
      },
    });

    expect(normalized).toEqual({
      order: [
        ['1', 'text'],
        ['2', 'image'],
      ],
      blocks: {
        '1': { value: 'Hello' },
        '2': { src: '/image.png' },
      },
    });
  });

  it('guards blocks and storage-like values', () => {
    expect(isBlock(blocks[0])).toBe(true);
    expect(isBlock({ id: '1', type: 'text' })).toBe(false);
    expect(isBlockStorage(blocksToStorage(blocks))).toBe(true);
    expect(isBlockStorage({ order: [], blocks: [] })).toBe(false);
  });

});
