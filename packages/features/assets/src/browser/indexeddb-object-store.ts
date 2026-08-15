import type { IDBPDatabase } from "idb";
import { AssetError } from "../core/asset-errors";
import type { AssetDatabaseSchema } from "./browser-storage-schema";
import { throwIfAborted, type AssetObjectStore } from "./object-store";

export class IndexedDbAssetObjectStore implements AssetObjectStore {
  readonly backend = "indexeddb" as const;

  constructor(private readonly database: IDBPDatabase<AssetDatabaseSchema>) {}

  async write(objectId: string, blob: Blob, signal?: AbortSignal): Promise<void> {
    throwIfAborted(signal);
    await this.database.put("fallbackObjects", blob, objectId);
    throwIfAborted(signal);
  }

  async read(objectId: string, signal?: AbortSignal): Promise<Blob> {
    throwIfAborted(signal);
    const blob = await this.database.get("fallbackObjects", objectId);
    throwIfAborted(signal);
    if (!blob) throw new AssetError("missing", `Stored object not found: ${objectId}`);
    return blob;
  }

  async remove(objectId: string): Promise<void> {
    await this.database.delete("fallbackObjects", objectId);
  }

  async has(objectId: string): Promise<boolean> {
    return (await this.database.count("fallbackObjects", objectId)) > 0;
  }
}
