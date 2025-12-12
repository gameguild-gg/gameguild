import { ImageIcon, Settings, Zap } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Trash2 } from "lucide-react"

interface PendingUpload {
  id: string
  type: "file" | "url"
  data: string
  name?: string
  size?: number
  file?: File
  compressed?: boolean
  originalSize?: number
  compressionRatio?: number
  needsCompression?: boolean
  isCompressing?: boolean
}

interface ReviewItemProps {
  upload: PendingUpload
  onRemove: (id: string) => void
  onCompressionSettings: (file: File) => void
  formatFileSize: (bytes?: number) => string
  isImageFile: (file: File) => boolean
}

export function ReviewItem({
  upload,
  onRemove,
  onCompressionSettings,
  formatFileSize,
  isImageFile,
}: ReviewItemProps) {
  return (
    <div className="p-3 bg-gray-50 dark:bg-gray-900 rounded-lg space-y-2 border border-gray-200 dark:border-gray-700">
      <div className="flex items-start gap-3">
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium truncate" title={upload.name}>
            {upload.name || "Unnamed file"}
          </p>
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <span
              className={`px-2 py-1 rounded text-xs ${
                upload.type === "file"
                  ? "bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-400"
                  : "bg-green-100 text-green-700 dark:bg-green-950 dark:text-green-400"
              }`}
            >
              {upload.type === "file" ? "File" : "URL"}
            </span>
            {upload.size && <span>{formatFileSize(upload.size)}</span>}
          </div>
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onRemove(upload.id)}
          className="h-8 w-8 p-0 hover:bg-red-100 dark:hover:bg-red-950 hover:text-red-600 dark:hover:text-red-400"
        >
          <Trash2 className="h-4 w-4" />
        </Button>
      </div>

      {upload.type === "file" && upload.file && isImageFile(upload.file) && (
        <div className="space-y-2">
          {upload.compressed && (
            <div className="flex items-center gap-2">
              <Badge variant="secondary" className="text-xs">
                <Zap className="h-3 w-3 mr-1" />
                Compressed -{Math.round(upload.compressionRatio || 0)}%
              </Badge>
              <span className="text-xs text-muted-foreground">
                {formatFileSize(upload.originalSize)} → {formatFileSize(upload.size)}
              </span>
            </div>
          )}

          {upload.needsCompression && (
            <div className="space-y-2">
              <Badge variant="outline" className="text-xs">
                <ImageIcon className="h-3 w-3 mr-1" />
                Compression Recommended
              </Badge>
              <Button
                variant="outline"
                size="sm"
                onClick={() => upload.file && onCompressionSettings(upload.file)}
                className="w-full h-7 text-xs"
                disabled={upload.isCompressing}
              >
                {upload.isCompressing ? (
                  <>
                    <div className="animate-spin rounded-full h-3 w-3 border-b border-current mr-1" />
                    Compressing...
                  </>
                ) : (
                  <>
                    <Settings className="h-3 w-3 mr-1" />
                    Configure Compression
                  </>
                )}
              </Button>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
