import type {
  AssetCachePolicy,
  AssetCacheResult,
  AssetImportBlobOptions,
  AssetImportOptions,
  AssetPage,
  AssetPersistenceResult,
  AssetPortabilityReport,
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
import { parseAssetUri, type AssetUri } from "../core/asset-uri";
import type { BrowserAssetRepository } from "../browser/browser-asset-repository";
import type { AssetProviderContext } from "../providers/remote-asset-provider";
import { RemoteAssetProviderRegistry } from "../providers/remote-provider-registry";
import type { AssetRepository } from "./asset-repository";

export interface ComposedAssetRepositoryOptions {
  context?: (providerKey: string, signal?: AbortSignal) => AssetProviderContext;
}

export interface AssetUploadMapping {
  localUri: AssetUri;
  remote: AssetRecord;
}

export class ComposedAssetRepository implements AssetRepository {
  constructor(
    private readonly browser: BrowserAssetRepository,
    private readonly providers: RemoteAssetProviderRegistry,
    private readonly options: ComposedAssetRepositoryOptions = {},
  ) {}

  private providerFor(uri: AssetUri) {
    const provider = this.providers.forUri(uri);
    if (!provider) throw new AssetError("missing", `No provider is registered for ${uri}`);
    return provider;
  }

  private context(providerKey: string, signal?: AbortSignal): AssetProviderContext {
    return this.options.context?.(providerKey, signal) ?? { signal };
  }

  importFiles(files: readonly File[], options?: AssetImportOptions): Promise<AssetRecord[]> {
    return this.browser.importFiles(files, options);
  }

  importBlob(blob: Blob, options: AssetImportBlobOptions): Promise<AssetRecord> {
    return this.browser.importBlob(blob, options);
  }

  async get(uri: AssetUri): Promise<AssetRecord | null> {
    const parsed = parseAssetUri(uri);
    if (parsed?.source !== "remote") return this.browser.get(uri);
    const cached = await this.browser.get(uri);
    if (cached) return cached;
    const provider = this.providerFor(uri);
    return provider.get(uri, this.context(provider.key));
  }

  async list(query: AssetQuery = {}): Promise<AssetPage> {
    const local = await this.browser.list(query);
    if (!query.includeRemote) return local;
    const remotePages = await Promise.all(
      this.providers.list()
        .filter((provider) => provider.capabilities.list)
        .map((provider) => provider.list(query, this.context(provider.key))),
    );
    const unique = new Map<AssetUri, AssetRecord>();
    for (const record of [...local.items, ...remotePages.flatMap((page) => page.items)]) {
      unique.set(record.uri, record);
    }
    return { items: Array.from(unique.values()) };
  }

  listUsedByScope(scope: AssetScope): Promise<AssetRecord[]> {
    return this.browser.listUsedByScope(scope);
  }

  async checkPortability(uris: readonly AssetUri[]): Promise<AssetPortabilityReport> {
    const localUris = uris.filter((uri) => parseAssetUri(uri)?.source === "local");
    const report = await this.browser.checkPortability(localUris);
    const unavailable = [...report.unavailable];
    for (const uri of uris) {
      const parsed = parseAssetUri(uri);
      if (parsed?.source === "remote" && !(await this.get(uri))) unavailable.push(uri);
    }
    return {
      portable: report.localOnly.length === 0 && unavailable.length === 0,
      localOnly: report.localOnly,
      unavailable: Array.from(new Set(unavailable)),
    };
  }

  private async ensureRemoteCached(uri: AssetUri, signal?: AbortSignal): Promise<void> {
    if (await this.browser.get(uri)) return;
    const provider = this.providerFor(uri);
    if (!provider.capabilities.download) {
      throw new AssetError("unsupported", `Provider ${provider.key} cannot download assets`);
    }
    const download = await provider.download(uri, this.context(provider.key, signal));
    await this.browser.cacheRemoteAsset(download, { signal });
  }

  async uploadLocalAsset(
    localUri: AssetUri,
    providerKey: string,
    options: { signal?: AbortSignal; scope?: AssetScope } = {},
  ): Promise<AssetUploadMapping> {
    if (parseAssetUri(localUri)?.source !== "local") {
      throw new AssetError("invalid", "Only browser-local assets can be uploaded");
    }
    const provider = this.providers.get(providerKey);
    if (!provider || !provider.capabilities.upload) {
      throw new AssetError("unsupported", `Provider ${providerKey} cannot upload assets`);
    }
    const record = await this.browser.get(localUri);
    if (!record) throw new AssetError("missing", `Asset not found: ${localUri}`);
    const blob = await this.browser.readBlob(localUri, { signal: options.signal });
    const [remote] = await provider.upload(
      [{ blob, name: record.name, mimeType: record.mimeType }],
      { ...this.context(providerKey, options.signal), scope: options.scope ?? record.scope },
    );
    const remoteIdentity = remote ? parseAssetUri(remote.uri) : null;
    if (
      !remote ||
      remoteIdentity?.source !== "remote" ||
      remoteIdentity.providerKey !== providerKey
    ) {
      throw new AssetError("corrupt", `Provider ${providerKey} returned an invalid asset record`);
    }
    await this.browser.setRemoteMapping(localUri, remote.uri);
    return { localUri, remote };
  }

  getRemoteMapping(localUri: AssetUri): Promise<AssetUri | null> {
    return this.browser.getRemoteMapping(localUri);
  }

  async readBlob(uri: AssetUri, options: AssetReadOptions = {}): Promise<Blob> {
    if (parseAssetUri(uri)?.source === "remote") {
      await this.ensureRemoteCached(uri, options.signal);
    }
    return this.browser.readBlob(uri, options);
  }

  async readStream(
    uri: AssetUri,
    options: AssetReadOptions = {},
  ): Promise<ReadableStream<Uint8Array>> {
    return (await this.readBlob(uri, options)).stream();
  }

  async readText(uri: AssetUri, options: AssetReadTextOptions = {}): Promise<string> {
    const blob = await this.readBlob(uri, options);
    return !options.encoding || options.encoding.toLowerCase() === "utf-8"
      ? blob.text()
      : new TextDecoder(options.encoding).decode(await blob.arrayBuffer());
  }

  async createObjectUrl(uri: AssetUri): Promise<ResolvedAssetUrl> {
    if (parseAssetUri(uri)?.source === "remote") await this.ensureRemoteCached(uri);
    return this.browser.createObjectUrl(uri);
  }

  rename(uri: AssetUri, name: string): Promise<AssetRecord> {
    if (parseAssetUri(uri)?.source === "remote") {
      throw new AssetError("unsupported", "Remote rename is provider-specific");
    }
    return this.browser.rename(uri, name);
  }

  async remove(uri: AssetUri, options: AssetRemoveOptions = {}): Promise<void> {
    const parsed = parseAssetUri(uri);
    if (parsed?.source !== "remote") return this.browser.remove(uri, options);
    const provider = this.providerFor(uri);
    if (!provider.delete || !provider.capabilities.delete) {
      throw new AssetError("unsupported", `Provider ${provider.key} cannot delete assets`);
    }
    await provider.delete(uri, this.context(provider.key, options.signal));
    await this.browser.removeCachedRemote(uri);
    await this.browser.removeRemoteMapping(uri);
  }

  reconcileUsage(scope: AssetScope, usages: readonly AssetUsageInput[]): Promise<void> {
    return this.browser.reconcileUsage(scope, usages);
  }

  getStorageStatus(): Promise<AssetStorageStatus> {
    return this.browser.getStorageStatus();
  }

  requestPersistentStorage(): Promise<AssetPersistenceResult> {
    return this.browser.requestPersistentStorage();
  }

  evictRemoteCache(policy?: AssetCachePolicy): Promise<AssetCacheResult> {
    return this.browser.evictRemoteCache(policy);
  }
}
