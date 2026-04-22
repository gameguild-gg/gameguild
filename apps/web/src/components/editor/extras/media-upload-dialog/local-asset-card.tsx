import { ImageIcon } from "lucide-react"

interface LocalAssetCardProps {
  asset: {
    id: string
    name: string
    type: string
    size: number
    dataUrl: string
  }
  isSelected: boolean
  onToggleSelection: (assetId: string) => void
  formatFileSize: (bytes?: number) => string
}

export function LocalAssetCard({ asset, isSelected, onToggleSelection, formatFileSize }: LocalAssetCardProps) {
  return (
    <div
      onClick={() => onToggleSelection(asset.id)}
      className={`relative cursor-pointer rounded-lg border-2 transition-all hover:shadow-md ${
        isSelected
          ? "border-blue-500 bg-blue-50 dark:bg-blue-950 dark:border-blue-400"
          : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
      }`}
    >
      <div className="aspect-video bg-gray-100 dark:bg-gray-800 rounded-t-lg overflow-hidden">
        {asset.type.startsWith("image/") ? (
          <img src={asset.dataUrl} alt={asset.name} className="w-full h-full object-cover" />
        ) : (
          <div className="w-full h-full flex items-center justify-center">
            <ImageIcon className="h-8 w-8 text-gray-400" />
          </div>
        )}
      </div>
      <div className="p-2 bg-white dark:bg-gray-950 rounded-b-lg">
        <p className="text-xs font-medium truncate" title={asset.name}>
          {asset.name}
        </p>
        <p className="text-xs text-muted-foreground">{formatFileSize(asset.size)}</p>
      </div>
      {isSelected && (
        <div className="absolute top-2 right-2 bg-blue-500 text-white rounded-full p-1">
          <svg className="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
            <path
              fillRule="evenodd"
              d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
              clipRule="evenodd"
            />
          </svg>
        </div>
      )}
    </div>
  )
}
