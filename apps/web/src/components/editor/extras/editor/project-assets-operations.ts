import { assetManager } from "@/lib/storage/assets/asset-manager"

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
 * Calculate total asset size for current project
 */
export async function calculateProjectAssetsSize(params: CalculateAssetsParams): Promise<void> {
  const { projectId, setCurrentProjectAssetsSize, setCurrentProjectAssets } = params

  if (!projectId) {
    setCurrentProjectAssetsSize(0)
    setCurrentProjectAssets([])
    return
  }

  try {
    // Get all assets with usage information
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
    
    setCurrentProjectAssets(assetsList)
    
    // Calculate total size in KB
    const totalSize = assetsList.reduce((sum, asset) => sum + asset.size, 0)
    
    setCurrentProjectAssetsSize(totalSize)
  } catch (error) {
    console.error("Failed to calculate assets size:", error)
    setCurrentProjectAssetsSize(0)
    setCurrentProjectAssets([])
  }
}
