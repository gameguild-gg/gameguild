export function isNumericBlockId(value: unknown): value is string {
  if (typeof value !== 'string' || value.trim() === '') return false;
  const numeric = Number(value);
  return Number.isFinite(numeric) && Number.isInteger(numeric) && numeric >= 0;
}

export function nextBlockId<TBlock extends { id: string }>(
  blocks: readonly TBlock[],
): string {
  // Use the greatest existing ID rather than the list length. This prevents
  // IDs from being reused after deletion, which keeps persisted references
  // stable for consumers that retain them.
  let max = 0;
  for (const block of blocks) {
    if (!isNumericBlockId(block.id)) continue;
    const numericId = Number(block.id);
    if (numericId > max) max = numericId;
  }
  return String(max + 1);
}
