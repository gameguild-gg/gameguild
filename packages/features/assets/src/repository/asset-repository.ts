import type {
  AssetCachePolicy,
  AssetCacheResult,
  AssetImportBlobOptions,
  AssetImportOptions,
  AssetPage,
  AssetPortabilityReport,
  AssetPersistenceResult,
  AssetQuery,
  AssetReadOptions,
  AssetReadTextOptions,
  AssetRecord,
  AssetRemoveOptions,
  AssetScope,
  AssetStorageStatus,
  AssetUsageInput,
  ResolvedAssetUrl,
} from "../core/asset-contracts";
import type { AssetUri } from "../core/asset-uri";

export interface AssetRepository {
  importFiles(
    files: readonly File[],
    options?: AssetImportOptions,
  ): Promise<AssetRecord[]>;
  importBlob(blob: Blob, options: AssetImportBlobOptions): Promise<AssetRecord>;
  get(uri: AssetUri): Promise<AssetRecord | null>;
  list(query?: AssetQuery): Promise<AssetPage>;
  listUsedByScope(scope: AssetScope): Promise<AssetRecord[]>;
  checkPortability(uris: readonly AssetUri[]): Promise<AssetPortabilityReport>;
  readBlob(uri: AssetUri, options?: AssetReadOptions): Promise<Blob>;
  readStream(uri: AssetUri, options?: AssetReadOptions): Promise<ReadableStream<Uint8Array>>;
  readText(uri: AssetUri, options?: AssetReadTextOptions): Promise<string>;
  createObjectUrl(uri: AssetUri): Promise<ResolvedAssetUrl>;
  rename(uri: AssetUri, name: string): Promise<AssetRecord>;
  remove(uri: AssetUri, options?: AssetRemoveOptions): Promise<void>;
  reconcileUsage(
    scope: AssetScope,
    usages: readonly AssetUsageInput[],
  ): Promise<void>;
  getStorageStatus(): Promise<AssetStorageStatus>;
  requestPersistentStorage(): Promise<AssetPersistenceResult>;
  evictRemoteCache?(policy?: AssetCachePolicy): Promise<AssetCacheResult>;
}
