import { getDefaultBrowserAssetRepository } from "@game-guild/assets/browser"

const assetRepository = getDefaultBrowserAssetRepository()

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

export async function calculateProjectAssetsSize({
  projectId,
  setCurrentProjectAssetsSize,
  setCurrentProjectAssets,
}: CalculateAssetsParams): Promise<void> {
  if (!projectId) {
    setCurrentProjectAssetsSize(0)
    setCurrentProjectAssets([])
    return
  }

  try {
    const assets = await assetRepository.listUsedByScope({ type: "project", id: projectId })
    const items = assets.map((asset) => ({
      id: asset.uri,
      name: asset.name,
      size: asset.size / 1024,
      mimeType: asset.mimeType,
    }))
    setCurrentProjectAssets(items)
    setCurrentProjectAssetsSize(items.reduce((total, item) => total + item.size, 0))
  } catch (error) {
    console.error("Failed to calculate project asset size:", error)
    setCurrentProjectAssetsSize(0)
    setCurrentProjectAssets([])
  }
}
