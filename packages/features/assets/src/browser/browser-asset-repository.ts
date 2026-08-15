import type { IDBPDatabase, IDBPTransaction } from "idb";
import type {
  AssetEventListener,
  AssetImportBlobOptions,
  AssetImportExternalOptions,
  AssetImportOptions,
  AssetKind,
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
import { AssetError, toAssetError } from "../core/asset-errors";
import { createAssetUri, parseAssetUri, type AssetUri } from "../core/asset-uri";
import { classifyAssetKind, inferMimeType } from "../core/mime";
import type { AssetRepository } from "../repository/asset-repository";
import type {
  AssetDatabaseSchema,
  StoredAssetRecord,
  StoredObjectRecord,
  StoredUsageRecord,
} from "./browser-storage-schema";
import { hashBlob } from "./content-hashing";
import { openAssetDatabase } from "./metadata-database";
import { ObjectUrlRegistry } from "./object-url-registry";

const DEFAULT_MAX_LOCAL_BYTES = 64 * 1024 * 1024;
const PROVIDER_KEY_PATTERN = /^[a-z0-9][a-z0-9._-]*$/i;
const CONTENT_HASH_PATTERN = /^sha256:[0-9a-f]{64}$/;
type AssetStoreName = "assets" | "objects" | "usages";

function throwIfAborted(signal?: AbortSignal): void {
  if (signal?.aborted) {
    throw new DOMException("Asset operation was aborted", "AbortError");
  }
}

function scopeKey(scope: AssetScope): string {
  return JSON.stringify([scope.type, scope.id]);
}

function usageKey(scope: AssetScope, usage: AssetUsageInput): string {
  return JSON.stringify([scope.type, scope.id, usage.consumerId, usage.uri]);
}

function publicRecord(record: StoredAssetRecord): AssetRecord {
  const { objectId: _objectId, ...asset } = record;
  return asset;
}

function validateName(name: string): string {
  const normalized = name.trim();
  if (!normalized) throw new AssetError("invalid", "Asset name cannot be empty");
  return normalized;
}

function validateProviderKey(providerKey: string): string {
  if (!PROVIDER_KEY_PATTERN.test(providerKey)) {
    throw new AssetError("invalid", `Invalid asset provider key: ${providerKey}`);
  }
  return providerKey;
}

function requireAssetId(uri: AssetUri): string {
  const parsed = parseAssetUri(uri);
  if (!parsed) throw new AssetError("invalid", `Invalid asset URI: ${uri}`);
  return parsed.id;
}

function validateRecordIdentity(record: AssetRecord): void {
  const parsed = parseAssetUri(record.uri);
  if (
    !parsed ||
    parsed.id !== record.id ||
    !record.name.trim() ||
    !record.mimeType.trim() ||
    !Number.isSafeInteger(record.size) ||
    record.size < 0 ||
    (record.contentHash !== undefined && !CONTENT_HASH_PATTERN.test(record.contentHash))
  ) {
    throw new AssetError("corrupt", "Asset record identity does not match its URI");
  }
  if (record.location.type === "local") return;
  validateProviderKey(record.location.providerKey);
  if (record.location.type === "external" && !record.location.reference.trim()) {
    throw new AssetError("corrupt", "External asset record has no reference");
  }
  if (record.location.type === "provider" && record.location.providerAssetId === "") {
    throw new AssetError("corrupt", "Provider asset record has an empty provider identity");
  }
}

export interface BrowserAssetRepositoryOptions {
  onEvent?: AssetEventListener;
  maxLocalBytes?: number;
  remoteOnlyKinds?: readonly AssetKind[];
}

export class BrowserAssetRepository implements AssetRepository {
  private databasePromise?: Promise<IDBPDatabase<AssetDatabaseSchema>>;
  private readonly urls = new ObjectUrlRegistry();

  constructor(private readonly options: BrowserAssetRepositoryOptions = {}) {}

  private async database(): Promise<IDBPDatabase<AssetDatabaseSchema>> {
    if (typeof indexedDB === "undefined") {
      throw new AssetError("storage-unavailable", "IndexedDB is unavailable");
    }
    this.databasePromise ??= openAssetDatabase();
    try {
      return await this.databasePromise;
    } catch (error) {
      throw toAssetError(error, "storage-unavailable");
    }
  }

  private async completeTransaction(
    tx: IDBPTransaction<AssetDatabaseSchema, AssetStoreName[], "readwrite">,
    signal?: AbortSignal,
  ): Promise<void> {
    const abort = () => {
      try {
        tx.abort();
      } catch {
        // The transaction already finished.
      }
    };
    signal?.addEventListener("abort", abort, { once: true });
    try {
      if (signal?.aborted) {
        abort();
        await tx.done.catch(() => undefined);
      }
      throwIfAborted(signal);
      await tx.done;
      throwIfAborted(signal);
    } finally {
      signal?.removeEventListener("abort", abort);
    }
  }

  async importFiles(
    files: readonly File[],
    options: AssetImportOptions = {},
  ): Promise<AssetRecord[]> {
    const records: AssetRecord[] = [];
    for (const file of files) {
      throwIfAborted(options.signal);
      records.push(
        await this.importBlob(file, {
          ...options,
          name: file.name,
          mimeType: file.type,
        }),
      );
    }
    return records;
  }

  async importBlob(blob: Blob, options: AssetImportBlobOptions): Promise<AssetRecord> {
    if (blob.size === 0) throw new AssetError("invalid", "Empty files are not supported");
    const maxLocalBytes = this.options.maxLocalBytes ?? DEFAULT_MAX_LOCAL_BYTES;
    if (blob.size > maxLocalBytes) {
      throw new AssetError(
        "unsupported",
        `Assets larger than ${maxLocalBytes} bytes require a remote provider`,
      );
    }

    const name = validateName(options.name);
    const mimeType = inferMimeType(name, options.mimeType || blob.type);
    const kind = classifyAssetKind(mimeType, name);
    if (this.options.remoteOnlyKinds?.includes(kind)) {
      throw new AssetError("unsupported", `${kind} assets require a remote provider`);
    }

    this.options.onEvent?.({ type: "import-started", name, size: blob.size });
    try {
      const contentHash = await hashBlob(blob, options.signal);
      throwIfAborted(options.signal);
      const uri = createAssetUri(options.id);
      const id = requireAssetId(uri);
      const now = new Date().toISOString();
      const stored: StoredAssetRecord = {
        id,
        uri,
        objectId: contentHash,
        name,
        kind,
        mimeType,
        size: blob.size,
        contentHash,
        location: { type: "local" },
        availability: "local-only",
        createdAt: now,
        updatedAt: now,
        source: options.source ?? { type: "device" },
        scope: options.scope,
        tags: options.tags,
      };

      const database = await this.database();
      const tx = database.transaction(["assets", "objects"], "readwrite", {
        durability: "strict",
      });
      const assets = tx.objectStore("assets");
      const existingAsset = await assets.get(id);
      if (existingAsset) {
        await tx.done;
        if (
          existingAsset.location.type === "local" &&
          existingAsset.contentHash === contentHash
        ) {
          return publicRecord(existingAsset);
        }
        throw new AssetError("invalid", `Asset id already exists: ${id}`);
      }

      const objects = tx.objectStore("objects");
      const existingObject = await objects.get(contentHash);
      const object: StoredObjectRecord = existingObject
        ? { ...existingObject, refCount: existingObject.refCount + 1 }
        : {
            id: contentHash,
            size: blob.size,
            refCount: 1,
            blob,
            createdAt: now,
          };
      await objects.put(object);
      await assets.add(stored);
      await this.completeTransaction(tx, options.signal);

      const record = publicRecord(stored);
      this.options.onEvent?.({ type: "import-completed", record });
      return record;
    } catch (error) {
      const assetError = toAssetError(error, "storage-unavailable");
      this.options.onEvent?.({ type: "import-failed", name, error: assetError });
      throw assetError;
    }
  }

  async importExternal(options: AssetImportExternalOptions): Promise<AssetRecord> {
    const name = validateName(options.name);
    const providerKey = validateProviderKey(options.providerKey);
    const reference = options.reference.trim();
    if (!reference) throw new AssetError("invalid", "External asset reference cannot be empty");
    const uri = createAssetUri(options.id);
    const id = requireAssetId(uri);
    const mimeType = inferMimeType(name, options.mimeType);
    const now = new Date().toISOString();
    const stored: StoredAssetRecord = {
      id,
      uri,
      name,
      kind: options.kind ?? classifyAssetKind(mimeType, name),
      mimeType,
      size: 0,
      location: { type: "external", providerKey, reference },
      availability: "remote",
      createdAt: now,
      updatedAt: now,
      source: options.source ?? { type: "remote", value: providerKey },
      scope: options.scope,
      tags: options.tags,
    };
    const database = await this.database();
    const tx = database.transaction("assets", "readwrite", { durability: "strict" });
    const existing = await tx.store.get(id);
    if (existing) {
      await tx.done;
      if (
        existing.location.type === "external" &&
        existing.location.providerKey === providerKey &&
        existing.location.reference === reference
      ) {
        return publicRecord(existing);
      }
      throw new AssetError("invalid", `Asset id already exists: ${id}`);
    }
    await tx.store.add(stored);
    await tx.done;
    return publicRecord(stored);
  }

  async storeMetadata(record: AssetRecord): Promise<AssetRecord> {
    validateRecordIdentity(record);
    const database = await this.database();
    const tx = database.transaction(["assets", "objects"], "readwrite", {
      durability: "strict",
    });
    const assets = tx.objectStore("assets");
    const existing = await assets.get(record.id);
    let objectId = existing?.objectId;
    if (
      objectId &&
      record.location.type !== "local" &&
      record.contentHash !== objectId
    ) {
      const objects = tx.objectStore("objects");
      const object = await objects.get(objectId);
      if (object?.refCount === 1) await objects.delete(objectId);
      else if (object) await objects.put({ ...object, refCount: object.refCount - 1 });
      objectId = undefined;
    }
    if (record.location.type === "local" && !objectId) {
      tx.abort();
      await tx.done.catch(() => undefined);
      throw new AssetError("corrupt", "Local asset metadata requires stored bytes");
    }
    const stored: StoredAssetRecord = { ...record, objectId };
    await assets.put(stored);
    await tx.done;
    return publicRecord(stored);
  }

  async get(uri: AssetUri): Promise<AssetRecord | null> {
    const id = requireAssetId(uri);
    const record = await (await this.database()).get("assets", id);
    return record ? publicRecord(record) : null;
  }

  async list(query: AssetQuery = {}): Promise<AssetPage> {
    const database = await this.database();
    const normalizedSearch = query.search?.trim().toLocaleLowerCase();
    const offset = Math.max(0, Number.parseInt(query.cursor ?? "0", 10) || 0);
    const limit = Math.min(200, Math.max(1, query.limit ?? 50));
    const expectedScope = query.scope ? scopeKey(query.scope) : undefined;
    const records = (await database.getAll("assets"))
      .filter((record) => {
        if (normalizedSearch && !record.name.toLocaleLowerCase().includes(normalizedSearch)) {
          return false;
        }
        if (query.kinds?.length && !query.kinds.includes(record.kind)) return false;
        if (query.mimeTypes?.length && !query.mimeTypes.includes(record.mimeType)) return false;
        if (query.availability?.length && !query.availability.includes(record.availability)) {
          return false;
        }
        if (expectedScope && (!record.scope || scopeKey(record.scope) !== expectedScope)) return false;
        if (query.createdAfter && record.createdAt <= query.createdAfter) return false;
        return true;
      })
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    const items = records.slice(offset, offset + limit).map(publicRecord);
    return {
      items,
      nextCursor: offset + limit < records.length ? String(offset + limit) : undefined,
    };
  }

  async listUsageUrisByScope(scope: AssetScope): Promise<AssetUri[]> {
    const usages = await (await this.database()).getAllFromIndex(
      "usages",
      "by-scope",
      scopeKey(scope),
    );
    return Array.from(new Set(usages.map((usage) => usage.uri as AssetUri)));
  }

  async listUsedByScope(scope: AssetScope): Promise<AssetRecord[]> {
    const records = await Promise.all(
      (await this.listUsageUrisByScope(scope)).map((uri) => this.get(uri)),
    );
    return records.filter((record): record is AssetRecord => Boolean(record));
  }

  async hasUsage(uri: AssetUri): Promise<boolean> {
    return (await (await this.database()).countFromIndex("usages", "by-uri", uri)) > 0;
  }

  async checkPortability(uris: readonly AssetUri[]): Promise<AssetPortabilityReport> {
    const localOnly: AssetUri[] = [];
    const unavailable: AssetUri[] = [];
    for (const uri of new Set(uris)) {
      const record = await this.get(uri);
      if (!record || record.availability === "unavailable" || record.availability === "failed") {
        unavailable.push(uri);
      } else if (record.location.type === "local") {
        localOnly.push(uri);
      }
    }
    return { portable: localOnly.length === 0 && unavailable.length === 0, localOnly, unavailable };
  }

  private async storedRecord(uri: AssetUri): Promise<StoredAssetRecord> {
    const id = requireAssetId(uri);
    const record = await (await this.database()).get("assets", id);
    if (!record) throw new AssetError("missing", `Asset not found: ${uri}`);
    return record;
  }

  async readBlob(uri: AssetUri, options: AssetReadOptions = {}): Promise<Blob> {
    throwIfAborted(options.signal);
    const record = await this.storedRecord(uri);
    if (!record.objectId) {
      throw new AssetError("missing", `Asset bytes are not stored in this browser: ${uri}`);
    }
    const object = await (await this.database()).get("objects", record.objectId);
    if (!object) throw new AssetError("corrupt", `Asset object is missing: ${uri}`);
    if (object.size !== record.size || object.blob.size !== record.size) {
      throw new AssetError("corrupt", `Asset size does not match metadata: ${uri}`);
    }
    throwIfAborted(options.signal);
    return object.blob.type === record.mimeType
      ? object.blob
      : object.blob.slice(0, object.blob.size, record.mimeType);
  }

  async readText(uri: AssetUri, options: AssetReadTextOptions = {}): Promise<string> {
    const blob = await this.readBlob(uri, options);
    if (!options.encoding || options.encoding.toLowerCase() === "utf-8") return blob.text();
    return new TextDecoder(options.encoding).decode(await blob.arrayBuffer());
  }

  async readStream(
    uri: AssetUri,
    options: AssetReadOptions = {},
  ): Promise<ReadableStream<Uint8Array>> {
    return (await this.readBlob(uri, options)).stream();
  }

  async createObjectUrl(uri: AssetUri): Promise<ResolvedAssetUrl> {
    const record = await this.storedRecord(uri);
    if (!record.objectId) {
      throw new AssetError("missing", `Asset bytes are not stored in this browser: ${uri}`);
    }
    const blob = await this.readBlob(uri);
    return this.urls.acquire(`${record.objectId}\u0000${record.mimeType}`, blob);
  }

  async rename(uri: AssetUri, name: string): Promise<AssetRecord> {
    const id = requireAssetId(uri);
    const database = await this.database();
    const tx = database.transaction("assets", "readwrite");
    const record = await tx.store.get(id);
    if (!record) throw new AssetError("missing", `Asset not found: ${uri}`);
    record.name = validateName(name);
    record.updatedAt = new Date().toISOString();
    await tx.store.put(record);
    await tx.done;
    return publicRecord(record);
  }

  async remove(uri: AssetUri, options: AssetRemoveOptions = {}): Promise<void> {
    throwIfAborted(options.signal);
    const id = requireAssetId(uri);
    const database = await this.database();
    const tx = database.transaction(["assets", "objects", "usages"], "readwrite", {
      durability: "strict",
    });
    const record = await tx.objectStore("assets").get(id);
    if (!record) throw new AssetError("missing", `Asset not found: ${uri}`);
    const usages = await tx.objectStore("usages").index("by-uri").getAll(uri);
    if (usages.length > 0 && !options.force) {
      throw new AssetError("invalid", "Asset is still referenced");
    }
    await tx.objectStore("assets").delete(id);
    for (const usage of usages) await tx.objectStore("usages").delete(usage.key);
    if (record.objectId) {
      const objects = tx.objectStore("objects");
      const object = await objects.get(record.objectId);
      if (object?.refCount === 1) await objects.delete(object.id);
      else if (object) await objects.put({ ...object, refCount: object.refCount - 1 });
    }
    await this.completeTransaction(tx, options.signal);
  }

  async reconcileUsage(scope: AssetScope, usages: readonly AssetUsageInput[]): Promise<void> {
    const database = await this.database();
    const key = scopeKey(scope);
    const tx = database.transaction("usages", "readwrite", { durability: "strict" });
    const current = await tx.store.index("by-scope").getAll(key);
    for (const usage of current) await tx.store.delete(usage.key);
    for (const usage of usages) {
      if (!parseAssetUri(usage.uri)) {
        tx.abort();
        await tx.done.catch(() => undefined);
        throw new AssetError("invalid", `Invalid asset usage URI: ${usage.uri}`);
      }
      const record: StoredUsageRecord = {
        key: usageKey(scope, usage),
        scopeKey: key,
        uri: usage.uri,
        consumerId: usage.consumerId,
        role: usage.role,
        scope,
      };
      await tx.store.put(record);
    }
    await tx.done;
  }

  async getStorageStatus(): Promise<AssetStorageStatus> {
    const objects = await (await this.database()).getAll("objects");
    const estimate =
      typeof navigator !== "undefined" && navigator.storage?.estimate
        ? await navigator.storage.estimate()
        : {};
    const persisted =
      typeof navigator !== "undefined" && navigator.storage?.persisted
        ? await navigator.storage.persisted()
        : null;
    return {
      backend: "indexeddb",
      available: true,
      persisted,
      usage: estimate.usage,
      quota: estimate.quota,
      localBytes: objects.reduce((total, object) => total + object.size, 0),
    };
  }

  async requestPersistentStorage(): Promise<AssetPersistenceResult> {
    if (typeof navigator === "undefined" || !navigator.storage?.persist) {
      const result = { supported: false, persisted: false };
      this.options.onEvent?.({ type: "persistence-result", result });
      return result;
    }
    const result = { supported: true, persisted: await navigator.storage.persist() };
    this.options.onEvent?.({ type: "persistence-result", result });
    return result;
  }

  dispose(): void {
    this.urls.clear();
  }
}

let defaultRepository: BrowserAssetRepository | undefined;

export function getDefaultBrowserAssetRepository(): BrowserAssetRepository {
  defaultRepository ??= new BrowserAssetRepository();
  return defaultRepository;
}
