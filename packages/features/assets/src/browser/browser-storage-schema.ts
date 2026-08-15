import type { DBSchema } from "idb";
import type { AssetRecord, AssetScope } from "../core/asset-contracts";

export const ASSET_DATABASE_NAME = "game-guild-assets";
export const ASSET_DATABASE_VERSION = 1;

export interface StoredAssetRecord extends AssetRecord {
  objectId: string;
}

export interface StoredObjectRecord {
  id: string;
  size: number;
  mimeType: string;
  refCount: number;
  backend: "opfs" | "indexeddb";
  createdAt: string;
}

export interface StoredUsageRecord {
  key: string;
  scopeKey: string;
  uri: string;
  consumerId: string;
  role?: string;
  scope: AssetScope;
}

export interface StoredJournalRecord {
  id: string;
  objectId: string;
  assetId: string;
  stage: "pending" | "object-written";
  backend?: StoredObjectRecord["backend"];
  target?: "local" | "remote-cache";
  startedAt: string;
}

export interface StoredRemoteCacheRecord {
  uri: string;
  objectId: string;
  record: AssetRecord;
  size: number;
  lastAccessedAt: string;
  pinned: boolean;
}

export interface AssetDatabaseSchema extends DBSchema {
  assets: {
    key: string;
    value: StoredAssetRecord;
    indexes: {
      "by-created-at": string;
      "by-kind": string;
      "by-name": string;
    };
  };
  objects: {
    key: string;
    value: StoredObjectRecord;
  };
  fallbackObjects: {
    key: string;
    value: Blob;
  };
  usages: {
    key: string;
    value: StoredUsageRecord;
    indexes: {
      "by-scope": string;
      "by-uri": string;
    };
  };
  remoteCache: {
    key: string;
    value: StoredRemoteCacheRecord;
    indexes: {
      "by-last-accessed-at": string;
    };
  };
  journal: {
    key: string;
    value: StoredJournalRecord;
  };
  settings: {
    key: string;
    value: unknown;
  };
}
