import type { Block } from './types';

// Updaters receive the whole discriminated block so callers can derive a new
// payload from its current value without mutating the existing list.
type BlockDataUpdater<TBlock extends Block> =
  | TBlock['data']
  | ((block: TBlock) => TBlock['data']);

export function insertBlock<TBlock extends Block>(
  blocks: readonly TBlock[],
  index: number,
  block: TBlock,
): TBlock[] {
  // All operations return a new array. Unchanged block objects retain identity
  // so UI consumers can efficiently detect the smallest possible change.
  const next = [...blocks];
  next.splice(clampInsertIndex(index, next.length), 0, block);
  return next;
}

export function updateBlock<TBlock extends Block>(
  blocks: readonly TBlock[],
  id: string,
  dataOrUpdater: BlockDataUpdater<TBlock>,
): TBlock[] {
  return blocks.map((block) => {
    if (block.id !== id) return block;
    const data = typeof dataOrUpdater === 'function'
      ? (dataOrUpdater as (block: TBlock) => TBlock['data'])(block)
      : dataOrUpdater;
    return { ...block, data };
  });
}

export function removeBlock<TBlock extends Block>(
  blocks: readonly TBlock[],
  id: string,
): TBlock[] {
  return blocks.filter((block) => block.id !== id);
}

export function moveBlock<TBlock extends Block>(
  blocks: readonly TBlock[],
  fromIndex: number,
  toIndex: number,
): TBlock[] {
  if (!Number.isInteger(fromIndex) || fromIndex < 0 || fromIndex >= blocks.length) {
    return [...blocks];
  }

  const next = [...blocks];
  const [moved] = next.splice(fromIndex, 1);
  if (!moved) return next;

  const targetIndex = clampInsertIndex(toIndex, next.length);
  next.splice(targetIndex, 0, moved);
  return next;
}

export function findBlock<TBlock extends Block>(
  blocks: readonly TBlock[],
  id: string,
): TBlock | null {
  return blocks.find((block) => block.id === id) ?? null;
}

export function hasBlockType<TBlock extends Block>(
  blocks: readonly TBlock[],
  type: TBlock['type'],
): boolean {
  return blocks.some((block) => block.type === type);
}

function clampInsertIndex(index: number, length: number): number {
  // Treat invalid destinations as an append and bound all valid values. This
  // makes drag-and-drop and programmatic callers share predictable semantics.
  if (!Number.isFinite(index)) return length;
  return Math.min(Math.max(Math.trunc(index), 0), length);
}
