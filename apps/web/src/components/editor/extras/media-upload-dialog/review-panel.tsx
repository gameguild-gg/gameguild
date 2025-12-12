import { Send, Upload } from "lucide-react"
import { Button } from "@/components/ui/button"
import { ReviewItem } from "./review-item"

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

interface ReviewPanelProps {
  pendingUploads: PendingUpload[]
  onRemove: (id: string) => void
  onCompressionSettings: (file: File) => void
  onSubmit: () => void
  formatFileSize: (bytes?: number) => string
  isImageFile: (file: File) => boolean
}

export function ReviewPanel({
  pendingUploads,
  onRemove,
  onCompressionSettings,
  onSubmit,
  formatFileSize,
  isImageFile,
}: ReviewPanelProps) {
  return (
    <div className="w-80 border-l dark:border-gray-700 pl-6 flex flex-col min-h-0">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold">Review Files</h3>
        <span className="text-sm text-muted-foreground">
          {pendingUploads.length} item{pendingUploads.length !== 1 ? "s" : ""}
        </span>
      </div>

      <div className="space-y-3 flex-1 overflow-y-auto min-h-0">
        {pendingUploads.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground">
            <Upload className="h-8 w-8 mx-auto mb-2 opacity-50" />
            <p className="text-sm">No files added yet</p>
            <p className="text-xs">Add files to review them here</p>
          </div>
        ) : (
          pendingUploads.map((upload) => (
            <ReviewItem
              key={upload.id}
              upload={upload}
              onRemove={onRemove}
              onCompressionSettings={onCompressionSettings}
              formatFileSize={formatFileSize}
              isImageFile={isImageFile}
            />
          ))
        )}
      </div>

      <div className="mt-6 pt-4 border-t dark:border-gray-700">
        <Button onClick={onSubmit} className="w-full h-12 text-base" disabled={pendingUploads.length === 0}>
          <Send className="h-4 w-4 mr-2" />
          Send{" "}
          {pendingUploads.length > 0
            ? `${pendingUploads.length} item${pendingUploads.length !== 1 ? "s" : ""}`
            : "Files"}
        </Button>
      </div>
    </div>
  )
}
