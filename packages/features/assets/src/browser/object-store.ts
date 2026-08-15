export interface AssetObjectStore {
  readonly backend: "opfs" | "indexeddb";
  write(objectId: string, blob: Blob, signal?: AbortSignal): Promise<void>;
  read(objectId: string, signal?: AbortSignal): Promise<Blob>;
  remove(objectId: string): Promise<void>;
  has(objectId: string): Promise<boolean>;
}

export function throwIfAborted(signal?: AbortSignal): void {
  if (signal?.aborted) {
    throw new DOMException("Asset operation was aborted", "AbortError");
  }
}
