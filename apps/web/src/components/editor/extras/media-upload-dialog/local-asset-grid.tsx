import { HardDrive } from "lucide-react"
import { LocalAssetCard } from "./local-asset-card"

interface LocalAssetGridProps {
  assets: Array<{
    id: string
    name: string
    type: string
    size: number
    dataUrl: string
  }>
  selectedAssets: Set<string>
  isLoading: boolean
  hasNoAssets: boolean
  noSearchResults: boolean
  onToggleSelection: (assetId: string) => void
  formatFileSize: (bytes?: number) => string
}

export function LocalAssetGrid({
  assets,
  selectedAssets,
  isLoading,
  hasNoAssets,
  noSearchResults,
  onToggleSelection,
  formatFileSize,
}: LocalAssetGridProps) {
  if (isLoading) {
    return (
      <div className="text-center py-12 text-muted-foreground">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-2"></div>
        <p className="text-sm">Loading files...</p>
      </div>
    )
  }

  if (hasNoAssets) {
    return (
      <div className="text-center py-12 text-muted-foreground">
        <HardDrive className="h-8 w-8 mx-auto mb-2 opacity-50" />
        <p className="text-sm font-medium">No files yet</p>
        <p className="text-xs mt-1">Click &quot;Upload New&quot; to add your first file</p>
      </div>
    )
  }

  if (noSearchResults) {
    return (
      <div className="text-center py-12 text-muted-foreground">
        <p className="text-sm font-medium">No files match your search</p>
        <p className="text-xs mt-1">Try a different search term</p>
      </div>
    )
  }

  return (
    <div className="grid grid-cols-4 gap-3">
      {assets.map((asset) => (
        <LocalAssetCard
          key={asset.id}
          asset={asset}
          isSelected={selectedAssets.has(asset.id)}
          onToggleSelection={onToggleSelection}
          formatFileSize={formatFileSize}
        />
      ))}
    </div>
  )
}
