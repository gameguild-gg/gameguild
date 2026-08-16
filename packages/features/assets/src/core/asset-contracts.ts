import type { AssetUri } from "./asset-uri";

export type AssetKind =
  | "image"
  | "video"
  | "audio"
  | "document"
  | "dataset"
  | "archive"
  | "code"
  | "other";

export type AssetAvailability =
  | "local-only"
  | "remote"
  | "pending-upload"
  | "unavailable"
  | "failed";

export interface AssetScope {
  type: string;
  id: string;
}

export interface AssetSource {
  type: "device" | "download" | "generated" | "remote";
  value?: string;
}

export type AssetLocation =
  | { type: "local" }
  | { type: "provider"; providerKey: string; providerAssetId?: string }
  | { type: "external"; providerKey: string; reference: string };

export interface AssetRecord {
  id: string;
  uri: AssetUri;
  name: string;
  kind: AssetKind;
  mimeType: string;
  size: number;
  contentHash?: string;
  location: AssetLocation;
  availability: AssetAvailability;
  createdAt: string;
  updatedAt: string;
  lastAccessedAt?: string;
  source?: AssetSource;
  scope?: AssetScope;
  tags?: string[];
}

export interface AssetImportOptions {
  scope?: AssetScope;
  source?: AssetSource;
  tags?: string[];
  signal?: AbortSignal;
}

export interface AssetImportBlobOptions extends AssetImportOptions {
  id?: string;
  name: string;
  mimeType?: string;
}

export interface AssetImportExternalOptions extends AssetImportOptions {
  id?: string;
  name: string;
  providerKey: string;
  reference: string;
  mimeType?: string;
  kind?: AssetKind;
}

export interface AssetReadOptions {
  signal?: AbortSignal;
}

export interface AssetReadTextOptions extends AssetReadOptions {
  encoding?: string;
}

export interface AssetQuery {
  search?: string;
  kinds?: readonly AssetKind[];
  mimeTypes?: readonly string[];
  scope?: AssetScope;
  availability?: readonly AssetAvailability[];
  createdAfter?: string;
  includeRemote?: boolean;
  cursor?: string;
  limit?: number;
}

export interface AssetPage {
  items: AssetRecord[];
  nextCursor?: string;
}

export interface ResolvedAssetUrl {
  url: string;
  release: () => void;
  expiresAt?: string;
}

export interface AssetUsageInput {
  uri: AssetUri;
  consumerId: string;
  role?: string;
}

export interface AssetRemoveOptions {
  force?: boolean;
  signal?: AbortSignal;
}

export interface AssetStorageStatus {
  backend: "indexeddb" | "memory";
  available: boolean;
  persisted: boolean | null;
  usage?: number;
  quota?: number;
  localBytes: number;
}

export interface AssetPersistenceResult {
  supported: boolean;
  persisted: boolean;
}

export interface AssetPortabilityReport {
  portable: boolean;
  localOnly: AssetUri[];
  unavailable: AssetUri[];
}

export type AssetEvent =
  | { type: "import-started"; name: string; size: number }
  | { type: "import-completed"; record: AssetRecord }
  | { type: "import-failed"; name: string; error: Error }
  | { type: "persistence-result"; result: AssetPersistenceResult };

export type AssetEventListener = (event: AssetEvent) => void;
