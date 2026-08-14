import { AssetError } from "../core/asset-errors";
import { throwIfAborted, type AssetObjectStore } from "./object-store";

interface StorageManagerWithDirectory extends StorageManager {
  getDirectory(): Promise<FileSystemDirectoryHandle>;
}

function storageManager(): StorageManagerWithDirectory | null {
  if (typeof navigator === "undefined") return null;
  const storage = navigator.storage as Partial<StorageManagerWithDirectory>;
  return typeof storage?.getDirectory === "function"
    ? (storage as StorageManagerWithDirectory)
    : null;
}

export class OpfsAssetObjectStore implements AssetObjectStore {
  readonly backend = "opfs" as const;

  static isSupported(): boolean {
    return storageManager() !== null;
  }

  private async objectDirectory(
    objectId: string,
    create: boolean,
  ): Promise<{ directory: FileSystemDirectoryHandle; fileName: string }> {
    const manager = storageManager();
    if (!manager) throw new AssetError("unsupported", "OPFS is unavailable");
    const root = await manager.getDirectory();
    const objects = await root.getDirectoryHandle("objects", { create });
    const normalized = objectId.replace(/^sha256:/, "");
    const prefix = normalized.slice(0, 2) || "00";
    const directory = await objects.getDirectoryHandle(prefix, { create });
    return { directory, fileName: normalized };
  }

  async write(objectId: string, blob: Blob, signal?: AbortSignal): Promise<void> {
    throwIfAborted(signal);
    const { directory, fileName } = await this.objectDirectory(objectId, true);
    const handle = await directory.getFileHandle(fileName, { create: true });
    const writable = await handle.createWritable();
    try {
      throwIfAborted(signal);
      await writable.write(blob);
      throwIfAborted(signal);
      await writable.close();
    } catch (error) {
      await writable.abort(error).catch(() => undefined);
      throw error;
    }
  }

  async read(objectId: string, signal?: AbortSignal): Promise<Blob> {
    throwIfAborted(signal);
    try {
      const { directory, fileName } = await this.objectDirectory(objectId, false);
      const handle = await directory.getFileHandle(fileName);
      const file = await handle.getFile();
      throwIfAborted(signal);
      return file;
    } catch (error) {
      if (error instanceof DOMException && error.name === "NotFoundError") {
        throw new AssetError("missing", `Stored object not found: ${objectId}`, {
          cause: error,
        });
      }
      throw error;
    }
  }

  async remove(objectId: string): Promise<void> {
    try {
      const { directory, fileName } = await this.objectDirectory(objectId, false);
      await directory.removeEntry(fileName);
    } catch (error) {
      if (!(error instanceof DOMException && error.name === "NotFoundError")) {
        throw error;
      }
    }
  }

  async has(objectId: string): Promise<boolean> {
    try {
      const { directory, fileName } = await this.objectDirectory(objectId, false);
      await directory.getFileHandle(fileName);
      return true;
    } catch (error) {
      if (error instanceof DOMException && error.name === "NotFoundError") return false;
      throw error;
    }
  }
}
