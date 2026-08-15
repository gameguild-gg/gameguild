import { isAssetUri, type AssetUri } from "./asset-uri";

export function findAssetUris(value: unknown): AssetUri[] {
  const found = new Set<AssetUri>();
  const visited = new WeakSet<object>();

  const visit = (current: unknown) => {
    if (isAssetUri(current)) {
      found.add(current);
      return;
    }
    if (!current || typeof current !== "object") return;
    if (visited.has(current)) return;
    visited.add(current);
    if (Array.isArray(current)) {
      current.forEach(visit);
      return;
    }
    Object.values(current as Record<string, unknown>).forEach(visit);
  };

  visit(value);
  return Array.from(found);
}
