declare const assetUriBrand: unique symbol;

export type AssetUri = string & { readonly [assetUriBrand]: true };

export type ParsedAssetUri =
  | { source: "local"; id: string }
  | { source: "remote"; providerKey: string; id: string };

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const PROVIDER_KEY_PATTERN = /^[a-z0-9][a-z0-9._-]*$/i;

export function createLocalAssetUri(id = crypto.randomUUID()): AssetUri {
  if (!UUID_PATTERN.test(id)) {
    throw new TypeError(`Invalid local asset id: ${id}`);
  }
  return `asset://local/${id}` as AssetUri;
}

export function createRemoteAssetUri(
  providerKey: string,
  id: string,
): AssetUri {
  if (!PROVIDER_KEY_PATTERN.test(providerKey) || !id) {
    throw new TypeError("Invalid remote asset identity");
  }
  return `asset://remote/${providerKey}/${encodeURIComponent(id)}` as AssetUri;
}

export function parseAssetUri(value: string): ParsedAssetUri | null {
  const local = /^asset:\/\/local\/([^/]+)$/.exec(value);
  if (local?.[1] && UUID_PATTERN.test(local[1])) {
    return { source: "local", id: local[1] };
  }

  const remote = /^asset:\/\/remote\/([^/]+)\/(.+)$/.exec(value);
  if (remote?.[1] && remote[2] && PROVIDER_KEY_PATTERN.test(remote[1])) {
    try {
      const id = decodeURIComponent(remote[2]);
      return id ? { source: "remote", providerKey: remote[1], id } : null;
    } catch {
      return null;
    }
  }

  return null;
}

export function isAssetUri(value: unknown): value is AssetUri {
  return typeof value === "string" && parseAssetUri(value) !== null;
}

export function toAssetUri(value: string): AssetUri {
  if (!isAssetUri(value)) {
    throw new TypeError(`Invalid asset URI: ${value}`);
  }
  return value;
}
