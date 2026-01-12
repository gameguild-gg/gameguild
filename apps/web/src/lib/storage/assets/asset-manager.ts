import type {
  AssetMetadata,
  AssetUsage,
  AssetIndex,
  AssetData,
  SaveAssetParams,
  SaveAssetResult,
} from "./types"
import { getMimeTypeFromFileName, TEXT_MIME_TYPES, TEXT_EXTENSIONS } from "./types"
import type {
  CollectionManifest,
  CollectionData,
  SaveCollectionParams,
  SaveCollectionResult,
  CollectionMetadata,
} from "./collection-types"

/**
 * AssetManager handles storage and management of media assets
 * Assets are stored in IndexedDB with a separate assets.json index
 */
export class AssetManager {
  private db: IDBDatabase | null = null
  private isInitialized = false

  private readonly DB_NAME = "GGAssetsDB"
  private readonly DB_VERSION = 2
  private readonly ASSETS_STORE = "assets"
  private readonly COLLECTIONS_STORE = "collections"
  private readonly INDEX_STORE = "asset_index"
  private readonly INDEX_KEY = "main_index"

  /**
   * Initialize the AssetManager and IndexedDB
   */
  async init(): Promise<void> {
    if (this.isInitialized) return

    console.log("AssetManager: Initializing...")

    return new Promise((resolve, reject) => {
      const request = indexedDB.open(this.DB_NAME, this.DB_VERSION)

      request.onerror = () => {
        console.error("AssetManager: Failed to open IndexedDB", request.error)
        reject(request.error)
      }
      request.onsuccess = () => {
        this.db = request.result
        this.isInitialized = true
        console.log("AssetManager: Initialized successfully")
        resolve()
      }

      request.onupgradeneeded = (event) => {
        console.log("AssetManager: Database upgrade needed")
        const db = (event.target as IDBOpenDBRequest).result

        // Create assets store
        if (!db.objectStoreNames.contains(this.ASSETS_STORE)) {
          console.log(`AssetManager: Creating object store: ${this.ASSETS_STORE}`)
          db.createObjectStore(this.ASSETS_STORE, { keyPath: "metadata.id" })
        }

        // Create collections store
        if (!db.objectStoreNames.contains(this.COLLECTIONS_STORE)) {
          console.log(`AssetManager: Creating object store: ${this.COLLECTIONS_STORE}`)
          db.createObjectStore(this.COLLECTIONS_STORE, { keyPath: "metadata.id" })
        }

        // Create index store
        if (!db.objectStoreNames.contains(this.INDEX_STORE)) {
          console.log(`AssetManager: Creating object store: ${this.INDEX_STORE}`)
          db.createObjectStore(this.INDEX_STORE, { keyPath: "key" })
        }
      }
    })
  }

  /**
   * Generate SHA1 hash from data
   */
  private async generateSHA1(data: string | ArrayBuffer): Promise<string> {
    let buffer: ArrayBuffer

    if (typeof data === "string") {
      // Convert data URL to ArrayBuffer
      if (data.startsWith("data:")) {
        const base64 = data.split(",")[1]
        if (!base64) {
          throw new Error("Invalid data URL format")
        }
        const binaryString = atob(base64)
        const bytes = new Uint8Array(binaryString.length)
        for (let i = 0; i < binaryString.length; i++) {
          bytes[i] = binaryString.charCodeAt(i)
        }
        buffer = bytes.buffer
      } else {
        // Regular string
        const encoder = new TextEncoder()
        buffer = encoder.encode(data).buffer
      }
    } else {
      buffer = data
    }

    const hashBuffer = await crypto.subtle.digest("SHA-1", buffer)
    const hashArray = Array.from(new Uint8Array(hashBuffer))
    const hashHex = hashArray.map((b) => b.toString(16).padStart(2, "0")).join("")
    return hashHex
  }

  /**
   * Load the asset index from IndexedDB
   */
  private async loadIndex(): Promise<AssetIndex> {
    if (!this.db) throw new Error("AssetManager not initialized")

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.INDEX_STORE], "readonly")
      const store = transaction.objectStore(this.INDEX_STORE)
      const request = store.get(this.INDEX_KEY)

      request.onsuccess = () => {
        const result = request.result
        if (result && result.index) {
          resolve(result.index)
        } else {
          // Create new index
          const newIndex: AssetIndex = {
            version: "1.0",
            lastUpdated: new Date().toISOString(),
            assets: {},
          }
          resolve(newIndex)
        }
      }
      request.onerror = () => reject(request.error)
    })
  }

  /**
   * Save the asset index to IndexedDB
   */
  private async saveIndex(index: AssetIndex): Promise<void> {
    if (!this.db) throw new Error("AssetManager not initialized")

    index.lastUpdated = new Date().toISOString()

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.INDEX_STORE], "readwrite")
      const store = transaction.objectStore(this.INDEX_STORE)
      const request = store.put({ key: this.INDEX_KEY, index })

      request.onsuccess = () => {
        console.log(`Asset index saved: ${Object.keys(index.assets).length} assets`)
        resolve()
      }
      request.onerror = () => {
        console.error("Failed to save asset index", request.error)
        reject(request.error)
      }
    })
  }

  /**
   * Save an asset to storage
   */
  async saveAsset(params: SaveAssetParams): Promise<SaveAssetResult> {
    if (!this.isInitialized) {
      await this.init()
    }

    console.log("AssetManager: Starting saveAsset", { 
      hasFile: !!params.file, 
      hasDataUrl: !!params.dataUrl, 
      hasUrlSource: !!params.urlSource 
    })

    try {
      let dataUrl: string
      let fileName: string
      let mimeType: string
      let size: number
      let origin: string

      // Determine the source and prepare data
      if (params.file) {
        // File upload
        fileName = params.file.name
        mimeType = params.file.type
        size = params.file.size
        origin = "upload"
        
        // Check if should store as text
        const shouldStoreAsText = params.forceTextStorage || this.isTextFile(params.file)
        dataUrl = await this.fileToDataUrl(params.file, shouldStoreAsText)
      } else if (params.dataUrl) {
        // Data URL (already converted)
        dataUrl = params.dataUrl
        origin = "data"
        
        // Use provided fileName or generate one
        fileName = params.fileName || `asset-${Date.now()}`
        
        // Check if it's a data URL or plain text
        if (dataUrl.startsWith('data:')) {
          // Extract MIME type from data URL
          const matches = dataUrl.match(/^data:([^;]+);/)
          mimeType = matches && matches[1] ? matches[1] : "application/octet-stream"
          
          // If we have a fileName, try to determine MIME type from extension
          if (params.fileName && mimeType === "application/octet-stream") {
            mimeType = getMimeTypeFromFileName(params.fileName)
          }
          
          // Estimate size from base64 data URL
          const base64 = dataUrl.split(",")[1]
          size = base64 ? Math.ceil((base64.length * 3) / 4) : 0
        } else {
          // Plain text content
          mimeType = getMimeTypeFromFileName(params.fileName || fileName)
          // Calculate size from text content (UTF-8 encoding)
          size = new TextEncoder().encode(dataUrl).length
        }
      } else if (params.urlSource) {
        // URL source
        dataUrl = params.urlSource
        origin = "url"
        mimeType = "unknown"
        size = 0
        fileName = params.urlSource.split("/").pop() || `url-asset-${Date.now()}`
      } else {
        return {
          success: false,
          error: "No file, dataUrl, or urlSource provided",
        }
      }

      // Generate SHA1 hash
      const sha1hash = await this.generateSHA1(dataUrl)
      console.log(`AssetManager: Generated SHA1 hash: ${sha1hash}`)

      // Load index
      const index = await this.loadIndex()
      console.log(`AssetManager: Loaded index with ${Object.keys(index.assets).length} assets`)

      // Check if asset already exists in store
      const existingAsset = await this.getAssetFromStore(sha1hash)
      const isNewAsset = !existingAsset
      console.log(`AssetManager: Asset ${isNewAsset ? 'is new' : 'already exists'}`)

      let metadata: AssetMetadata

      if (isNewAsset) {
        // Determine storage type
        const storageType: 'dataurl' | 'text' = 
          !dataUrl.startsWith('data:') && !dataUrl.startsWith('http') 
            ? 'text' 
            : 'dataurl'
        
        // Create new metadata
        metadata = {
          id: sha1hash,
          name: fileName,
          origin,
          type: params.type || "standard",
          author: params.author,
          license: params.license,
          sha1hash,
          size,
          mimeType,
          storageType,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        }

        // Save asset data to store
        const assetData: AssetData = {
          metadata,
          data: dataUrl,
        }
        console.log(`AssetManager: Saving new asset to store: ${sha1hash}`)
        await this.saveAssetToStore(assetData)
      } else {
        // Use existing metadata
        metadata = existingAsset.metadata
        metadata.updatedAt = new Date().toISOString()
        
        // Update metadata in store
        const assetData: AssetData = {
          metadata,
          data: existingAsset.data,
        }
        await this.saveAssetToStore(assetData)
      }

      // Update usage tracking in index (separate from asset metadata)
      if (params.projectId && params.nodeId) {
        let usageList = index.assets[sha1hash] || []
        const existingUsage = usageList.find((u) => u.projectId === params.projectId)
        
        if (existingUsage) {
          if (!existingUsage.nodeIds.includes(params.nodeId)) {
            existingUsage.nodeIds.push(params.nodeId)
          }
        } else {
          usageList.push({
            projectId: params.projectId,
            nodeIds: [params.nodeId],
          })
        }
        
        index.assets[sha1hash] = usageList
        console.log(`AssetManager: Updating index with usage tracking`)
        await this.saveIndex(index)
      }

      // Return asset URL (using the SHA1 hash as identifier)
      const assetUrl = `asset://${sha1hash}`
      console.log(`AssetManager: Asset saved successfully, URL: ${assetUrl}`)

      return {
        success: true,
        assetId: sha1hash,
        assetUrl,
        metadata,
      }
    } catch (error) {
      console.error("AssetManager: Failed to save asset:", error)
      return {
        success: false,
        error: error instanceof Error ? error.message : "Unknown error",
      }
    }
  }

  /**
   * Convert File to data URL or plain text
   */
  private fileToDataUrl(file: File, asText: boolean = false): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader()
      reader.onload = () => resolve(reader.result as string)
      reader.onerror = reject
      
      if (asText) {
        // Read as plain text
        reader.readAsText(file)
      } else {
        // Read as data URL (base64)
        reader.readAsDataURL(file)
      }
    })
  }

  /**
   * Check if file is a text file based on MIME type or extension
   */
  private isTextFile(file: File): boolean {
    // Check MIME type
    if (TEXT_MIME_TYPES.some(type => file.type.startsWith(type))) {
      return true
    }
    
    // Check file extension
    const fileName = file.name.toLowerCase()
    return TEXT_EXTENSIONS.some(ext => fileName.endsWith(ext))
  }

  /**
   * Save asset data to IndexedDB
   */
  private async saveAssetToStore(assetData: AssetData): Promise<void> {
    if (!this.db) throw new Error("AssetManager not initialized")

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.ASSETS_STORE], "readwrite")
      const store = transaction.objectStore(this.ASSETS_STORE)
      const request = store.put(assetData)

      request.onsuccess = () => {
        console.log(`Asset saved to IndexedDB: ${assetData.metadata.id} (${assetData.metadata.name})`)
        resolve()
      }
      request.onerror = () => {
        console.error(`Failed to save asset to IndexedDB: ${assetData.metadata.id}`, request.error)
        reject(request.error)
      }
    })
  }

  /**
   * Get an asset from store by ID (internal method)
   */
  private async getAssetFromStore(assetId: string): Promise<AssetData | null> {
    if (!this.isInitialized) {
      await this.init()
    }

    if (!this.db) throw new Error("AssetManager not initialized")

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.ASSETS_STORE], "readonly")
      const store = transaction.objectStore(this.ASSETS_STORE)
      const request = store.get(assetId)

      request.onsuccess = () => resolve(request.result || null)
      request.onerror = () => reject(request.error)
    })
  }

  /**
   * Get an asset by ID (public method - returns data and metadata)
   */
  async getAsset(assetId: string): Promise<AssetData | null> {
    return this.getAssetFromStore(assetId)
  }

  /**
   * Get asset URL (data URL) by asset ID
   */
  async getAssetUrl(assetId: string): Promise<string | null> {
    const asset = await this.getAsset(assetId)
    return asset ? asset.data : null
  }

  /**
   * Rename an asset by updating its metadata
   * @param assetId - The asset ID
   * @param newName - The new name for the asset
   * @returns true if successful
   */
  async renameAsset(assetId: string, newName: string): Promise<boolean> {
    if (!this.isInitialized) {
      await this.init()
    }

    try {
      // Get the asset data
      const assetData = await this.getAsset(assetId)
      if (!assetData) {
        console.error(`Asset ${assetId} not found`)
        return false
      }

      // Update the name in metadata
      assetData.metadata.name = newName.trim()

      // Save back to store
      await this.saveAssetToStore(assetData)
      
      console.log(`Asset ${assetId} renamed to "${newName}"`)
      return true
    } catch (error) {
      console.error("Failed to rename asset:", error)
      return false
    }
  }

  /**
   * Remove project reference from asset index
   * This is used when a node with an asset is deleted from a project
   * @param assetId - The asset ID
   * @param projectId - The project ID to check and potentially remove
   * @param nodeId - Optional node ID being removed
   * @param projectData - The project's Lexical JSON to verify if asset is still used
   */
  async removeProjectReference(
    assetId: string, 
    projectId?: string, 
    nodeId?: string,
    projectData?: string | object
  ): Promise<boolean> {
    if (!this.isInitialized) {
      await this.init()
    }

    try {
      const index = await this.loadIndex()
      const usageList = index.assets[assetId]

      if (!usageList) {
        console.warn(`Asset ${assetId} not found in index`)
        return true
      }

      // If projectId is NOT provided, do nothing
      // Assets are never deleted, only references are cleaned
      if (!projectId) {
        console.log(`No projectId provided, asset ${assetId} references unchanged`)
        return true
      }

      // If projectData is provided, verify if asset is still referenced in the JSON
      if (projectData) {
        const stillUsed = this.isAssetUsedInProjectData(projectData, assetId)
        
        if (stillUsed) {
          console.log(`Asset ${assetId} is still being used in project ${projectId}, keeping reference`)
          return true
        }
        
        console.log(`Asset ${assetId} is no longer used in project ${projectId}, removing reference`)
      }
      
      // Remove the project from usage list
      const usage = usageList.find((u: AssetUsage) => u.projectId === projectId)
      if (usage) {
        // If nodeId is provided, remove only that node
        if (nodeId) {
          usage.nodeIds = usage.nodeIds.filter((id: string) => id !== nodeId)
          if (usage.nodeIds.length === 0) {
            // No more nodes from this project, remove project reference
            index.assets[assetId] = usageList.filter((u: AssetUsage) => u.projectId !== projectId)
          }
        } else {
          // Remove entire project usage
          index.assets[assetId] = usageList.filter((u: AssetUsage) => u.projectId !== projectId)
        }
      }

      // Clean up empty usage list from index
      const updatedUsageList = index.assets[assetId]
      if (updatedUsageList && updatedUsageList.length === 0) {
        delete index.assets[assetId]
        console.log(`Asset ${assetId} has no more project references in index`)
      }

      await this.saveIndex(index)
      return true
    } catch (error) {
      console.error("Failed to remove asset reference:", error)
      return false
    }
  }

  /**
   * Check if an asset is still being used in a project's Lexical JSON
   * @param projectData - The Lexical JSON data as string or object
   * @param assetId - The asset ID to search for
   * @returns true if the asset is found in the JSON, false otherwise
   */
  private isAssetUsedInProjectData(projectData: string | object, assetId: string): boolean {
    try {
      let jsonData: any
      
      if (typeof projectData === 'string') {
        jsonData = JSON.parse(projectData)
      } else {
        jsonData = projectData
      }
      
      // Convert to string and search for asset:// references
      const jsonString = JSON.stringify(jsonData)
      const assetPattern = new RegExp(`asset://${assetId}`, 'g')
      const matches = jsonString.match(assetPattern)
      
      return matches !== null && matches.length > 0
    } catch (error) {
      console.error('Failed to parse project data:', error)
      return false
    }
  }

  /**
   * Delete asset from IndexedDB store
   */
  private async deleteAssetFromStore(assetId: string): Promise<void> {
    if (!this.db) throw new Error("AssetManager not initialized")

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.ASSETS_STORE], "readwrite")
      const store = transaction.objectStore(this.ASSETS_STORE)
      const request = store.delete(assetId)

      request.onsuccess = () => resolve()
      request.onerror = () => reject(request.error)
    })
  }

  /**
   * Completely delete an asset - removes from index and deletes from store
   * This should be used when explicitly deleting an asset from the manager UI
   * @param assetId - The asset ID to delete
   * @returns true if successful
   */
  async deleteAssetCompletely(assetId: string): Promise<boolean> {
    if (!this.isInitialized) {
      await this.init()
    }

    try {
      const index = await this.loadIndex()
      
      // Remove from index
      if (index.assets[assetId]) {
        delete index.assets[assetId]
        await this.saveIndex(index)
        console.log(`Asset ${assetId} removed from index`)
      }
      
      // Delete from store
      await this.deleteAssetFromStore(assetId)
      console.log(`Asset ${assetId} deleted from store`)
      
      return true
    } catch (error) {
      console.error("Failed to delete asset completely:", error)
      return false
    }
  }

  /**
   * List all assets
   */
  async listAssets(): Promise<AssetMetadata[]> {
    if (!this.isInitialized) {
      await this.init()
    }

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.ASSETS_STORE], "readonly")
      const store = transaction.objectStore(this.ASSETS_STORE)
      const request = store.getAll()

      request.onsuccess = () => {
        const assets: AssetMetadata[] = request.result.map((assetData: AssetData) => assetData.metadata)
        resolve(assets)
      }
      request.onerror = () => reject(request.error)
    })
  }

  /**
   * List all assets with their project usage information
   */
  async listAssetsWithUsage(): Promise<Array<AssetMetadata & { projects: string[] }>> {
    if (!this.isInitialized) {
      await this.init()
    }

    const index = await this.loadIndex()
    const allAssets = await this.listAssets()

    // Map assets to include project IDs
    return allAssets.map(asset => {
      const usageList = index.assets[asset.id] || []
      const projectIds = usageList.map(u => u.projectId)
      
      return {
        ...asset,
        projects: projectIds
      }
    })
  }

  /**
   * Get assets used by a specific project
   */
  async getAssetsForProject(projectId: string): Promise<AssetMetadata[]> {
    if (!this.isInitialized) {
      await this.init()
    }

    const index = await this.loadIndex()
    const assetIds: string[] = []

    // Find all assets used by this project
    for (const assetId in index.assets) {
      const usageList = index.assets[assetId]
      if (usageList && usageList.some((u) => u.projectId === projectId)) {
        assetIds.push(assetId)
      }
    }

    // Fetch metadata for these assets
    const assets: AssetMetadata[] = []
    for (const assetId of assetIds) {
      const assetData = await this.getAssetFromStore(assetId)
      if (assetData) {
        assets.push(assetData.metadata)
      }
    }

    return assets
  }

  /**
   * Clean up unused assets (assets with no usages)
   */
  async cleanupUnusedAssets(): Promise<number> {
    if (!this.isInitialized) {
      await this.init()
    }

    const index = await this.loadIndex()
    let deletedCount = 0

    for (const assetId in index.assets) {
      const usageList = index.assets[assetId]
      if (usageList && usageList.length === 0) {
        delete index.assets[assetId]
        await this.deleteAssetFromStore(assetId)
        deletedCount++
      }
    }

    if (deletedCount > 0) {
      await this.saveIndex(index)
    }

    return deletedCount
  }

  /**
   * Get storage statistics
   */
  async getStats(): Promise<{
    totalAssets: number
    totalSize: number
    usedAssets: number
    unusedAssets: number
  }> {
    const assets = await this.listAssets()
    const index = await this.loadIndex()
    
    const totalSize = assets.reduce((sum, asset) => sum + asset.size, 0)
    let usedAssets = 0
    let unusedAssets = 0

    for (const asset of assets) {
      const usageList = index.assets[asset.id] || []
      if (usageList.length > 0) {
        usedAssets++
      } else {
        unusedAssets++
      }
    }

    return {
      totalAssets: assets.length,
      totalSize,
      usedAssets,
      unusedAssets,
    }
  }

  /**
   * Register usage of an existing asset with a project/node
   * This is useful when importing assets from collections
   */
  async registerAssetUsage(assetId: string, projectId: string, nodeId: string): Promise<void> {
    if (!this.isInitialized) {
      await this.init()
    }

    console.log(`AssetManager: Registering asset usage - asset:${assetId}, project:${projectId}, node:${nodeId}`)

    const index = await this.loadIndex()
    let usageList = index.assets[assetId] || []
    const existingUsage = usageList.find((u) => u.projectId === projectId)
    
    if (existingUsage) {
      if (!existingUsage.nodeIds.includes(nodeId)) {
        existingUsage.nodeIds.push(nodeId)
        console.log(`AssetManager: Added nodeId to existing usage`)
      } else {
        console.log(`AssetManager: Usage already registered`)
      }
    } else {
      usageList.push({
        projectId,
        nodeIds: [nodeId],
      })
      console.log(`AssetManager: Created new usage entry`)
    }
    
    index.assets[assetId] = usageList
    await this.saveIndex(index)
    console.log(`AssetManager: Asset usage registered successfully`)
  }

  /**
   * Export assets for a specific project
   * Returns a map of assetId -> AssetData for all assets used by the project
   */
  async exportProjectAssets(projectId: string): Promise<Record<string, AssetData>> {
    if (!this.isInitialized) {
      await this.init()
    }

    const index = await this.loadIndex()
    const projectAssets: Record<string, AssetData> = {}

    // Find all assets used by this project
    for (const assetId in index.assets) {
      const usageList = index.assets[assetId]
      if (usageList && usageList.some((u) => u.projectId === projectId)) {
        const assetData = await this.getAssetFromStore(assetId)
        if (assetData) {
          projectAssets[assetId] = assetData
        }
      }
    }

    console.log(`AssetManager: Exported ${Object.keys(projectAssets).length} assets for project ${projectId}`)
    return projectAssets
  }

  /**
   * Export usage index for a specific project
   * Returns only the usage tracking for assets used by this project
   */
  async exportProjectAssetIndex(projectId: string): Promise<Record<string, AssetUsage[]>> {
    if (!this.isInitialized) {
      await this.init()
    }

    const index = await this.loadIndex()
    const projectIndex: Record<string, AssetUsage[]> = {}

    // Filter index to only include assets used by this project
    for (const assetId in index.assets) {
      const usageList = index.assets[assetId]
      if (usageList && usageList.some((u) => u.projectId === projectId)) {
        // Only include usage for this specific project
        projectIndex[assetId] = usageList.filter((u) => u.projectId === projectId)
      }
    }

    return projectIndex
  }

  /**
   * Import assets from exported data
   * Merges imported assets with existing ones, updating usage tracking
   */
  async importProjectAssets(
    assets: Record<string, AssetData>,
    assetIndex: Record<string, AssetUsage[]>,
    targetProjectId: string
  ): Promise<{ imported: number; skipped: number; updated: number }> {
    if (!this.isInitialized) {
      await this.init()
    }

    let imported = 0
    let skipped = 0
    let updated = 0

    const index = await this.loadIndex()

    for (const assetId in assets) {
      const assetData = assets[assetId]
      if (!assetData) continue

      try {
        // Check if asset already exists
        const existingAsset = await this.getAssetFromStore(assetId)

        if (existingAsset) {
          // Asset exists, just update usage tracking
          updated++
        } else {
          // New asset, save to store
          await this.saveAssetToStore(assetData)
          imported++
        }

        // Update usage index with new project ID
        const importedUsage = assetIndex[assetId] || []
        let currentUsageList = index.assets[assetId] || []

        // Update usage to point to target project
        for (const usage of importedUsage) {
          const existingUsage = currentUsageList.find((u) => u.projectId === targetProjectId)
          
          if (existingUsage) {
            // Merge node IDs
            for (const nodeId of usage.nodeIds) {
              if (!existingUsage.nodeIds.includes(nodeId)) {
                existingUsage.nodeIds.push(nodeId)
              }
            }
          } else {
            // Add new usage entry
            currentUsageList.push({
              projectId: targetProjectId,
              nodeIds: usage.nodeIds,
            })
          }
        }

        index.assets[assetId] = currentUsageList
      } catch (error) {
        console.error(`Failed to import asset ${assetId}:`, error)
        skipped++
      }
    }

    // Save updated index
    await this.saveIndex(index)

    console.log(`AssetManager: Import complete - ${imported} imported, ${updated} updated, ${skipped} skipped`)
    return { imported, skipped, updated }
  }

  /**
   * Remove project from all asset usage tracking (when project is deleted)
   */
  async removeProjectFromAssets(projectId: string): Promise<number> {
    if (!this.isInitialized) {
      await this.init()
    }

    const index = await this.loadIndex()
    let removedCount = 0

    for (const assetId in index.assets) {
      const usageList = index.assets[assetId]
      if (usageList) {
        const originalLength = usageList.length
        index.assets[assetId] = usageList.filter((u) => u.projectId !== projectId)
        
        if (index.assets[assetId].length < originalLength) {
          removedCount++
        }

        // If no more usages, delete the asset
        if (index.assets[assetId].length === 0) {
          delete index.assets[assetId]
          await this.deleteAssetFromStore(assetId)
        }
      }
    }

    await this.saveIndex(index)
    console.log(`AssetManager: Removed project ${projectId} from ${removedCount} assets`)
    return removedCount
  }

  /**
   * Synchronize asset index with project data
   * Removes references to assets that are no longer used in the project
   * @param projectId - The project ID to sync
   * @param projectData - The project's Lexical JSON data
   * @returns Number of references removed
   */
  async syncProjectAssets(projectId: string, projectData: string | object): Promise<number> {
    if (!this.isInitialized) {
      await this.init()
    }

    try {
      const index = await this.loadIndex()
      let removedCount = 0

      // Check each asset in the index
      for (const assetId in index.assets) {
        const usageList = index.assets[assetId]
        if (!usageList) continue

        // Check if this asset is referenced by the project
        const hasProjectRef = usageList.some(u => u.projectId === projectId)
        if (!hasProjectRef) continue

        // Verify if asset is actually used in the project data
        const stillUsed = this.isAssetUsedInProjectData(projectData, assetId)

        if (!stillUsed) {
          // Remove the project reference from this asset
          index.assets[assetId] = usageList.filter(u => u.projectId !== projectId)
          removedCount++

          console.log(`Asset ${assetId} no longer used by project ${projectId}, removing reference`)

          // If no more usages, delete the asset entry from index
          if (index.assets[assetId].length === 0) {
            delete index.assets[assetId]
            console.log(`Asset ${assetId} has no more project references`)
          }
        }
      }

      if (removedCount > 0) {
        await this.saveIndex(index)
        console.log(`AssetManager: Synced project ${projectId}, removed ${removedCount} unused asset references`)
      }

      return removedCount
    } catch (error) {
      console.error("Failed to sync project assets:", error)
      return 0
    }
  }

  /**
   * Save a collection to storage
   * Collections are special assets that contain manifests referencing other assets
   */
  async saveCollection(params: SaveCollectionParams): Promise<SaveCollectionResult> {
    if (!this.isInitialized) {
      await this.init()
    }

    try {
      console.log("AssetManager: Saving collection", { name: params.name })

      // Create manifest
      const manifest: CollectionManifest = {
        type: "collection",
        metadata: {
          id: "", // Will be set after hashing
          name: params.name,
          description: params.description,
          tags: params.tags,
          created: Date.now(),
          updated: Date.now(),
          author: params.author,
        },
        structure: params.structure,
      }

      // Generate SHA1 hash of the manifest
      const manifestJson = JSON.stringify(manifest, null, 2)
      const collectionId = await this.generateSHA1(manifestJson)
      manifest.metadata.id = collectionId

      // Check if collection already exists
      const existing = await this.getCollectionFromStore(collectionId)
      if (existing) {
        console.log(`Collection ${collectionId} already exists, updating`)
        manifest.metadata.created = existing.metadata.created
        manifest.metadata.updated = Date.now()
      }

      // Save to collections store
      const collectionData: CollectionData = {
        metadata: manifest.metadata,
        manifest: JSON.stringify(manifest),
      }

      await this.saveCollectionToStore(collectionData)

      console.log(`Collection saved successfully: ${collectionId} (${params.name})`)

      return {
        success: true,
        collectionId,
        metadata: manifest.metadata,
      }
    } catch (error) {
      console.error("Failed to save collection:", error)
      return {
        success: false,
        error: error instanceof Error ? error.message : "Unknown error",
      }
    }
  }

  /**
   * Save collection data to IndexedDB
   */
  private async saveCollectionToStore(collectionData: CollectionData): Promise<void> {
    if (!this.db) throw new Error("AssetManager not initialized")

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.COLLECTIONS_STORE], "readwrite")
      const store = transaction.objectStore(this.COLLECTIONS_STORE)
      const request = store.put(collectionData)

      request.onsuccess = () => {
        console.log(`Collection saved: ${collectionData.metadata.id} (${collectionData.metadata.name})`)
        resolve()
      }
      request.onerror = () => {
        console.error(`Failed to save collection: ${collectionData.metadata.id}`, request.error)
        reject(request.error)
      }
    })
  }

  /**
   * Get a collection from store by ID
   */
  private async getCollectionFromStore(collectionId: string): Promise<CollectionData | null> {
    if (!this.db) throw new Error("AssetManager not initialized")

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.COLLECTIONS_STORE], "readonly")
      const store = transaction.objectStore(this.COLLECTIONS_STORE)
      const request = store.get(collectionId)

      request.onsuccess = () => resolve(request.result || null)
      request.onerror = () => reject(request.error)
    })
  }

  /**
   * Get a collection by ID (public method)
   */
  async getCollection(collectionId: string): Promise<CollectionManifest | null> {
    if (!this.isInitialized) {
      await this.init()
    }

    const data = await this.getCollectionFromStore(collectionId)
    if (!data) return null

    try {
      return JSON.parse(data.manifest) as CollectionManifest
    } catch (error) {
      console.error("Failed to parse collection manifest:", error)
      return null
    }
  }

  /**
   * List all collections
   */
  async listCollections(): Promise<CollectionMetadata[]> {
    if (!this.isInitialized) {
      await this.init()
    }

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.COLLECTIONS_STORE], "readonly")
      const store = transaction.objectStore(this.COLLECTIONS_STORE)
      const request = store.getAll()

      request.onsuccess = () => {
        const collections: CollectionMetadata[] = request.result.map(
          (data: CollectionData) => data.metadata
        )
        resolve(collections)
      }
      request.onerror = () => reject(request.error)
    })
  }

  /**
   * Delete a collection
   */
  async deleteCollection(collectionId: string): Promise<boolean> {
    if (!this.isInitialized) {
      await this.init()
    }

    try {
      if (!this.db) throw new Error("AssetManager not initialized")

      return new Promise((resolve, reject) => {
        const transaction = this.db!.transaction([this.COLLECTIONS_STORE], "readwrite")
        const store = transaction.objectStore(this.COLLECTIONS_STORE)
        const request = store.delete(collectionId)

        request.onsuccess = () => {
          console.log(`Collection deleted: ${collectionId}`)
          resolve(true)
        }
        request.onerror = () => {
          console.error(`Failed to delete collection: ${collectionId}`, request.error)
          reject(request.error)
        }
      })
    } catch (error) {
      console.error("Failed to delete collection:", error)
      return false
    }
  }

  /**
   * Rename a collection
   */
  async renameCollection(collectionId: string, newName: string): Promise<boolean> {
    if (!this.isInitialized) {
      await this.init()
    }

    try {
      const data = await this.getCollectionFromStore(collectionId)
      if (!data) {
        console.error(`Collection ${collectionId} not found`)
        return false
      }

      const manifest = JSON.parse(data.manifest) as CollectionManifest
      manifest.metadata.name = newName.trim()
      manifest.metadata.updated = Date.now()

      const updatedData: CollectionData = {
        metadata: manifest.metadata,
        manifest: JSON.stringify(manifest),
      }

      await this.saveCollectionToStore(updatedData)

      console.log(`Collection ${collectionId} renamed to "${newName}"`)
      return true
    } catch (error) {
      console.error("Failed to rename collection:", error)
      return false
    }
  }
}

// Export singleton instance
export const assetManager = new AssetManager()
