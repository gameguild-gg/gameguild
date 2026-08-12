export type BlockTypeId = string;

/**
 * Consumers provide the concrete type-to-payload mapping. Keeping the map
 * outside this package prevents domain-specific block knowledge from leaking
 * into the generic list layer.
 */
export type BlockDataByType = object;

export interface Block<TType extends BlockTypeId = BlockTypeId, TData = unknown> {
  id: string;
  type: TType;
  data: TData;
}

export type BlockList<TBlock extends Block = Block> = TBlock[];

export type BlockOrderEntry<TType extends BlockTypeId = BlockTypeId> = readonly [
  id: string,
  type: TType,
];

export interface BlockStorage<
  TType extends BlockTypeId = BlockTypeId,
  TData = unknown,
> {
  // Order and payloads are deliberately stored separately so reordering does
  // not require copying or wrapping every payload in persisted JSON.
  order: BlockOrderEntry<TType>[];
  blocks: Record<string, TData>;
}

/**
 * Builds a discriminated union from a consumer-owned map. Narrowing `type`
 * also narrows `data`, while block-list remains unaware of concrete types.
 */
export type TypedBlock<TMap extends BlockDataByType> = {
  [TType in keyof TMap & string]: Block<TType, TMap[TType]>;
}[keyof TMap & string];

export type TypedBlockList<TMap extends BlockDataByType> =
  TypedBlock<TMap>[];

export type TypedBlockStorage<TMap extends BlockDataByType> = BlockStorage<
  keyof TMap & string,
  TMap[keyof TMap & string]
>;

export interface BlockView<
  TType extends BlockTypeId = BlockTypeId,
  TData = unknown,
> {
  // This is a read model, not a persistence model. `data` is always used for
  // the payload so all renderers consume one stable shape.
  id: string;
  type: TType;
  data: TData;
  version: number;
}

export type TypedBlockView<TMap extends BlockDataByType> = {
  [TType in keyof TMap & string]: BlockView<TType, TMap[TType]>;
}[keyof TMap & string];
