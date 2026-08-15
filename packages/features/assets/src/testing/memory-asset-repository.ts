import type {
  AssetImportBlobOptions,
  AssetImportExternalOptions,
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
import { AssetError } from "../core/asset-errors";
import { createAssetUri, parseAssetUri, type AssetUri } from "../core/asset-uri";
import { classifyAssetKind, inferMimeType } from "../core/mime";
import type { AssetRepository } from "../repository/asset-repository";
import { hashBlob } from "../browser/content-hashing";

interface MemoryRecord {
  record: AssetRecord;
  objectId?: string;
}

export class MemoryAssetRepository implements AssetRepository {
  private readonly records = new Map<string, MemoryRecord>();
  private readonly objects = new Map<string, Blob>();
  private readonly objectRefs = new Map<string, number>();
  private readonly usages = new Map<string, AssetUsageInput[]>();

  async importFiles(files: readonly File[], options: AssetImportOptions = {}): Promise<AssetRecord[]> {
    return Promise.all(files.map((file) => this.importBlob(file, {
      ...options,
      name: file.name,
      mimeType: file.type,
    })));
  }

  async importBlob(blob: Blob, options: AssetImportBlobOptions): Promise<AssetRecord> {
    if (options.signal?.aborted) throw new DOMException("Aborted", "AbortError");
    if (!blob.size) throw new AssetError("invalid", "Empty files are not supported");
    const name = options.name.trim();
    if (!name) throw new AssetError("invalid", "Asset name cannot be empty");
    const uri = createAssetUri(options.id);
    const id = parseAssetUri(uri)!.id;
    const existing = this.records.get(id);
    const mimeType = inferMimeType(name, options.mimeType || blob.type);
    const contentHash = await hashBlob(blob, options.signal);
    if (existing) {
      if (existing.record.location.type === "local" && existing.objectId === contentHash) {
        return structuredClone(existing.record);
      }
      throw new AssetError("invalid", `Asset id already exists: ${id}`);
    }
    const now = new Date().toISOString();
    const record: AssetRecord = {
      id,
      uri,
      name,
      kind: classifyAssetKind(mimeType, name),
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
    this.objects.set(contentHash, blob);
    this.objectRefs.set(contentHash, (this.objectRefs.get(contentHash) ?? 0) + 1);
    this.records.set(id, { record, objectId: contentHash });
    return structuredClone(record);
  }

  async importExternal(options: AssetImportExternalOptions): Promise<AssetRecord> {
    const name = options.name.trim();
    const reference = options.reference.trim();
    if (!name || !reference) throw new AssetError("invalid", "External asset metadata is invalid");
    const uri = createAssetUri(options.id);
    const id = parseAssetUri(uri)!.id;
    const existing = this.records.get(id);
    if (existing) {
      if (existing.record.location.type === "external" &&
        existing.record.location.providerKey === options.providerKey &&
        existing.record.location.reference === reference) return structuredClone(existing.record);
      throw new AssetError("invalid", `Asset id already exists: ${id}`);
    }
    const mimeType = inferMimeType(name, options.mimeType);
    const now = new Date().toISOString();
    const record: AssetRecord = {
      id,
      uri,
      name,
      kind: options.kind ?? classifyAssetKind(mimeType, name),
      mimeType,
      size: 0,
      location: { type: "external", providerKey: options.providerKey, reference },
      availability: "remote",
      createdAt: now,
      updatedAt: now,
      source: options.source ?? { type: "remote", value: options.providerKey },
      scope: options.scope,
      tags: options.tags,
    };
    this.records.set(id, { record });
    return structuredClone(record);
  }

  async get(uri: AssetUri): Promise<AssetRecord | null> {
    const parsed = parseAssetUri(uri);
    const value = parsed ? this.records.get(parsed.id) : undefined;
    return value ? structuredClone(value.record) : null;
  }

  async list(query: AssetQuery = {}): Promise<AssetPage> {
    const search = query.search?.trim().toLocaleLowerCase();
    const offset = Number.parseInt(query.cursor ?? "0", 10) || 0;
    const limit = query.limit ?? 50;
    const items = Array.from(this.records.values(), ({ record }) => record)
      .filter((record) => {
        if (search && !record.name.toLocaleLowerCase().includes(search)) return false;
        if (query.kinds?.length && !query.kinds.includes(record.kind)) return false;
        if (query.mimeTypes?.length && !query.mimeTypes.includes(record.mimeType)) return false;
        if (query.availability?.length && !query.availability.includes(record.availability)) return false;
        if (query.scope &&
          (record.scope?.type !== query.scope.type || record.scope.id !== query.scope.id)) return false;
        if (query.createdAfter && record.createdAt <= query.createdAfter) return false;
        return true;
      })
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    return {
      items: structuredClone(items.slice(offset, offset + limit)),
      nextCursor: offset + limit < items.length ? String(offset + limit) : undefined,
    };
  }

  async listUsedByScope(scope: AssetScope): Promise<AssetRecord[]> {
    const usages = this.usages.get(JSON.stringify([scope.type, scope.id])) ?? [];
    const records = await Promise.all(usages.map((usage) => this.get(usage.uri)));
    return records.filter((record): record is AssetRecord => Boolean(record));
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

  private require(uri: AssetUri): MemoryRecord {
    const parsed = parseAssetUri(uri);
    const value = parsed ? this.records.get(parsed.id) : undefined;
    if (!value) throw new AssetError("missing", `Asset not found: ${uri}`);
    return value;
  }

  async readBlob(uri: AssetUri, options: AssetReadOptions = {}): Promise<Blob> {
    if (options.signal?.aborted) throw new DOMException("Aborted", "AbortError");
    const value = this.require(uri);
    const blob = value.objectId ? this.objects.get(value.objectId) : undefined;
    if (!blob) throw new AssetError("missing", `Asset bytes are not stored: ${uri}`);
    return blob.type === value.record.mimeType ? blob : blob.slice(0, blob.size, value.record.mimeType);
  }

  async readText(uri: AssetUri, options: AssetReadTextOptions = {}): Promise<string> {
    const blob = await this.readBlob(uri, options);
    return options.encoding && options.encoding.toLowerCase() !== "utf-8"
      ? new TextDecoder(options.encoding).decode(await blob.arrayBuffer())
      : blob.text();
  }

  async readStream(uri: AssetUri, options: AssetReadOptions = {}): Promise<ReadableStream<Uint8Array>> {
    return (await this.readBlob(uri, options)).stream();
  }

  async createObjectUrl(uri: AssetUri): Promise<ResolvedAssetUrl> {
    const url = URL.createObjectURL(await this.readBlob(uri));
    let released = false;
    return { url, release: () => {
      if (released) return;
      released = true;
      URL.revokeObjectURL(url);
    } };
  }

  async rename(uri: AssetUri, name: string): Promise<AssetRecord> {
    const value = this.require(uri);
    const normalized = name.trim();
    if (!normalized) throw new AssetError("invalid", "Asset name cannot be empty");
    value.record.name = normalized;
    value.record.updatedAt = new Date().toISOString();
    return structuredClone(value.record);
  }

  async remove(uri: AssetUri, options: AssetRemoveOptions = {}): Promise<void> {
    const value = this.require(uri);
    const referenced = Array.from(this.usages.values()).some((entries) =>
      entries.some((entry) => entry.uri === uri));
    if (referenced && !options.force) throw new AssetError("invalid", "Asset is referenced");
    this.records.delete(value.record.id);
    if (!value.objectId) return;
    const refs = (this.objectRefs.get(value.objectId) ?? 1) - 1;
    if (refs <= 0) {
      this.objectRefs.delete(value.objectId);
      this.objects.delete(value.objectId);
    } else this.objectRefs.set(value.objectId, refs);
  }

  async reconcileUsage(scope: AssetScope, usages: readonly AssetUsageInput[]): Promise<void> {
    this.usages.set(JSON.stringify([scope.type, scope.id]), [...usages]);
  }

  async getStorageStatus(): Promise<AssetStorageStatus> {
    return {
      backend: "memory",
      available: true,
      persisted: false,
      localBytes: Array.from(this.objects.values()).reduce((total, blob) => total + blob.size, 0),
    };
  }

  async requestPersistentStorage(): Promise<AssetPersistenceResult> {
    return { supported: false, persisted: false };
  }
}
