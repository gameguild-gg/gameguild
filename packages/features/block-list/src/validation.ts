import type { Block, BlockStorage } from './types';

type UnknownRecord = Record<string, unknown>;

export function isBlock(value: unknown): value is Block {
  const record = asRecord(value);
  return Boolean(
    record &&
      typeof record.id === 'string' &&
      typeof record.type === 'string' &&
      Object.prototype.hasOwnProperty.call(record, 'data'),
  );
}

export function isBlockStorage(value: unknown): value is BlockStorage {
  const record = asRecord(value);
  return Boolean(
    record &&
      Array.isArray(record.order) &&
      asRecord(record.blocks),
  );
}

export function normalizeBlockStorage(value: unknown): BlockStorage {
  const storage = isBlockStorage(value) ? value : null;
  if (!storage) return { order: [], blocks: {} };

  const blocks = asRecord(storage.blocks) ?? {};
  const normalizedOrder: BlockStorage['order'] = [];
  const normalizedBlocks: BlockStorage['blocks'] = {};

  for (const entry of storage.order) {
    // Keep only complete order entries whose payload exists. Payloads not
    // referenced by order are intentionally dropped, preventing orphaned data
    // from becoming visible after a storage roundtrip.
    if (!Array.isArray(entry) || entry.length < 2) continue;
    const [rawId, rawType] = entry;
    if (typeof rawId !== 'string' || typeof rawType !== 'string') continue;
    if (!Object.prototype.hasOwnProperty.call(blocks, rawId)) continue;

    normalizedOrder.push([rawId, rawType]);
    normalizedBlocks[rawId] = blocks[rawId];
  }

  return {
    order: normalizedOrder,
    blocks: normalizedBlocks,
  };
}

function asRecord(value: unknown): UnknownRecord | null {
  // Arrays are objects in JavaScript but are not valid key-value containers for
  // either the storage envelope or its payload map.
  if (!value || typeof value !== 'object' || Array.isArray(value)) return null;
  return value as UnknownRecord;
}
