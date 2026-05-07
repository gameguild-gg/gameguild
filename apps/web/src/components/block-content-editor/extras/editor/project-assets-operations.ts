import { assetManager } from "@/components/block-content-editor/lib/storage/assets/asset-manager"
import type { CollectionStructure } from "@/components/block-content-editor/lib/storage/assets/collection-types"

// Parameter interface
export interface CalculateAssetsParams {
  projectId: string
  setCurrentProjectAssetsSize: (size: number) => void
  setCurrentProjectAssets: (assets: Array<{ 
    id: string
    name: string
    size: number
    thumbnail?: string
    mimeType?: string 
  }>) => void
}

/**
 * Calculate total size recursively for collection structure
 */
function calculateCollectionSize(structure: CollectionStructure): number {
  let size = 0
  
  // Calculate size from files
  if (structure.files) {
    structure.files.forEach((file) => {
      if (file.size) {
        size += file.size
      }
    })
  }
  
  // Recursively calculate size from folders
  if (structure.folders && Array.isArray(structure.folders)) {
    structure.folders.forEach((folder) => {
      size += calculateCollectionSize(folder as CollectionStructure)
    })
  }
  
  return size
}

/**
 * Calculate total asset size for current project (including collections)
 */
export async function calculateProjectAssetsSize(params: CalculateAssetsParams): Promise<void> {
  const { projectId, setCurrentProjectAssetsSize, setCurrentProjectAssets } = params

  if (!projectId) {
    setCurrentProjectAssetsSize(0)
    setCurrentProjectAssets([])
    return
  }

  try {
    // Get all regular assets with usage information
    const assetsWithUsage = await assetManager.listAssetsWithUsage()
    
    // Filter assets used by this project
    const projectAssets = assetsWithUsage.filter(asset => 
      asset.projects && asset.projects.includes(projectId)
    )
    
    // Store individual assets with their info and load thumbnails
    const assetsListPromises = projectAssets.map(async (asset) => {
      let thumbnailUrl: string | undefined
      
      try {
        // Load the full asset to get its data URL
        const assetData = await assetManager.getAsset(asset.id)
        if (assetData && assetData.data) {
          thumbnailUrl = assetData.data
        }
      } catch (error) {
        console.error(`Failed to load thumbnail for asset ${asset.id}:`, error)
      }
      
      return {
        id: asset.id,
        name: asset.name || asset.id,
        size: (asset.size || 0) / 1024, // Convert to KB
        thumbnail: thumbnailUrl,
        mimeType: asset.mimeType
      }
    })
    
    const assetsList = await Promise.all(assetsListPromises)
    
    // Get collections used by this project (using the same usage tracking as regular assets)
    const collectionsWithUsage = await assetManager.listCollections()
    
    // Filter collections - only include those used by this project
    const projectCollections = collectionsWithUsage.filter(collection => {
      // Check if this collection has usage data for this project
      const usageData = assetsWithUsage.find(a => a.id === collection.id)
      return usageData && usageData.projects && usageData.projects.includes(projectId)
    })
    
    const collectionPromises = projectCollections.map(async (collection) => {
      try {
        const manifest = await assetManager.getCollection(collection.id)
        if (manifest) {
          const collectionSize = calculateCollectionSize(manifest.structure)
          
          return {
            id: collection.id,
            name: collection.name || collection.id,
            size: collectionSize / 1024, // Convert to KB
            thumbnail: undefined,
            mimeType: 'application/collection'
          }
        }
      } catch (error) {
        console.error(`Failed to process collection ${collection.id}:`, error)
      }
      return null
    })
    
    const collectionsResults = await Promise.all(collectionPromises)
    const collectionsList = collectionsResults.filter(Boolean) as Array<{
      id: string
      name: string
      size: number
      thumbnail?: string
      mimeType?: string
    }>
    
    // Combine assets and collections
    const allItems = [...assetsList, ...collectionsList]
    setCurrentProjectAssets(allItems)
    
    // Calculate total size in KB
    const totalSize = allItems.reduce((sum, item) => sum + item.size, 0)
    
    setCurrentProjectAssetsSize(totalSize)
  } catch (error) {
    console.error("Failed to calculate assets size:", error)
    setCurrentProjectAssetsSize(0)
    setCurrentProjectAssets([])
  }
}
