import type { Block, BlockList, BlockStorage } from './types';
import { normalizeBlockStorage } from './validation';

export const EMPTY_BLOCK_STORAGE: BlockStorage = {
  order: [],
  blocks: {},
};

export const EMPTY_BLOCK_LIST_DATA = JSON.stringify(EMPTY_BLOCK_STORAGE);

export function blocksToStorage<TBlock extends Block>(
  blocks: readonly TBlock[],
): BlockStorage<TBlock['type'], TBlock['data']> {
  // The order tuple carries identity and type; the map carries only payloads.
  // This is the persisted shape and must remain generic for every block type.
  const order: BlockStorage<TBlock['type'], TBlock['data']>['order'] = [];
  const map: Record<string, TBlock['data']> = {};

  for (const block of blocks) {
    order.push([block.id, block.type]);
    map[block.id] = block.data;
  }

  return { order, blocks: map };
}

export function storageToBlocks<
  TType extends string = string,
  TData = unknown,
  TBlock extends Block<TType, TData> = Block<TType, TData>,
>(
  storage: BlockStorage<TType, TData> | null | undefined,
): BlockList<TBlock> {
  // Storage can originate from a database or JSON, so normalize it before
  // reconstructing runtime blocks. Invalid and orphaned entries are ignored.
  const normalized = normalizeBlockStorage(storage) as BlockStorage<TType, TData>;
  const blocks: BlockList<TBlock> = [];

  for (const [id, type] of normalized.order) {
    const data = normalized.blocks[id];
    if (data === undefined) continue;
    blocks.push({ id, type, data } as TBlock);
  }

  return blocks;
}

export function serializeBlockList<TBlock extends Block>(
  blocks: readonly TBlock[],
): string {
  return JSON.stringify(blocksToStorage(blocks));
}

export function deserializeBlockList<TBlock extends Block = Block>(
  data: string | null | undefined,
): BlockList<TBlock> {
  if (!data) return [];

  try {
    // Deserialization is intentionally fail-closed: malformed persisted data
    // becomes an empty list rather than leaking partial, unvalidated structure.
    const parsed = JSON.parse(data) as unknown;
    return storageToBlocks<string, unknown, TBlock>(normalizeBlockStorage(parsed));
  } catch {
    return [];
  }
}
