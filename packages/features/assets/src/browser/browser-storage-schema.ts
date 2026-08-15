import type { DBSchema } from "idb";
import type { AssetRecord, AssetScope } from "../core/asset-contracts";

export const ASSET_DATABASE_NAME = "game-guild-assets";
export const ASSET_DATABASE_VERSION = 3;

export interface StoredAssetRecord extends AssetRecord {
  objectId?: string;
}

export interface StoredObjectRecord {
  id: string;
  size: number;
  refCount: number;
  blob: Blob;
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
  usages: {
    key: string;
    value: StoredUsageRecord;
    indexes: {
      "by-scope": string;
      "by-uri": string;
    };
  };
}
