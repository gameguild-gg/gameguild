export interface SerializedBlockNode<TType extends string = string, TData = unknown> {
  type: TType
  version: number
  data: TData
}
