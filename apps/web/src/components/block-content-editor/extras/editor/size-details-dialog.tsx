import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { HardDrive, FolderArchive, Image, FileText } from "lucide-react"

interface AssetInfo {
  id: string
  name: string
  size: number
  thumbnail?: string
  mimeType?: string
}

interface SizeDetailsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  currentProjectSize: number
  currentProjectAssetsSize: number
  currentProjectAssets: AssetInfo[]
  recommendedSizeKB: number
  formatSize: (sizeInKB: number) => string
  getSizeIndicatorColor: () => string
}

export function SizeDetailsDialog({
  open,
  onOpenChange,
  currentProjectSize,
  currentProjectAssetsSize,
  currentProjectAssets,
  recommendedSizeKB,
  formatSize,
  getSizeIndicatorColor,
}: SizeDetailsDialogProps) {
  // Separate assets and collections
  const collections = currentProjectAssets.filter(asset => asset.mimeType === 'application/collection')
  const regularAssets = currentProjectAssets.filter(asset => asset.mimeType !== 'application/collection')
  
  const totalCollectionsSize = collections.reduce((sum, c) => sum + c.size, 0)
  const totalAssetsSize = regularAssets.reduce((sum, a) => sum + a.size, 0)

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Project Size Details</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          {/* JSON Size */}
          <div className="flex items-center justify-between rounded-lg bg-gray-50 p-3 dark:bg-gray-800">
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">JSON Content:</span>
            <span className={`text-sm font-semibold ${getSizeIndicatorColor()}`}>
              {formatSize(currentProjectSize)}
            </span>
          </div>

          {/* Assets Section */}
          {regularAssets.length > 0 && (
            <div className="space-y-2">
              <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300">
                Assets ({regularAssets.length}):
              </h4>
              <div className="max-h-60 space-y-1.5 overflow-y-auto rounded-lg bg-gray-50 p-2 dark:bg-gray-800">
                {regularAssets.map((asset) => (
                  <div
                    key={asset.id}
                    className="flex items-center gap-2 rounded bg-white px-3 py-2 text-xs dark:bg-gray-900"
                  >
                    {/* Thumbnail */}
                    {asset.thumbnail && asset.mimeType?.startsWith("image/") ? (
                      <img
                        src={asset.thumbnail}
                        alt={asset.name}
                        className="h-10 w-10 rounded border border-gray-200 object-cover dark:border-gray-700"
                      />
                    ) : asset.mimeType?.startsWith("image/") ? (
                      <div className="flex h-10 w-10 items-center justify-center rounded border border-gray-200 bg-blue-50 dark:border-gray-700 dark:bg-blue-900/30">
                        <Image className="h-5 w-5 text-blue-500 dark:text-blue-400" />
                      </div>
                    ) : (
                      <div className="flex h-10 w-10 items-center justify-center rounded border border-gray-200 bg-gray-100 dark:border-gray-700 dark:bg-gray-800">
                        <FileText className="h-5 w-5 text-gray-400 dark:text-gray-600" />
                      </div>
                    )}

                    {/* Name and Size */}
                    <div className="flex min-w-0 flex-1 items-center justify-between">
                      <span className="truncate text-gray-700 dark:text-gray-300" title={asset.name}>
                        {asset.name}
                      </span>
                      <span className="ml-2 whitespace-nowrap font-medium text-blue-600 dark:text-blue-400">
                        {formatSize(asset.size)}
                      </span>
                    </div>
                  </div>
                ))}
              </div>

              {/* Assets Subtotal */}
              <div className="flex items-center justify-between rounded-lg bg-blue-50 p-3 dark:bg-blue-900/30">
                <span className="text-sm font-medium text-blue-700 dark:text-blue-300">Total Assets:</span>
                <span className="text-sm font-semibold text-blue-600 dark:text-blue-400">
                  {formatSize(totalAssetsSize)}
                </span>
              </div>
            </div>
          )}

          {/* Collections Section */}
          {collections.length > 0 && (
            <div className="space-y-2">
              <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300">
                Collections ({collections.length}):
              </h4>
              <div className="max-h-60 space-y-1.5 overflow-y-auto rounded-lg bg-gray-50 p-2 dark:bg-gray-800">
                {collections.map((collection) => (
                  <div
                    key={collection.id}
                    className="flex items-center gap-2 rounded bg-white px-3 py-2 text-xs dark:bg-gray-900"
                  >
                    {/* Collection Icon */}
                    <div className="flex h-10 w-10 items-center justify-center rounded border border-purple-200 bg-purple-50 dark:border-purple-700 dark:bg-purple-900/30">
                      <FolderArchive className="h-5 w-5 text-purple-500 dark:text-purple-400" />
                    </div>

                    {/* Name and Size */}
                    <div className="flex min-w-0 flex-1 items-center justify-between">
                      <span className="truncate text-gray-700 dark:text-gray-300" title={collection.name}>
                        {collection.name}
                      </span>
                      <span className="ml-2 whitespace-nowrap font-medium text-purple-600 dark:text-purple-400">
                        {formatSize(collection.size)}
                      </span>
                    </div>
                  </div>
                ))}
              </div>

              {/* Collections Subtotal */}
              <div className="flex items-center justify-between rounded-lg bg-purple-50 p-3 dark:bg-purple-900/30">
                <span className="text-sm font-medium text-purple-700 dark:text-purple-300">Total Collections:</span>
                <span className="text-sm font-semibold text-purple-600 dark:text-purple-400">
                  {formatSize(totalCollectionsSize)}
                </span>
              </div>
            </div>
          )}

          {/* Total Size */}
          <div className="flex items-center justify-between rounded-lg bg-linear-to-r from-purple-50 to-blue-50 p-3 dark:from-purple-900/30 dark:to-blue-900/30">
            <span className="text-sm font-bold text-gray-800 dark:text-gray-200">Total Project Size:</span>
            <span className={`text-lg font-bold ${getSizeIndicatorColor()}`}>
              {formatSize(currentProjectSize + currentProjectAssetsSize)}
            </span>
          </div>

          {/* Recommended size warning */}
          {currentProjectSize + currentProjectAssetsSize > recommendedSizeKB && (
            <div className="rounded-lg bg-amber-50 p-3 dark:bg-amber-900/30">
              <p className="text-xs text-amber-800 dark:text-amber-200">
                ⚠️ Your project exceeds the recommended size of {formatSize(recommendedSizeKB)}. Consider
                optimizing images or splitting content.
              </p>
            </div>
          )}

          <div className="flex justify-end">
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              Close
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
