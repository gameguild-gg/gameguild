import { SyncManager } from "../../sync/editor/sync-manager"
import { HashManager } from "../../sync/editor/hash-manager"

interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  hash?: string
  syncStatus?: "synced" | "pending" | "conflict" | "local-only"
}

interface TagData {
  id: string
  name: string
  projectIds: string[]
}

interface ProjectMetadata {
  id: string
  name: string
  tags: string[]
  size: number
  hash: string
  createdAt: string
  updatedAt: string
  syncStatus?: "synced" | "pending" | "conflict" | "local-only"
}

export class EnhancedStorageAdapter {
  private db: IDBDatabase | null = null
  private syncManager: SyncManager
  private isInitialized = false

  private readonly DB_NAME = "GGEditorDB"
  private readonly DB_VERSION = 2 // Incremented for TagData support
  private readonly STORE_NAME = "projects"
  private readonly TAGS_STORE_NAME = "tags" // Kept for migration/compatibility, can be removed later
  private readonly METADATA_STORE_NAME = "project_metadata"
  private readonly TAG_DATA_STORE_NAME = "tag_data"

  constructor() {
    this.syncManager = new SyncManager()
  }

  async init(): Promise<void> {
    if (this.isInitialized) return

    await Promise.all([this.initIndexedDB(), this.syncManager.init()])

    this.isInitialized = true
    console.log("Enhanced Storage Adapter initialized")
  }

  private async initIndexedDB(): Promise<void> {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(this.DB_NAME, this.DB_VERSION)

      request.onerror = () => reject(request.error)
      request.onsuccess = () => {
        this.db = request.result
        resolve()
      }

      request.onupgradeneeded = (event) => {
        const db = (event.target as IDBOpenDBRequest).result

        // Create projects store
        if (!db.objectStoreNames.contains(this.STORE_NAME)) {
          db.createObjectStore(this.STORE_NAME, { keyPath: "id" })
        }

        // Create tags store (legacy, can be migrated and removed)
        if (!db.objectStoreNames.contains(this.TAGS_STORE_NAME)) {
          db.createObjectStore(this.TAGS_STORE_NAME, { keyPath: "name" })
        }

        // Create metadata store for sync optimization
        if (!db.objectStoreNames.contains(this.METADATA_STORE_NAME)) {
          const metadataStore = db.createObjectStore(this.METADATA_STORE_NAME, { keyPath: "id" })
          metadataStore.createIndex("hash", "hash", { unique: false })
        }

        // Create new tag_data store
        if (!db.objectStoreNames.contains(this.TAG_DATA_STORE_NAME)) {
          const tagDataStore = db.createObjectStore(this.TAG_DATA_STORE_NAME, { keyPath: "id" })
          tagDataStore.createIndex("name", "name", { unique: true })
        }
      }
    })
  }

  async save(id: string, name: string, data: string, tags: string[] = []): Promise<void> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    const hash = await HashManager.generateHash(data)
    const now = new Date().toISOString()

    // Check if project exists to preserve createdAt and get old tags
    const existing = await this.loadFromIndexedDB(id)
    const oldTags = existing ? existing.tags : []

    const projectData: ProjectData = {
      id,
      name,
      data,
      tags,
      size: this.estimateSize(data),
      hash,
      createdAt: existing ? existing.createdAt : now,
      updatedAt: now,
      syncStatus: "pending",
    }

    // Save to IndexedDB
    await this.saveToIndexedDB(projectData)

    // Update tag relationships
    await this.updateTagProjectRelationships(id, oldTags, tags)

    // Queue for sync
    await this.syncManager.queueProjectUpdate(projectData)

    console.log(`Saved project "${name}" (${id}) - Size: ${this.formatSize(projectData.size)}`)
  }

  private async saveToIndexedDB(projectData: ProjectData): Promise<void> {
    if (!this.db) throw new Error("IndexedDB not initialized")

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.STORE_NAME, this.METADATA_STORE_NAME], "readwrite")

      // Save full project data
      const projectStore = transaction.objectStore(this.STORE_NAME)
      projectStore.put(projectData)

      // Save metadata for sync optimization
      const metadataStore = transaction.objectStore(this.METADATA_STORE_NAME)
      const metadata: ProjectMetadata = {
        id: projectData.id,
        name: projectData.name,
        tags: projectData.tags,
        size: projectData.size,
        hash: projectData.hash!,
        createdAt: projectData.createdAt,
        updatedAt: projectData.updatedAt,
        syncStatus: projectData.syncStatus,
      }
      metadataStore.put(metadata)

      transaction.oncomplete = () => resolve()
      transaction.onerror = () => reject(transaction.error)
    })
  }

  async load(id: string): Promise<ProjectData | null> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    // First try to load from local IndexedDB
    const localProject = await this.loadFromIndexedDB(id)

    if (!localProject) {
      // Project not found locally, try to download from server
      console.log(`Project ${id} not found locally, attempting download`)
      const serverProject = await this.syncManager.downloadProject(id)

      if (serverProject) {
        // Save downloaded project locally
        await this.saveToIndexedDB({
          ...serverProject,
          syncStatus: "synced",
        })
        return serverProject
      }

      return null
    }

    // Check if local project needs sync with server
    const syncedProject = await this.syncManager.syncProjectIfNeeded(localProject)
    if (syncedProject) {
      // Update local project with server version
      await this.saveToIndexedDB({
        ...syncedProject,
        syncStatus: "synced",
      })
      return syncedProject
    }

    return localProject
  }

  private async loadFromIndexedDB(id: string): Promise<ProjectData | null> {
    if (!this.db) throw new Error("IndexedDB not initialized")

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.STORE_NAME], "readonly")
      const store = transaction.objectStore(this.STORE_NAME)
      const request = store.get(id)

      request.onsuccess = () => resolve(request.result || null)
      request.onerror = () => reject(request.error)
    })
  }

  async delete(id: string): Promise<void> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    // Get project before deleting to know its tags
    const projectToDelete = await this.loadFromIndexedDB(id)
    const tagsToRemove = projectToDelete ? projectToDelete.tags : []

    // Delete from IndexedDB
    await this.deleteFromIndexedDB(id)

    // Update tag relationships
    if (tagsToRemove.length > 0) {
      await this.updateTagProjectRelationships(id, tagsToRemove, [])
    }

    // Queue for sync
    await this.syncManager.queueProjectDelete(id)

    console.log(`Deleted project ${id}`)
  }

  private async deleteFromIndexedDB(id: string): Promise<void> {
    if (!this.db) throw new Error("IndexedDB not initialized")

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.STORE_NAME, this.METADATA_STORE_NAME], "readwrite")

      const projectStore = transaction.objectStore(this.STORE_NAME)
      projectStore.delete(id)

      const metadataStore = transaction.objectStore(this.METADATA_STORE_NAME)
      metadataStore.delete(id)

      transaction.oncomplete = () => resolve()
      transaction.onerror = () => reject(transaction.error)
    })
  }

  async list(): Promise<ProjectData[]> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    // Get local projects
    const localProjects = await this.listFromIndexedDB()

    // Fetch server metadata in parallel (non-blocking)
    this.syncServerMetadata().catch((error) => {
      console.error("Failed to sync server metadata:", error)
    })

    return localProjects
  }

  private async listFromIndexedDB(): Promise<ProjectData[]> {
    if (!this.db) throw new Error("IndexedDB not initialized")

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.STORE_NAME], "readonly")
      const store = transaction.objectStore(this.STORE_NAME)
      const request = store.getAll()

      request.onsuccess = () => {
        const projects = request.result as ProjectData[]
        projects.sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
        resolve(projects)
      }
      request.onerror = () => reject(request.error)
    })
  }

  private async syncServerMetadata(): Promise<void> {
    try {
      const serverMetadata = await this.syncManager.fetchServerProjectsMetadata()

      // Compare with local metadata and identify projects that need sync
      for (const serverMeta of serverMetadata) {
        const localMeta = await this.getLocalMetadata(serverMeta.id)
        if (!localMeta || localMeta.hash !== serverMeta.hash) {
          // Mark for sync or update status
          // This logic can be expanded based on sync strategy
        }
      }
    } catch (error) {
      console.error("Failed to sync server metadata:", error)
    }
  }

  private async getLocalMetadata(id: string): Promise<ProjectMetadata | null> {
    if (!this.db) return null

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.METADATA_STORE_NAME], "readonly")
      const store = transaction.objectStore(this.METADATA_STORE_NAME)
      const request = store.get(id)

      request.onsuccess = () => resolve(request.result || null)
      request.onerror = () => reject(request.error)
    })
  }

  async searchProjects(
    searchTerm: string,
    tags: string[],
    filterMode: "all" | "any" = "any",
  ): Promise<ProjectData[]> {
    if (!this.db) throw new Error("IndexedDB not initialized")

    let projectIdsToLoad: Set<string> | null = null

    // 1. Filter by tags using the tag_data store for efficiency
    if (tags.length > 0) {
      const transaction = this.db.transaction([this.TAG_DATA_STORE_NAME], "readonly")
      const store = transaction.objectStore(this.TAG_DATA_STORE_NAME)
      const nameIndex = store.index("name")

      const tagPromises = tags.map(
        (tagName) =>
          new Promise<string[]>((resolve, reject) => {
            const request = nameIndex.get(tagName)
            request.onsuccess = () => {
              const tagData: TagData = request.result
              resolve(tagData ? tagData.projectIds : [])
            }
            request.onerror = () => reject(request.error)
          }),
      )

      const projectIdsByTag = await Promise.all(tagPromises)
      projectIdsToLoad = new Set<string>()

      if (filterMode === "any") {
        // Union of all project IDs
        for (const ids of projectIdsByTag) {
          for (const id of ids) {
            projectIdsToLoad.add(id)
          }
        }
      } else {
        // Intersection of all project IDs
        if (projectIdsByTag.length > 0) {
          const firstSet = new Set(projectIdsByTag[0])
          for (let i = 1; i < projectIdsByTag.length; i++) {
            const currentSet = new Set(projectIdsByTag[i])
            for (const id of Array.from(firstSet)) {
              if (!currentSet.has(id)) {
                firstSet.delete(id)
              }
            }
          }
          projectIdsToLoad = firstSet
        }
      }
    }

    // 2. Load projects
    // If filtered by tags, load only those projects. Otherwise, load all.
    const projectsToFilter = await this.getProjectsByIds(projectIdsToLoad ? Array.from(projectIdsToLoad) : null)

    // 3. Filter by search term
    if (searchTerm) {
      const lowerCaseSearchTerm = searchTerm.toLowerCase()
      return projectsToFilter.filter((project) => project.name.toLowerCase().includes(lowerCaseSearchTerm))
    }

    return projectsToFilter
  }

  private async getProjectsByIds(ids: string[] | null): Promise<ProjectData[]> {
    if (!this.db) throw new Error("IndexedDB not initialized")

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.STORE_NAME], "readonly")
      const store = transaction.objectStore(this.STORE_NAME)
      const projects: ProjectData[] = []

      if (ids === null) {
        // No IDs provided, fetch all projects (fallback to list)
        return this.listFromIndexedDB().then(resolve).catch(reject)
      }

      if (ids.length === 0) {
        // If IDs array is empty (e.g., from tag intersection), return empty
        return resolve([])
      }

      const idSet = new Set(ids)
      let cursorReq = store.openCursor()

      cursorReq.onsuccess = (event) => {
        const cursor = (event.target as IDBRequest<IDBCursorWithValue>).result
        if (cursor) {
          if (idSet.has(cursor.value.id)) {
            projects.push(cursor.value)
          }
          cursor.continue()
        } else {
          // Sort to maintain consistency with list()
          projects.sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
          resolve(projects)
        }
      }
      cursorReq.onerror = () => reject(cursorReq.error)
    })
  }

  // New Tag Management System
  private async updateTagProjectRelationships(projectId: string, oldTags: string[], newTags: string[]): Promise<void> {
    const oldTagSet = new Set(oldTags)
    const newTagSet = new Set(newTags)

    const tagsAdded = newTags.filter((tag) => !oldTagSet.has(tag))
    const tagsRemoved = oldTags.filter((tag) => !newTagSet.has(tag))

    if (!this.db) throw new Error("IndexedDB not initialized")

    const transaction = this.db.transaction([this.TAG_DATA_STORE_NAME], "readwrite")
    const store = transaction.objectStore(this.TAG_DATA_STORE_NAME)
    const nameIndex = store.index("name")

    const processTags = async (tagNames: string[], action: "add" | "remove") => {
      for (const tagName of tagNames) {
        const tagData = await new Promise<TagData | undefined>((res) => {
          const req = nameIndex.get(tagName)
          req.onsuccess = () => res(req.result)
          req.onerror = () => res(undefined) // Fail silently on lookup
        })

        if (action === "add") {
          if (tagData) {
            // Add project to existing tag
            if (!tagData.projectIds.includes(projectId)) {
              tagData.projectIds.push(projectId)
              store.put(tagData)
            }
          } else {
            // Create new tag
            const newTagData: TagData = {
              id: crypto.randomUUID(),
              name: tagName,
              projectIds: [projectId],
            }
            store.add(newTagData)
          }
        } else if (action === "remove") {
          if (tagData) {
            // Remove project from existing tag
            const index = tagData.projectIds.indexOf(projectId)
            if (index > -1) {
              tagData.projectIds.splice(index, 1)
              // If no projects are associated, we could delete it, but for now, we keep it
              store.put(tagData)
            }
          }
        }
      }
    }

    await processTags(tagsAdded, "add")
    await processTags(tagsRemoved, "remove")

    return new Promise((resolve, reject) => {
      transaction.oncomplete = () => resolve()
      transaction.onerror = () => reject(transaction.error)
    })
  }

  async getAllTags(): Promise<Array<{ name: string; usageCount: number }>> {
    if (!this.db) throw new Error("IndexedDB not initialized")

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.TAG_DATA_STORE_NAME], "readonly")
      const store = transaction.objectStore(this.TAG_DATA_STORE_NAME)
      const request = store.getAll()

      request.onsuccess = () => {
        const allTagsData: TagData[] = request.result
        const tagsWithCount = allTagsData.map((tagData) => ({
          name: tagData.name,
          usageCount: tagData.projectIds.length,
        }))
        // Sort by usage count descending, then alphabetically
        tagsWithCount.sort((a, b) => {
          if (b.usageCount !== a.usageCount) {
            return b.usageCount - a.usageCount
          }
          return a.name.localeCompare(b.name)
        })
        resolve(tagsWithCount)
      }
      request.onerror = () => reject(request.error)
    })
  }

  // Storage info
  async getStorageInfo(): Promise<{ totalSize: number; projectCount: number }> {
    const projects = await this.listFromIndexedDB()
    let totalSize = 0

    projects.forEach((project) => {
      totalSize += project.size || this.estimateSize(project.data)
    })

    return { totalSize, projectCount: projects.length }
  }

  async getProjectInfo(id: string): Promise<{ size: number; createdAt: string; updatedAt: string } | null> {
    const project = await this.loadFromIndexedDB(id)

    if (project) {
      return {
        size: project.size || this.estimateSize(project.data),
        createdAt: project.createdAt,
        updatedAt: project.updatedAt,
      }
    }

    return null
  }

  // Sync management
  async getSyncStats() {
    return await this.syncManager.getSyncStats()
  }

  async retryFailedSync(): Promise<void> {
    await this.syncManager.retryFailedItems()
  }

  onSyncStart(callback: () => void): void {
    this.syncManager.onSyncStart(callback)
  }

  onSyncComplete(callback: (stats: any) => void): void {
    this.syncManager.onSyncComplete(callback)
  }

  onSyncError(callback: (error: Error) => void): void {
    this.syncManager.onSyncError(callback)
  }

  // Utility methods
  private estimateSize(data: string): number {
    return new Blob([data]).size / 1024
  }

  private formatSize(sizeInKB: number): string {
    if (sizeInKB < 1024) {
      return `${sizeInKB.toFixed(1)}KB`
    } else {
      return `${(sizeInKB / 1024).toFixed(1)}MB`
    }
  }

  destroy(): void {
    this.syncManager.destroy()
  }
}


