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
      upgrade(database, oldVersion) {
        if (oldVersion >= 3) return;
        for (const storeName of Array.from(database.objectStoreNames)) {
          database.deleteObjectStore(storeName);
        }

        const assets = database.createObjectStore("assets", { keyPath: "id" });
        assets.createIndex("by-created-at", "createdAt");
        assets.createIndex("by-kind", "kind");
        assets.createIndex("by-name", "name");

        database.createObjectStore("objects", { keyPath: "id" });

        const usages = database.createObjectStore("usages", { keyPath: "key" });
        usages.createIndex("by-scope", "scopeKey");
        usages.createIndex("by-uri", "uri");
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
