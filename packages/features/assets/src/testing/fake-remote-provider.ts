import type { AssetPage, AssetQuery, AssetRecord } from "../core/asset-contracts";
import { createRemoteAssetUri, type AssetUri } from "../core/asset-uri";
import { classifyAssetKind, inferMimeType } from "../core/mime";
import type {
  AssetDownload,
  AssetProviderContext,
  AssetUploadInput,
  RemoteAssetProvider,
} from "../providers/remote-asset-provider";

export class FakeRemoteAssetProvider implements RemoteAssetProvider {
  readonly key = "fake";
  readonly capabilities = {
    upload: true,
    list: true,
    download: true,
    delete: true,
  } as const;

  private readonly values = new Map<AssetUri, AssetDownload>();

  async upload(
    files: readonly AssetUploadInput[],
    context: AssetProviderContext,
  ): Promise<AssetRecord[]> {
    if (context.signal?.aborted) throw new DOMException("Aborted", "AbortError");
    return files.map((file) => {
      const id = crypto.randomUUID();
      const uri = createRemoteAssetUri(this.key, id);
      const now = new Date().toISOString();
      const mimeType = inferMimeType(file.name, file.mimeType || file.blob.type);
      const record: AssetRecord = {
        id,
        uri,
        name: file.name,
        kind: classifyAssetKind(mimeType, file.name),
        mimeType,
        size: file.blob.size,
        availability: "remote",
        createdAt: now,
        updatedAt: now,
        source: { type: "remote", value: this.key },
        scope: context.scope,
      };
      this.values.set(uri, { blob: file.blob, record });
      return structuredClone(record);
    });
  }

  async get(uri: AssetUri): Promise<AssetRecord | null> {
    return this.values.get(uri)?.record ?? null;
  }

  async list(query: AssetQuery): Promise<AssetPage> {
    const search = query.search?.trim().toLocaleLowerCase();
    const items = Array.from(this.values.values(), ({ record }) => record).filter((record) => {
      if (search && !record.name.toLocaleLowerCase().includes(search)) return false;
      if (query.kinds?.length && !query.kinds.includes(record.kind)) return false;
      if (query.scope &&
        (record.scope?.type !== query.scope.type || record.scope.id !== query.scope.id)) return false;
      return true;
    });
    return { items: structuredClone(items) };
  }

  async download(uri: AssetUri): Promise<AssetDownload> {
    const value = this.values.get(uri);
    if (!value) throw new Error(`Remote asset not found: ${uri}`);
    return value;
  }

  async delete(uri: AssetUri): Promise<void> {
    this.values.delete(uri);
  }

  seed(uri: AssetUri, download: AssetDownload): void {
    this.values.set(uri, download);
  }
}
