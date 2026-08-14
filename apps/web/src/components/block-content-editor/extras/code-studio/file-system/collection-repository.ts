import { openDB, type DBSchema, type IDBPDatabase } from "idb"
import type {
  CollectionManifest,
  CollectionMetadata,
  SaveCollectionParams,
} from "./collection-types"

interface CodeStudioDatabase extends DBSchema {
  collections: {
    key: string
    value: CollectionManifest
    indexes: { "by-updated": number }
  }
}

let databasePromise: Promise<IDBPDatabase<CodeStudioDatabase>> | undefined

function database() {
  databasePromise ??= openDB<CodeStudioDatabase>("game-guild-code-studio", 1, {
    upgrade(db) {
      const collections = db.createObjectStore("collections", {
        keyPath: "metadata.id",
      })
      collections.createIndex("by-updated", "metadata.updated")
    },
  })
  return databasePromise
}

export const collectionRepository = {
  async save(params: SaveCollectionParams): Promise<CollectionManifest> {
    const now = Date.now()
    const metadata: CollectionMetadata = {
      id: crypto.randomUUID(),
      name: params.name.trim(),
      description: params.description,
      tags: params.tags,
      author: params.author,
      created: now,
      updated: now,
    }
    if (!metadata.name) throw new Error("Collection name cannot be empty")
    const manifest: CollectionManifest = {
      type: "collection",
      metadata,
      structure: structuredClone(params.structure),
    }
    await (await database()).add("collections", manifest)
    return manifest
  },

  async list(): Promise<CollectionMetadata[]> {
    const manifests = await (await database()).getAll("collections")
    return manifests
      .map(({ metadata }) => metadata)
      .sort((a, b) => b.updated - a.updated)
  },

  async get(id: string): Promise<CollectionManifest | null> {
    return (await (await database()).get("collections", id)) ?? null
  },

  async remove(id: string): Promise<void> {
    await (await database()).delete("collections", id)
  },

  async rename(id: string, name: string): Promise<boolean> {
    const db = await database()
    const manifest = await db.get("collections", id)
    const normalized = name.trim()
    if (!manifest || !normalized) return false
    manifest.metadata.name = normalized
    manifest.metadata.updated = Date.now()
    await db.put("collections", manifest)
    return true
  },
}
