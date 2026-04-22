import { SyncManager } from "../../sync/editor/sync-manager"
import { GoogleDriveSync } from "../../sync/editor/google-drive-sync"
import { HashManager } from "../../sync/editor/hash-manager"
import { getHistoryManager, type CommitInfo, type SnapshotInfo } from "../git"
import type { ProjectPreferences } from "./project-preferences"
import { type EngineType, ENGINE_TYPES } from "./project-types"
import { type StorageType, STORAGE_TYPES, type SyncStatus, SYNC_STATUS } from "./storage-types"

export type { ProjectPreferences } from "./project-preferences"
export type { StorageType, SyncStatus } from "./storage-types"
export type { CommitInfo, SnapshotInfo } from "../git"

export interface ProjectData {
  id: string
  name: string
  engine?: EngineType // Engine type: "lexical" or "blocks"
  data: string // Serialized project data
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  hash: string
  syncStatus?: SyncStatus
  storageType: StorageType
  isLocallyAvailable?: boolean // Computed dynamically based on local storage check
  preferences?: ProjectPreferences // Project-level preferences
}

interface TagData {
  id: string
  name: string
  projectIds: string[]
}

interface ProjectMetadata {
  id: string
  name: string
  engine?: EngineType
  tags: string[]
  size: number
  hash: string
  createdAt: string
  updatedAt: string
  syncStatus?: SyncStatus
  storageType: StorageType
  preferences?: ProjectPreferences
}

export class EnhancedStorageAdapter {
  private db: IDBDatabase | null = null
  private syncManager: SyncManager
  private googleDriveSync: GoogleDriveSync
  private isInitialized = false

  private readonly DB_NAME = "GGEditorDB"
  private readonly DB_VERSION = 3 // Incremented for preferences support
  private readonly STORE_NAME = "projects"
  private readonly TAGS_STORE_NAME = "tags" // Kept for migration/compatibility, can be removed later
  private readonly METADATA_STORE_NAME = "project_metadata"
  private readonly TAG_DATA_STORE_NAME = "tag_data"

  constructor() {
    this.syncManager = new SyncManager()
    this.googleDriveSync = new GoogleDriveSync()
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
        const oldVersion = event.oldVersion

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

        // Migration for storageType field (version 2 -> 3)
        if (oldVersion < 3) {
          // This migration will run after the stores are created
          event.target!.addEventListener('success', () => {
            this.migrateToStorageType()
          })
        }
      }
    })
  }

  private async migrateToStorageType(): Promise<void> {
    if (!this.db) return

    try {
      const transaction = this.db.transaction([this.STORE_NAME, this.METADATA_STORE_NAME], "readwrite")
      const projectStore = transaction.objectStore(this.STORE_NAME)
      const metadataStore = transaction.objectStore(this.METADATA_STORE_NAME)

      // Get all projects
      const projectsRequest = projectStore.getAll()
      projectsRequest.onsuccess = () => {
        const projects = projectsRequest.result as (ProjectData & { storageType?: string })[]
        
        projects.forEach(project => {
          // Add storageType if it doesn't exist
          if (!project.storageType) {
            const updatedProject: ProjectData = {
              ...project,
              storageType: STORAGE_TYPES.LOCAL, // Default to local for existing projects
            }
            projectStore.put(updatedProject)

            // Update metadata as well
            const metadata: ProjectMetadata = {
              id: project.id,
              name: project.name,
              tags: project.tags,
              size: project.size,
              hash: project.hash || "",
              createdAt: project.createdAt,
              updatedAt: project.updatedAt,
              syncStatus: project.syncStatus,
              storageType: STORAGE_TYPES.LOCAL,
            }
            metadataStore.put(metadata)
          }
        })
      }

      transaction.oncomplete = () => {
        console.log("Migration to storageType completed successfully")
      }

      transaction.onerror = () => {
        console.error("Migration to storageType failed:", transaction.error)
      }
    } catch (error) {
      console.error("Failed to migrate to storageType:", error)
    }
  }

  async save(id: string, name: string, data: string, tags: string[] = [], storageType: StorageType = STORAGE_TYPES.LOCAL, preferences?: ProjectPreferences, engine?: EngineType): Promise<void> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    const hash = await HashManager.generateHash(data)
    const now = new Date().toISOString()

    // Check if project exists to preserve createdAt, old tags, and preferences
    const existing = await this.loadFromIndexedDB(id)
    const oldTags = existing ? existing.tags : []

    const projectData: ProjectData = {
      id,
      name,
      engine: engine ?? existing?.engine ?? ENGINE_TYPES.LEXICAL, // Preserve existing engine or use provided
      data,
      tags,
      size: this.estimateSize(data),
      hash,
      createdAt: existing ? existing.createdAt : now,
      updatedAt: now,
      syncStatus: SYNC_STATUS.PENDING,
      storageType,
      preferences: preferences || existing?.preferences, // Preserve existing preferences if not provided
    }

    // Save to IndexedDB
    await this.saveToIndexedDB(projectData)

    // Update tag relationships
    await this.updateTagProjectRelationships(id, oldTags, tags)

    // Handle sync based on storage type
    if (storageType === STORAGE_TYPES.GOOGLE_DRIVE) {
      // Sync to Google Drive
      console.log("Attempting Google Drive sync for project:", name)
      console.log("GoogleDriveService isReady:", this.googleDriveSync ? "GoogleDriveSync initialized" : "GoogleDriveSync NOT initialized")
      
      try {
        const syncResult = await this.googleDriveSync.syncToGoogleDrive(projectData)
        if (syncResult.success) {
          console.log("Google Drive sync successful for project:", name)
          // Update sync status to synced
          projectData.syncStatus = SYNC_STATUS.SYNCED
          await this.saveToIndexedDB(projectData)
        } else {
          console.error("Google Drive sync failed:", syncResult.error)
          // Keep as pending for retry
        }
      } catch (error) {
        console.error("Google Drive sync error:", error)
        // Keep as pending for retry
      }
    } else if (storageType === STORAGE_TYPES.GAMEGUILD_CLOUD) {
      // Queue for GameGuild cloud sync
      await this.syncManager.queueProjectUpdate(projectData)
    }
    // For local storage, no additional sync needed

    // Auto-commit to Git for history tracking
    try {
      const historyManager = getHistoryManager()
      // Commit the full project data object (not just the data field)
      // This ensures we can restore complete project state from history
      await historyManager.commitProject(id, JSON.stringify(projectData))
    } catch (error) {
      console.warn("Git commit failed (non-blocking):", error)
      // Git commit failure should not block the save operation
    }

    console.log(`Saved project "${name}" (${id}) to ${storageType} - Size: ${this.formatSize(projectData.size)}`)
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
        engine: projectData.engine,
        tags: projectData.tags,
        size: projectData.size,
        hash: projectData.hash!,
        createdAt: projectData.createdAt,
        updatedAt: projectData.updatedAt,
        syncStatus: projectData.syncStatus,
        storageType: projectData.storageType,
        preferences: projectData.preferences,
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
      // Project not found locally, try to download based on storage preferences
      console.log(`Project ${id} not found locally, attempting download`)
      
      // Try Google Drive first (if available)
      if (await this.googleDriveSync.isGoogleDriveAvailable()) {
        const googleDriveProject = await this.googleDriveSync.loadFromGoogleDrive(id)
        if (googleDriveProject) {
          // Ensure hash exists for the project
          const hash = googleDriveProject.hash || await HashManager.generateHash(googleDriveProject.data)
          
          // Save downloaded project locally
          await this.saveToIndexedDB({
            ...googleDriveProject,
            hash,
            syncStatus: SYNC_STATUS.SYNCED,
            storageType: STORAGE_TYPES.GOOGLE_DRIVE,
          })
          return {
            ...googleDriveProject,
            hash,
            storageType: STORAGE_TYPES.GOOGLE_DRIVE,
          }
        }
      }
      
      // Try GameGuild cloud server
      const serverProject = await this.syncManager.downloadProject(id)
      if (serverProject) {
        // Ensure hash exists for the project
        const hash = serverProject.hash || await HashManager.generateHash(serverProject.data)
        
        // Save downloaded project locally
        await this.saveToIndexedDB({
          ...serverProject,
          hash,
          syncStatus: SYNC_STATUS.SYNCED,
          storageType: STORAGE_TYPES.GAMEGUILD_CLOUD, // Server projects are cloud-based
        })
        return {
          ...serverProject,
          hash,
          storageType: STORAGE_TYPES.GAMEGUILD_CLOUD,
        }
      }

      return null
    }

    // Handle sync based on storage type
    if (localProject.storageType === STORAGE_TYPES.GOOGLE_DRIVE) {
      // For Google Drive projects, always check if we have the latest version
      if (await this.googleDriveSync.isGoogleDriveAvailable()) {
        try {
          const googleDriveProject = await this.googleDriveSync.loadFromGoogleDrive(id)
          if (googleDriveProject && googleDriveProject.updatedAt > localProject.updatedAt) {
            // Ensure hash exists for the project
            const hash = googleDriveProject.hash || await HashManager.generateHash(googleDriveProject.data)
            
            // Update local copy with newer version from Google Drive
            await this.saveToIndexedDB({
              ...googleDriveProject,
              hash,
              syncStatus: SYNC_STATUS.SYNCED,
              storageType: STORAGE_TYPES.GOOGLE_DRIVE,
            })
            return {
              ...googleDriveProject,
              hash,
              storageType: STORAGE_TYPES.GOOGLE_DRIVE,
            }
          }
        } catch (error) {
          console.error("Failed to sync from Google Drive:", error)
          // Continue with local version
        }
      }
    } else if (localProject.storageType === STORAGE_TYPES.GAMEGUILD_CLOUD) {
      // Check if local project needs sync with server
      const syncedProject = await this.syncManager.syncProjectIfNeeded(localProject)
      if (syncedProject) {
        // Ensure hash exists for the project
        const hash = syncedProject.hash || await HashManager.generateHash(syncedProject.data)
        
        // Update local project with server version, preserving storage type
        await this.saveToIndexedDB({
          ...syncedProject,
          hash,
          syncStatus: SYNC_STATUS.SYNCED,
          storageType: localProject.storageType,
        })
        return {
          ...syncedProject,
          hash,
          storageType: localProject.storageType,
        }
      }
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

    // Delete Git history
    try {
      const historyManager = getHistoryManager()
      await historyManager.deleteProjectRepo(id)
    } catch (error) {
      console.warn("Failed to delete Git repo (non-blocking):", error)
    }

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
    
    // Get Google Drive projects metadata efficiently (if authenticated)
    let googleDriveProjects: ProjectData[] = []
    if (await this.googleDriveSync.isGoogleDriveAvailable()) {
      try {
        googleDriveProjects = await this.googleDriveSync.listFromGoogleDrive()
        console.log(`Found ${googleDriveProjects.length} Google Drive projects`)
      } catch (error) {
        console.error("Failed to list Google Drive projects:", error)
      }
    }

    // Efficiently determine which projects are locally available
    const localGoogleDriveProjectIds = new Set(
      localProjects
        .filter(p => p.storageType === STORAGE_TYPES.GOOGLE_DRIVE)
        .map(p => p.id)
    )
    
    // Mark Google Drive projects as locally available or not
    const googleDriveProjectsWithAvailability = googleDriveProjects.map(project => ({
      ...project,
      isLocallyAvailable: localGoogleDriveProjectIds.has(project.id)
    }))
    
    // Mark local projects as locally available
    const localProjectsMarked = localProjects.map(project => ({
      ...project,
      isLocallyAvailable: true
    }))
    
    // Merge projects, removing duplicates (prioritize local versions for actual data)
    const localProjectIds = new Set(localProjects.map(p => p.id))
    const uniqueGoogleDriveProjects = googleDriveProjectsWithAvailability
      .filter(p => !localProjectIds.has(p.id))
    
    const allProjects = [...localProjectsMarked, ...uniqueGoogleDriveProjects]
    
    // Sort by last updated
    allProjects.sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())

    // Fetch server metadata in parallel (non-blocking)
    this.syncServerMetadata().catch((error) => {
      console.error("Failed to sync server metadata:", error)
    })

    return allProjects
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
    storageTypeFilter?: StorageType,
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
    const allProjects = await this.list() // Use list() method which includes Google Drive projects
    const projectsToFilter = projectIdsToLoad 
      ? allProjects.filter(project => projectIdsToLoad!.has(project.id))
      : allProjects

    // 3. Filter by storage type
    let filteredByStorage = projectsToFilter
    if (storageTypeFilter) {
      filteredByStorage = projectsToFilter.filter((project) => project.storageType === storageTypeFilter)
    }

    // 4. Filter by search term
    if (searchTerm) {
      const lowerCaseSearchTerm = searchTerm.toLowerCase()
      return filteredByStorage.filter((project) => project.name.toLowerCase().includes(lowerCaseSearchTerm))
    }

    return filteredByStorage
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

  // Storage type management
  async updateProjectStorageType(id: string, storageType: StorageType): Promise<void> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    const project = await this.loadFromIndexedDB(id)
    if (!project) {
      throw new Error(`Project ${id} not found`)
    }

    const updatedProject: ProjectData = {
      ...project,
      storageType,
      updatedAt: new Date().toISOString(),
      syncStatus: SYNC_STATUS.PENDING,
    }

    await this.saveToIndexedDB(updatedProject)
    await this.syncManager.queueProjectUpdate(updatedProject)

    console.log(`Updated storage type for project "${project.name}" to: ${storageType}`)
  }

  async getProjectsByStorageType(storageType: StorageType): Promise<ProjectData[]> {
    const allProjects = await this.listFromIndexedDB()
    return allProjects.filter((project) => project.storageType === storageType)
  }

  // Project preferences management
  async updateProjectPreferences(id: string, preferences: ProjectPreferences): Promise<void> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    const project = await this.loadFromIndexedDB(id)
    if (!project) {
      throw new Error(`Project ${id} not found`)
    }

    const updatedProject: ProjectData = {
      ...project,
      preferences,
      updatedAt: new Date().toISOString(),
      syncStatus: SYNC_STATUS.PENDING,
    }

    await this.saveToIndexedDB(updatedProject)
    await this.syncManager.queueProjectUpdate(updatedProject)

    console.log(`Updated preferences for project "${project.name}"`)
  }

  async getProjectPreferences(id: string): Promise<ProjectPreferences | undefined> {
    const project = await this.loadFromIndexedDB(id)
    return project?.preferences
  }

  async getStorageTypeStats(): Promise<{
    local: number
    gameguildCloud: number
    googleDrive: number
    total: number
  }> {
    const allProjects = await this.listFromIndexedDB()
    const stats = {
      local: 0,
      gameguildCloud: 0,
      googleDrive: 0,
      total: allProjects.length,
    }

    allProjects.forEach((project) => {
      switch (project.storageType) {
        case STORAGE_TYPES.LOCAL:
          stats.local++
          break
        case STORAGE_TYPES.GAMEGUILD_CLOUD:
          stats.gameguildCloud++
          break
        case STORAGE_TYPES.GOOGLE_DRIVE:
          stats.googleDrive++
          break
      }
    })

    return stats
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

  // ==========================================
  // Git History & Snapshot Management
  // ==========================================

  /**
   * Create a snapshot (tag) of the current project state
   * @param id - Project ID
   * @param name - Snapshot name (optional, auto-generated if not provided)
   */
  async createSnapshot(id: string, name?: string): Promise<SnapshotInfo | null> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    const project = await this.loadFromIndexedDB(id)
    if (!project) {
      console.error(`Cannot create snapshot: Project ${id} not found`)
      return null
    }

    try {
      const historyManager = getHistoryManager()
      
      // Generate tag name if not provided
      let tag: string
      if (name) {
        tag = name
      } else {
        const version = await historyManager.getNextVersionNumber(id, project.name)
        tag = `${project.name}-v${version}`
      }

      const snapshot = await historyManager.createSnapshot(id, tag)
      console.log(`Created snapshot "${snapshot.tag}" for project "${project.name}"`)
      return snapshot
    } catch (error) {
      console.error(`Failed to create snapshot for project ${id}:`, error)
      throw error
    }
  }

  /**
   * List all snapshots for a project
   */
  async listSnapshots(id: string): Promise<SnapshotInfo[]> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    const historyManager = getHistoryManager()
    return await historyManager.listSnapshots(id)
  }

  /**
   * List commit history for a project
   * @param id - Project ID
   * @param maxCount - Maximum number of commits to return (default 50)
   */
  async listHistory(id: string, maxCount: number = 50): Promise<CommitInfo[]> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    const historyManager = getHistoryManager()
    return await historyManager.listHistory(id, maxCount)
  }

  /**
   * Load a snapshot and replace the current IndexedDB state
   * @param id - Project ID
   * @param tag - Snapshot tag to load
   */
  async loadFromSnapshot(id: string, tag: string): Promise<ProjectData | null> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    const historyManager = getHistoryManager()
    const snapshotData = await historyManager.loadSnapshot(id, tag)
    
    if (!snapshotData) {
      console.error(`Snapshot "${tag}" not found for project ${id}`)
      return null
    }

    // Parse the snapshot data
    let projectData: ProjectData
    try {
      projectData = JSON.parse(snapshotData)
    } catch (error) {
      console.error(`Failed to parse snapshot data for ${tag}:`, error)
      return null
    }

    // Ensure required fields and update timestamp
    const now = new Date().toISOString()
    const updatedProject: ProjectData = {
      ...projectData,
      id,
      updatedAt: now,
      hash: await HashManager.generateHash(projectData.data),
      syncStatus: SYNC_STATUS.PENDING,
    }

    // Save to IndexedDB (replaces current state)
    await this.saveToIndexedDB(updatedProject)

    console.log(`Loaded snapshot "${tag}" for project "${updatedProject.name}" - now the current version`)
    return updatedProject
  }

  /**
   * Load a specific commit from history for viewing (READ-ONLY)
   * Does NOT modify IndexedDB - just returns the historical data for display
   * @param id - Project ID
   * @param sha - Commit SHA to load
   */
  async loadFromHistory(id: string, sha: string): Promise<ProjectData | null> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    const historyManager = getHistoryManager()
    const commitData = await historyManager.loadCommit(id, sha)
    
    if (!commitData) {
      console.error(`Commit ${sha} not found for project ${id}`)
      return null
    }

    // Parse the commit data
    let projectData: ProjectData
    try {
      projectData = JSON.parse(commitData)
    } catch (error) {
      console.error(`Failed to parse commit data for ${sha}:`, error)
      return null
    }

    // Return the historical data AS-IS for viewing (don't modify anything)
    console.log(`Loaded commit ${sha.substring(0, 7)} for project "${projectData.name}" - viewing historical version (read-only)`)
    return projectData
  }

  /**
   * Delete a snapshot
   */
  async deleteSnapshot(id: string, tag: string): Promise<boolean> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    const historyManager = getHistoryManager()
    return await historyManager.deleteSnapshot(id, tag)
  }

  /**
   * Check if a project has any snapshots
   */
  async hasSnapshots(id: string): Promise<boolean> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    const historyManager = getHistoryManager()
    return await historyManager.hasSnapshots(id)
  }

  /**
   * Check if a project has any history
   */
  async hasHistory(id: string): Promise<boolean> {
    if (!this.isInitialized) throw new Error("Storage adapter not initialized")

    const historyManager = getHistoryManager()
    return await historyManager.hasHistory(id)
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


