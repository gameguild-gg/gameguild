declare const assetUriBrand: unique symbol;

export type AssetUri = string & { readonly [assetUriBrand]: true };

export interface ParsedAssetUri {
  id: string;
}

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export function isAssetId(value: unknown): value is string {
  return typeof value === "string" && UUID_PATTERN.test(value);
}

export function createAssetUri(id?: string): AssetUri {
  id ??= crypto.randomUUID();
  if (!isAssetId(id)) throw new TypeError(`Invalid asset id: ${id}`);
  return `asset://${id}` as AssetUri;
}

export function parseAssetUri(value: string): ParsedAssetUri | null {
  const match = /^asset:\/\/([^/]+)$/.exec(value);
  return match?.[1] && isAssetId(match[1]) ? { id: match[1] } : null;
}

export function isAssetUri(value: unknown): value is AssetUri {
  return typeof value === "string" && parseAssetUri(value) !== null;
}

export function toAssetUri(value: string): AssetUri {
  if (!isAssetUri(value)) throw new TypeError(`Invalid asset URI: ${value}`);
  return value;
}
