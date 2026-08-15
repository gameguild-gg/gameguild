import type { IDBPDatabase } from "idb";
import type {
  AssetCachePolicy,
  AssetCacheResult,
  AssetEventListener,
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
import { AssetError, toAssetError } from "../core/asset-errors";
import { createLocalAssetUri, parseAssetUri, type AssetUri } from "../core/asset-uri";
import { classifyAssetKind, inferMimeType } from "../core/mime";
import type { AssetRepository } from "../repository/asset-repository";
import type { AssetDownload } from "../providers/remote-asset-provider";
import type {
  AssetDatabaseSchema,
  StoredAssetRecord,
  StoredJournalRecord,
  StoredObjectRecord,
  StoredUsageRecord,
} from "./browser-storage-schema";
import { hashBlob } from "./content-hashing";
import { IndexedDbAssetObjectStore } from "./indexeddb-object-store";
import { openAssetDatabase } from "./metadata-database";
import { ObjectUrlRegistry } from "./object-url-registry";
import type { AssetObjectStore } from "./object-store";
import { throwIfAborted } from "./object-store";
import { OpfsAssetObjectStore } from "./opfs-object-store";

const MUTATION_LOCK = "game-guild-assets-mutation";
const BROADCAST_CHANNEL = "game-guild-assets";

function scopeKey(scope: AssetScope): string {
  return `${scope.type}:${scope.id}`;
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

interface LockManagerWithSignal {
  request<T>(
    name: string,
    options: LockOptions & { signal?: AbortSignal },
    callback: () => Promise<T>,
  ): Promise<T>;
}

export interface BrowserAssetRepositoryOptions {
  onEvent?: AssetEventListener;
}

export class BrowserAssetRepository implements AssetRepository {
  private databasePromise?: Promise<IDBPDatabase<AssetDatabaseSchema>>;
  private readonly urls = new ObjectUrlRegistry();
  private readonly channel: BroadcastChannel | null;
  private initialization?: Promise<void>;
  private opfsStore?: OpfsAssetObjectStore;
  private fallbackStore?: IndexedDbAssetObjectStore;

  constructor(private readonly options: BrowserAssetRepositoryOptions = {}) {
    this.channel =
      typeof window !== "undefined" && typeof BroadcastChannel === "function"
        ? new BroadcastChannel(BROADCAST_CHANNEL)
        : null;
    this.channel?.addEventListener("message", (event) => {
      const objectId = (event.data as { objectId?: unknown } | null)?.objectId;
      if (typeof objectId === "string") this.urls.invalidate(objectId);
    });
  }

  private async initialize(): Promise<void> {
    this.initialization ??= this.recoverInterruptedWrites().catch((reason: unknown) => {
      const error = reason instanceof Error ? reason : new Error(String(reason));
      this.options.onEvent?.({ type: "recovery-failed", error });
      throw error;
    });
    return this.initialization;
  }

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

  private async fallback(): Promise<IndexedDbAssetObjectStore> {
    this.fallbackStore ??= new IndexedDbAssetObjectStore(await this.database());
    return this.fallbackStore;
  }

  private preferredStore(): AssetObjectStore | Promise<AssetObjectStore> {
    if (OpfsAssetObjectStore.isSupported()) {
      this.opfsStore ??= new OpfsAssetObjectStore();
      return this.opfsStore;
    }
    return this.fallback();
  }

  private async storeFor(
    backend: StoredObjectRecord["backend"],
  ): Promise<AssetObjectStore> {
    if (backend === "opfs") {
      if (!OpfsAssetObjectStore.isSupported()) {
        throw new AssetError("storage-unavailable", "This asset requires OPFS");
      }
      this.opfsStore ??= new OpfsAssetObjectStore();
      return this.opfsStore;
    }
    return this.fallback();
  }

  private async mutate<T>(signal: AbortSignal | undefined, work: () => Promise<T>): Promise<T> {
    throwIfAborted(signal);
    const locks =
      typeof navigator !== "undefined"
        ? (navigator.locks as unknown as LockManagerWithSignal | undefined)
        : undefined;
    if (locks?.request) {
      return locks.request(MUTATION_LOCK, { mode: "exclusive", signal }, work);
    }
    return work();
  }

  private async recoverInterruptedWrites(): Promise<void> {
    const database = await this.database();
    const entries = await database.getAll("journal");
    for (const entry of entries) {
      const targetExists = entry.target === "remote-cache"
        ? await database.get("remoteCache", entry.assetId)
        : await database.get("assets", entry.assetId);
      if (!targetExists && entry.stage === "object-written") {
        const object = await database.get("objects", entry.objectId);
        if (!object) {
          if (entry.backend) {
            await (await this.storeFor(entry.backend)).remove(entry.objectId).catch(() => undefined);
          } else {
            await (await this.fallback()).remove(entry.objectId).catch(() => undefined);
            if (OpfsAssetObjectStore.isSupported()) {
              this.opfsStore ??= new OpfsAssetObjectStore();
              await this.opfsStore.remove(entry.objectId).catch(() => undefined);
            }
          }
        }
      }
      await database.delete("journal", entry.id);
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
    await this.initialize();
    if (blob.size === 0) throw new AssetError("invalid", "Empty files are not supported");
    const name = validateName(options.name);
    const mimeType = inferMimeType(name, options.mimeType || blob.type);
    this.options.onEvent?.({ type: "import-started", name, size: blob.size });

    return this.mutate(options.signal, async () => {
      const database = await this.database();
      const contentHash = await hashBlob(blob, options.signal);
      const id = crypto.randomUUID();
      const uri = createLocalAssetUri(id);
      const now = new Date().toISOString();
      const journal: StoredJournalRecord = {
        id: crypto.randomUUID(),
        objectId: contentHash,
        assetId: id,
        stage: "pending",
        startedAt: now,
      };
      await database.put("journal", journal);

      try {
        const existingObject = await database.get("objects", contentHash);
        let object = existingObject;
        if (!object) {
          let store = await this.preferredStore();
          try {
            await store.write(contentHash, blob, options.signal);
          } catch (error) {
            if (store.backend !== "opfs") throw error;
            await store.remove(contentHash).catch(() => undefined);
            store = await this.fallback();
            await store.write(contentHash, blob, options.signal);
          }
          object = {
            id: contentHash,
            size: blob.size,
            mimeType,
            refCount: 0,
            backend: store.backend,
            createdAt: now,
          };
          journal.stage = "object-written";
          journal.backend = store.backend;
          await database.put("journal", journal);
        }

        const stored: StoredAssetRecord = {
          id,
          uri,
          objectId: contentHash,
          name,
          kind: classifyAssetKind(mimeType, name),
          mimeType,
          size: blob.size,
          contentHash,
          availability: "local-only",
          createdAt: now,
          updatedAt: now,
          source: options.source ?? { type: "device" },
          scope: options.scope,
          tags: options.tags,
        };

        const tx = database.transaction(["assets", "objects", "journal"], "readwrite");
        await tx.objectStore("assets").add(stored);
        await tx.objectStore("objects").put({ ...object, refCount: object.refCount + 1 });
        await tx.objectStore("journal").delete(journal.id);
        await tx.done;
        this.channel?.postMessage({ type: "import", objectId: contentHash, uri });
        const record = publicRecord(stored);
        this.options.onEvent?.({ type: "import-completed", record });
        return record;
      } catch (error) {
        const assetError = toAssetError(error, "storage-unavailable");
        this.options.onEvent?.({ type: "import-failed", name, error: assetError });
        throw assetError;
      }
    });
  }

  async get(uri: AssetUri): Promise<AssetRecord | null> {
    await this.initialize();
    const parsed = parseAssetUri(uri);
    if (!parsed) return null;
    const database = await this.database();
    if (parsed.source === "remote") {
      const cached = await database.get("remoteCache", uri);
      return cached ? { ...cached.record, availability: "cached-remote" } : null;
    }
    const record = await database.get("assets", parsed.id);
    return record ? publicRecord(record) : null;
  }

  async list(query: AssetQuery = {}): Promise<AssetPage> {
    await this.initialize();
    const database = await this.database();
    const normalizedSearch = query.search?.trim().toLocaleLowerCase();
    const offset = Math.max(0, Number.parseInt(query.cursor ?? "0", 10) || 0);
    const limit = Math.min(200, Math.max(1, query.limit ?? 50));
    const scope = query.scope ? scopeKey(query.scope) : undefined;
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
        if (scope && (!record.scope || scopeKey(record.scope) !== scope)) return false;
        if (query.createdAfter && record.createdAt <= query.createdAfter) return false;
        return true;
      })
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    const page = records.slice(offset, offset + limit).map(publicRecord);
    return {
      items: page,
      nextCursor: offset + limit < records.length ? String(offset + limit) : undefined,
    };
  }

  async listUsedByScope(scope: AssetScope): Promise<AssetRecord[]> {
    await this.initialize();
    const database = await this.database();
    const usages = await database.getAllFromIndex("usages", "by-scope", scopeKey(scope));
    const uris = new Set(usages.map((usage) => usage.uri as AssetUri));
    const records = await Promise.all(Array.from(uris, (uri) => this.get(uri)));
    return records.filter((record): record is AssetRecord => Boolean(record));
  }

  async checkPortability(uris: readonly AssetUri[]): Promise<AssetPortabilityReport> {
    const localOnly: AssetUri[] = [];
    const unavailable: AssetUri[] = [];
    for (const uri of new Set(uris)) {
      const record = await this.get(uri);
      if (!record || record.availability === "unavailable" || record.availability === "failed") {
        unavailable.push(uri);
      } else if (record.availability === "local-only" || record.availability === "pending-upload") {
        localOnly.push(uri);
      }
    }
    return { portable: localOnly.length === 0 && unavailable.length === 0, localOnly, unavailable };
  }

  private async storedRecord(uri: AssetUri): Promise<StoredAssetRecord> {
    const parsed = parseAssetUri(uri);
    if (parsed?.source !== "local") {
      throw new AssetError("missing", `Asset is not available locally: ${uri}`);
    }
    const database = await this.database();
    const record = await database.get("assets", parsed.id);
    if (!record) throw new AssetError("missing", `Asset not found: ${uri}`);
    return record;
  }

  async readBlob(uri: AssetUri, options: AssetReadOptions = {}): Promise<Blob> {
    await this.initialize();
    throwIfAborted(options.signal);
    const database = await this.database();
    const parsed = parseAssetUri(uri);
    if (parsed?.source === "remote") {
      const cached = await database.get("remoteCache", uri);
      if (!cached) throw new AssetError("missing", `Remote asset is not cached: ${uri}`);
      const object = await database.get("objects", cached.objectId);
      if (!object) throw new AssetError("corrupt", `Cached object metadata is missing: ${uri}`);
      const blob = await (await this.storeFor(object.backend)).read(object.id, options.signal);
      if (blob.size !== cached.size) {
        throw new AssetError("corrupt", `Cached asset size does not match metadata: ${uri}`);
      }
      cached.lastAccessedAt = new Date().toISOString();
      await database.put("remoteCache", cached);
      return blob.type ? blob : blob.slice(0, blob.size, cached.record.mimeType);
    }
    const record = await this.storedRecord(uri);
    const object = await database.get("objects", record.objectId);
    if (!object) throw new AssetError("corrupt", `Asset object metadata is missing: ${uri}`);
    const blob = await (await this.storeFor(object.backend)).read(object.id, options.signal);
    if (blob.size !== record.size) {
      throw new AssetError("corrupt", `Asset size does not match metadata: ${uri}`);
    }
    record.lastAccessedAt = new Date().toISOString();
    void database.put("assets", record);
    return blob.type ? blob : blob.slice(0, blob.size, record.mimeType);
  }

  async readText(
    uri: AssetUri,
    options: AssetReadTextOptions = {},
  ): Promise<string> {
    const blob = await this.readBlob(uri, options);
    if (!options.encoding || options.encoding.toLowerCase() === "utf-8") {
      return blob.text();
    }
    return new TextDecoder(options.encoding).decode(await blob.arrayBuffer());
  }

  async readStream(
    uri: AssetUri,
    options: AssetReadOptions = {},
  ): Promise<ReadableStream<Uint8Array>> {
    return (await this.readBlob(uri, options)).stream();
  }

  async createObjectUrl(uri: AssetUri): Promise<ResolvedAssetUrl> {
    await this.initialize();
    const parsed = parseAssetUri(uri);
    const objectId = parsed?.source === "remote"
      ? (await (await this.database()).get("remoteCache", uri))?.objectId
      : (await this.storedRecord(uri)).objectId;
    if (!objectId) throw new AssetError("missing", `Asset is not available locally: ${uri}`);
    const blob = await this.readBlob(uri);
    return this.urls.acquire(objectId, blob);
  }

  async cacheRemoteAsset(
    download: AssetDownload,
    options: { pinned?: boolean; signal?: AbortSignal } = {},
  ): Promise<AssetRecord> {
    await this.initialize();
    const parsed = parseAssetUri(download.record.uri);
    if (parsed?.source !== "remote") {
      throw new AssetError("invalid", "Remote cache requires a remote asset URI");
    }
    if (download.blob.size !== download.record.size) {
      throw new AssetError("corrupt", "Downloaded asset size does not match metadata");
    }
    return this.mutate(options.signal, async () => {
      const database = await this.database();
      const existing = await database.get("remoteCache", download.record.uri);
      if (existing) {
        existing.lastAccessedAt = new Date().toISOString();
        existing.pinned = options.pinned ?? existing.pinned;
        existing.record = { ...download.record, availability: "cached-remote" };
        await database.put("remoteCache", existing);
        return existing.record;
      }

      const objectId = await hashBlob(download.blob, options.signal);
      const now = new Date().toISOString();
      const journal: StoredJournalRecord = {
        id: crypto.randomUUID(),
        objectId,
        assetId: download.record.uri,
        stage: "pending",
        target: "remote-cache",
        startedAt: now,
      };
      await database.put("journal", journal);
      try {
        let object = await database.get("objects", objectId);
        if (!object) {
          let store = await this.preferredStore();
          try {
            await store.write(objectId, download.blob, options.signal);
          } catch (error) {
            if (store.backend !== "opfs") throw error;
            await store.remove(objectId).catch(() => undefined);
            store = await this.fallback();
            await store.write(objectId, download.blob, options.signal);
          }
          object = {
            id: objectId,
            size: download.blob.size,
            mimeType: download.record.mimeType,
            refCount: 0,
            backend: store.backend,
            createdAt: now,
          };
          journal.stage = "object-written";
          journal.backend = store.backend;
          await database.put("journal", journal);
        }
        const record = { ...download.record, availability: "cached-remote" as const };
        const tx = database.transaction(["objects", "remoteCache", "journal"], "readwrite");
        await tx.objectStore("objects").put({ ...object, refCount: object.refCount + 1 });
        await tx.objectStore("remoteCache").put({
          uri: record.uri,
          objectId,
          size: record.size,
          lastAccessedAt: now,
          pinned: options.pinned ?? false,
          record,
        });
        await tx.objectStore("journal").delete(journal.id);
        await tx.done;
        this.channel?.postMessage({ type: "remote-cache", objectId, uri: record.uri });
        return record;
      } catch (error) {
        throw toAssetError(error, "storage-unavailable");
      }
    });
  }

  async removeCachedRemote(uri: AssetUri): Promise<void> {
    await this.initialize();
    await this.mutate(undefined, async () => {
      const database = await this.database();
      const cached = await database.get("remoteCache", uri);
      if (!cached) return;
      const object = await database.get("objects", cached.objectId);
      const tx = database.transaction(["remoteCache", "objects"], "readwrite");
      await tx.objectStore("remoteCache").delete(uri);
      if (object) {
        if (object.refCount <= 1) await tx.objectStore("objects").delete(object.id);
        else await tx.objectStore("objects").put({ ...object, refCount: object.refCount - 1 });
      }
      await tx.done;
      if (object?.refCount === 1) {
        await (await this.storeFor(object.backend)).remove(object.id);
        this.urls.invalidate(object.id);
      }
      this.channel?.postMessage({ type: "remote-cache-remove", objectId: cached.objectId, uri });
    });
  }

  async setRemoteMapping(localUri: AssetUri, remoteUri: AssetUri): Promise<void> {
    const local = parseAssetUri(localUri);
    const remote = parseAssetUri(remoteUri);
    if (local?.source !== "local" || remote?.source !== "remote") {
      throw new AssetError("invalid", "Asset mapping requires local and remote URIs");
    }
    const database = await this.database();
    const previousRemote = await database.get("settings", `remote-mapping:${localUri}`);
    const previousLocal = await database.get("settings", `local-mapping:${remoteUri}`);
    const tx = database.transaction("settings", "readwrite");
    if (typeof previousRemote === "string") {
      await tx.store.delete(`local-mapping:${previousRemote}`);
    }
    if (typeof previousLocal === "string") {
      await tx.store.delete(`remote-mapping:${previousLocal}`);
    }
    await tx.store.put(remoteUri, `remote-mapping:${localUri}`);
    await tx.store.put(localUri, `local-mapping:${remoteUri}`);
    await tx.done;
  }

  async getRemoteMapping(localUri: AssetUri): Promise<AssetUri | null> {
    if (parseAssetUri(localUri)?.source !== "local") return null;
    const value = await (await this.database()).get("settings", `remote-mapping:${localUri}`);
    return typeof value === "string" && parseAssetUri(value)?.source === "remote"
      ? value as AssetUri
      : null;
  }

  async removeRemoteMapping(remoteUri: AssetUri): Promise<void> {
    if (parseAssetUri(remoteUri)?.source !== "remote") return;
    const database = await this.database();
    const localUri = await database.get("settings", `local-mapping:${remoteUri}`);
    const tx = database.transaction("settings", "readwrite");
    await tx.store.delete(`local-mapping:${remoteUri}`);
    if (typeof localUri === "string") await tx.store.delete(`remote-mapping:${localUri}`);
    await tx.done;
  }

  async evictRemoteCache(policy: AssetCachePolicy = {}): Promise<AssetCacheResult> {
    await this.initialize();
    const database = await this.database();
    const maxBytes = policy.maxBytes ?? 256 * 1024 * 1024;
    const maxEntries = policy.maxEntries ?? 500;
    const entries = (await database.getAll("remoteCache"))
      .sort((a, b) => a.lastAccessedAt.localeCompare(b.lastAccessedAt));
    let bytes = entries.reduce((total, entry) => total + entry.size, 0);
    let count = entries.length;
    let entriesRemoved = 0;
    let bytesRemoved = 0;
    for (const entry of entries) {
      if (bytes <= maxBytes && count <= maxEntries) break;
      if (entry.pinned || this.urls.hasLease(entry.objectId)) continue;
      await this.removeCachedRemote(entry.record.uri);
      this.options.onEvent?.({ type: "cache-evicted", uri: entry.record.uri, size: entry.size });
      bytes -= entry.size;
      count -= 1;
      entriesRemoved += 1;
      bytesRemoved += entry.size;
    }
    return { entriesRemoved, bytesRemoved };
  }

  async rename(uri: AssetUri, name: string): Promise<AssetRecord> {
    await this.initialize();
    return this.mutate(undefined, async () => {
      const database = await this.database();
      const record = await this.storedRecord(uri);
      record.name = validateName(name);
      record.updatedAt = new Date().toISOString();
      await database.put("assets", record);
      this.channel?.postMessage({ type: "rename", uri });
      return publicRecord(record);
    });
  }

  async remove(uri: AssetUri, options: AssetRemoveOptions = {}): Promise<void> {
    await this.initialize();
    await this.mutate(options.signal, async () => {
      const database = await this.database();
      const record = await this.storedRecord(uri);
      const usages = await database.getAllFromIndex("usages", "by-uri", uri);
      const remoteMapping = await this.getRemoteMapping(uri);
      if (usages.length > 0 && !options.force) {
        throw new AssetError("invalid", "Asset is still referenced");
      }
      const object = await database.get("objects", record.objectId);
      const tx = database.transaction(["assets", "objects", "usages", "settings"], "readwrite");
      await tx.objectStore("assets").delete(record.id);
      for (const usage of usages) await tx.objectStore("usages").delete(usage.key);
      await tx.objectStore("settings").delete(`remote-mapping:${uri}`);
      if (remoteMapping) {
        await tx.objectStore("settings").delete(`local-mapping:${remoteMapping}`);
      }
      if (object) {
        if (object.refCount <= 1) await tx.objectStore("objects").delete(object.id);
        else await tx.objectStore("objects").put({ ...object, refCount: object.refCount - 1 });
      }
      await tx.done;
      if (object?.refCount === 1) {
        await (await this.storeFor(object.backend)).remove(object.id);
        this.urls.invalidate(object.id);
      }
      this.channel?.postMessage({ type: "remove", objectId: record.objectId, uri });
    });
  }

  async reconcileUsage(
    scope: AssetScope,
    usages: readonly AssetUsageInput[],
  ): Promise<void> {
    await this.initialize();
    await this.mutate(undefined, async () => {
      const database = await this.database();
      const key = scopeKey(scope);
      const current = await database.getAllFromIndex("usages", "by-scope", key);
      const tx = database.transaction("usages", "readwrite");
      for (const usage of current) await tx.store.delete(usage.key);
      for (const usage of usages) {
        const record: StoredUsageRecord = {
          key: `${key}:${usage.consumerId}:${usage.uri}`,
          scopeKey: key,
          uri: usage.uri,
          consumerId: usage.consumerId,
          role: usage.role,
          scope,
        };
        await tx.store.put(record);
      }
      await tx.done;
      this.channel?.postMessage({ type: "usage", scope: key });
    });
  }

  async getStorageStatus(): Promise<AssetStorageStatus> {
    await this.initialize();
    const database = await this.database();
    const objects = await database.getAll("objects");
    const localObjectIds = new Set((await database.getAll("assets")).map((asset) => asset.objectId));
    const cache = await database.getAll("remoteCache");
    const estimate =
      typeof navigator !== "undefined" && navigator.storage?.estimate
        ? await navigator.storage.estimate()
        : {};
    const persisted =
      typeof navigator !== "undefined" && navigator.storage?.persisted
        ? await navigator.storage.persisted()
        : null;
    const backend = (await this.preferredStore()).backend;
    return {
      backend,
      available: true,
      persisted,
      usage: estimate.usage,
      quota: estimate.quota,
      localBytes: objects.reduce(
        (total, object) => total + (localObjectIds.has(object.id) ? object.size : 0),
        0,
      ),
      cacheBytes: cache.reduce(
        (total, entry) => total + (localObjectIds.has(entry.objectId) ? 0 : entry.size),
        0,
      ),
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
    this.channel?.close();
  }
}

let defaultRepository: BrowserAssetRepository | undefined;

export function getDefaultBrowserAssetRepository(): BrowserAssetRepository {
  defaultRepository ??= new BrowserAssetRepository();
  return defaultRepository;
}
