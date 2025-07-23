import { ApiClient } from "../api/api-client"
import { SyncQueue } from "./sync-queue"
import { HashManager } from "./hash-manager"
import { syncConfig } from "./sync-config"

interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  hash?: string
}

interface ProjectMetadata {
  id: string
  name: string
  tags: string[]
  size: number
  hash: string
  createdAt: string
  updatedAt: string
}

interface SyncStats {
  isOnline: boolean
  isSyncing: boolean
  lastSync: string | null
  queue: {
    pending: number
    processing: number
    completed: number
    failed: number
  }
}

export class SyncManager {
  private apiClient: ApiClient
  private syncQueue: SyncQueue
  private isInitialized = false
  private isSyncing = false
  private lastSync: string | null = null
  private eventListeners: {
    syncStart: Array<() => void>
    syncComplete: Array<(stats: any) => void>
    syncError: Array<(error: Error) => void>
  } = {
    syncStart: [],
    syncComplete: [],
    syncError: [],
  }

  constructor() {
    this.apiClient = new ApiClient()
    this.syncQueue = new SyncQueue()
  }

  async init(): Promise<void> {
    if (this.isInitialized) return

    await this.syncQueue.init()
    this.isInitialized = true

    // Listen for config changes
    syncConfig.onConfigChange((config) => {
      if (config.debugMode) {
        console.log("Sync manager received config update:", config)
      }
    })

    console.log("Sync Manager initialized")
  }

  async fetchServerProjectsMetadata(): Promise<ProjectMetadata[]> {
    if (!syncConfig.isEnabled()) {
      if (syncConfig.getConfig().debugMode) {
        console.log("Sync disabled, skipping server metadata fetch")
      }
      return []
    }

    try {
      const metadata = await this.apiClient.getProjectsMetadata()

      if (syncConfig.getConfig().debugMode) {
        console.log("Fetched server projects metadata:", metadata.length, "projects")
      }

      return metadata
    } catch (error) {
      // Don't throw error, just log warning when sync is enabled
      if (syncConfig.getConfig().debugMode) {
        console.warn("⚠️ Sync Warning: Failed to fetch server metadata:", error.message)
      } else {
        console.warn("Sync: Server unavailable, working offline")
      }
      return []
    }
  }

  async downloadProject(id: string): Promise<ProjectData | null> {
    if (!syncConfig.isEnabled()) {
      return null
    }

    try {
      const project = await this.apiClient.getProject(id)

      if (syncConfig.getConfig().debugMode && project) {
        console.log("Downloaded project from server:", project.name)
      }

      return project
    } catch (error) {
      if (syncConfig.getConfig().debugMode) {
        console.warn("⚠️ Sync Warning: Failed to download project:", error.message)
      }
      return null
    }
  }

  async syncProjectIfNeeded(localProject: ProjectData): Promise<ProjectData | null> {
    if (!syncConfig.isEnabled() || !navigator.onLine) {
      return null
    }

    try {
      const localHash = localProject.hash || (await HashManager.generateProjectHash(localProject))
      const hashCheck = await this.apiClient.checkProjectHash(localProject.id, localHash)

      if (hashCheck.needsUpdate && hashCheck.serverHash) {
        if (syncConfig.getConfig().debugMode) {
          console.log("Project needs sync, downloading latest version:", localProject.name)
        }

        return await this.downloadProject(localProject.id)
      }

      return null
    } catch (error) {
      if (syncConfig.getConfig().debugMode) {
        console.warn("⚠️ Sync Warning: Failed to check project sync status:", error.message)
      }
      return null
    }
  }

  async queueProjectUpdate(project: ProjectData): Promise<void> {
    if (!syncConfig.isEnabled()) {
      return
    }

    await this.syncQueue.enqueue({
      type: "update",
      projectId: project.id,
      data: project,
    })

    if (syncConfig.getConfig().debugMode) {
      console.log("Queued project update:", project.name)
    }
  }

  async queueProjectDelete(projectId: string): Promise<void> {
    if (!syncConfig.isEnabled()) {
      return
    }

    await this.syncQueue.enqueue({
      type: "delete",
      projectId: projectId,
    })

    if (syncConfig.getConfig().debugMode) {
      console.log("Queued project delete:", projectId)
    }
  }

  async getSyncStats(): Promise<SyncStats> {
    const queueStats = await this.syncQueue.getStats()

    return {
      isOnline: navigator.onLine && syncConfig.isEnabled(),
      isSyncing: this.isSyncing,
      lastSync: this.lastSync,
      queue: queueStats,
    }
  }

  async retryFailedItems(): Promise<void> {
    if (!syncConfig.isEnabled()) {
      return
    }

    await this.syncQueue.retryFailedItems()

    if (syncConfig.getConfig().debugMode) {
      console.log("Retried failed sync items")
    }
  }

  // Event listeners
  onSyncStart(callback: () => void): void {
    this.eventListeners.syncStart.push(callback)
  }

  onSyncComplete(callback: (stats: any) => void): void {
    this.eventListeners.syncComplete.push(callback)
  }

  onSyncError(callback: (error: Error) => void): void {
    this.eventListeners.syncError.push(callback)
  }

  private emitSyncStart(): void {
    this.eventListeners.syncStart.forEach((callback) => {
      try {
        callback()
      } catch (error) {
        console.error("Error in sync start listener:", error)
      }
    })
  }

  private emitSyncComplete(stats: any): void {
    this.eventListeners.syncComplete.forEach((callback) => {
      try {
        callback(stats)
      } catch (error) {
        console.error("Error in sync complete listener:", error)
      }
    })
  }

  private emitSyncError(error: Error): void {
    this.eventListeners.syncError.forEach((callback) => {
      try {
        callback(error)
      } catch (error) {
        console.error("Error in sync error listener:", error)
      }
    })
  }

  destroy(): void {
    this.syncQueue.destroy()
    this.isInitialized = false
    this.eventListeners = {
      syncStart: [],
      syncComplete: [],
      syncError: [],
    }
  }
}
