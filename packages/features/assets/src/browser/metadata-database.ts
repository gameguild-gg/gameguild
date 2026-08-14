import { openDB, type IDBPDatabase } from "idb";
import {
  ASSET_DATABASE_NAME,
  ASSET_DATABASE_VERSION,
  type AssetDatabaseSchema,
} from "./browser-storage-schema";

let databasePromise: Promise<IDBPDatabase<AssetDatabaseSchema>> | undefined;

export function openAssetDatabase(): Promise<IDBPDatabase<AssetDatabaseSchema>> {
  databasePromise ??= openDB<AssetDatabaseSchema>(
    ASSET_DATABASE_NAME,
    ASSET_DATABASE_VERSION,
    {
      upgrade(database) {
        const assets = database.createObjectStore("assets", { keyPath: "id" });
        assets.createIndex("by-created-at", "createdAt");
        assets.createIndex("by-kind", "kind");
        assets.createIndex("by-name", "name");

        database.createObjectStore("objects", { keyPath: "id" });
        database.createObjectStore("fallbackObjects");

        const usages = database.createObjectStore("usages", { keyPath: "key" });
        usages.createIndex("by-scope", "scopeKey");
        usages.createIndex("by-uri", "uri");

        const remoteCache = database.createObjectStore("remoteCache", {
          keyPath: "uri",
        });
        remoteCache.createIndex("by-last-accessed-at", "lastAccessedAt");

        database.createObjectStore("journal", { keyPath: "id" });
        database.createObjectStore("settings");
      },
      blocking() {
        databasePromise?.then((database) => database.close());
        databasePromise = undefined;
      },
      terminated() {
        databasePromise = undefined;
      },
    },
  );
  return databasePromise;
}

export function resetAssetDatabaseConnectionForTests(): void {
  databasePromise = undefined;
}
