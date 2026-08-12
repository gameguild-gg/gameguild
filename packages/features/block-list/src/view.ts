import type { Block, BlockView } from './types';

export interface BlockViewOptions {
  version?: number;
}

export function blockToView<TBlock extends Block>(
  block: TBlock,
  options: BlockViewOptions = {},
): BlockView<TBlock['type'], TBlock['data']> {
  // Do not branch on block type here. Consumers can render a single canonical
  // shape and retain their domain-specific narrowing through `type`.
  return {
    id: block.id,
    type: block.type,
    data: block.data,
    version: options.version ?? 1,
  };
}

export function blocksToViews<TBlock extends Block>(
  blocks: readonly TBlock[],
  options: BlockViewOptions = {},
): Array<BlockView<TBlock['type'], TBlock['data']>> {
  return blocks.map((block) => blockToView(block, options));
}
