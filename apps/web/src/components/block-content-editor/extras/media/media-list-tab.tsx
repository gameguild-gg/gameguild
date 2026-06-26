"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Plus, Trash2, Image, Video, Music } from "lucide-react"
import { MediaUploadDialog } from "@/components/block-content-editor/extras/media-upload-dialog"
import type { BaseMediaData } from "@/components/block-content-editor/nodes/base/media-node-base"
import { AssetImage } from "./asset-image"

interface MediaListTabProps {
  items: BaseMediaData[]
  onItemsChange: (items: BaseMediaData[]) => void
  allowMixedTypes?: boolean
  defaultType?: "image" | "video" | "audio"
}

export function MediaListTab({ items, onItemsChange, allowMixedTypes = false, defaultType = "image" }: MediaListTabProps) {
  const [showUploadDialog, setShowUploadDialog] = useState(false)
  const [uploadType, setUploadType] = useState<"image" | "video" | "audio">(defaultType)

  const handleAddMedia = (type: "image" | "video" | "audio") => {
    setUploadType(type)
    setShowUploadDialog(true)
  }

  const handleMediaSelected = (result: any) => {
    if (Array.isArray(result)) {
      // Multiple files
      const newItems = result.map((item) => ({
        type: uploadType,
        src: item.data,
        alt: item.name || "",
        caption: "",
        size: 100,
        embedType: "direct",
        embedAudioType: "direct",
        videoType: "video/mp4",
        audioType: "audio/mpeg",
      } as BaseMediaData))
      onItemsChange([...items, ...newItems])
    } else {
      // Single file
      const newItem: BaseMediaData = {
        type: uploadType,
        src: result.data,
        alt: result.name || "",
        caption: "",
        size: 100,
        embedType: "direct",
        embedAudioType: "direct",
        videoType: "video/mp4",
        audioType: "audio/mpeg",
      }
      onItemsChange([...items, newItem])
    }
  }

  const handleRemoveItem = (index: number) => {
    const newItems = items.filter((_, i) => i !== index)
    onItemsChange(newItems)
  }

  const getMediaIcon = (type: string) => {
    switch (type) {
      case "image":
        return <Image className="h-4 w-4" />
      case "video":
        return <Video className="h-4 w-4" />
      case "audio":
        return <Music className="h-4 w-4" />
      default:
        return <Image className="h-4 w-4" />
    }
  }

  return (
    <div className="space-y-4">
      {/* Add Media Buttons */}
      <div className="flex gap-2">
        <Button
          variant="outline"
          size="sm"
          onClick={() => handleAddMedia("image")}
          className="flex items-center gap-2"
        >
          <Image className="h-4 w-4" />
          Add Image
        </Button>
        {/*allowMixedTypes && (
          <>
            <Button
              variant="outline"
              size="sm"
              onClick={() => handleAddMedia("video")}
              className="flex items-center gap-2"
            >
              <Video className="h-4 w-4" />
              Add Video
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => handleAddMedia("audio")}
              className="flex items-center gap-2"
            >
              <Music className="h-4 w-4" />
              Add Audio
            </Button>
          </>
        )*/}
      </div>

      {/* Media List */}
      <div className="space-y-2 max-h-[500px] overflow-y-auto">
        {items.length === 0 ? (
          <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 rounded-lg border-2 border-dashed">
            <Plus className="h-8 w-8 mx-auto mb-2 opacity-50" />
            <p>No media items yet</p>
            <p className="text-sm">Click the buttons above to add media</p>
          </div>
        ) : (
          items.map((item, index) => (
            <div
              key={index}
              className="flex items-center gap-3 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
            >
              {/* Thumbnail */}
              <div className="w-16 h-16 bg-gray-200 dark:bg-gray-700 rounded overflow-hidden shrink-0">
                {item.type === "image" && item.src && (
                  <AssetImage src={item.src} alt={item.alt || ""} className="w-full h-full object-cover" />
                )}
                {item.type === "video" && (
                  <div className="w-full h-full flex items-center justify-center">
                    <Video className="h-6 w-6 text-gray-400" />
                  </div>
                )}
                {item.type === "audio" && (
                  <div className="w-full h-full flex items-center justify-center">
                    <Music className="h-6 w-6 text-gray-400" />
                  </div>
                )}
              </div>

              {/* Info */}
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  {getMediaIcon(item.type)}
                  <span className="text-sm font-medium capitalize text-gray-900 dark:text-gray-100">
                    {item.type}
                  </span>
                  <span className="text-xs text-gray-500 dark:text-gray-400">#{index + 1}</span>
                </div>
                <p className="text-xs text-gray-600 dark:text-gray-400 truncate mt-1">
                  {item.src || "No URL"}
                </p>
              </div>

              {/* Actions */}
              <Button
                variant="ghost"
                size="sm"
                onClick={() => handleRemoveItem(index)}
                className="text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-900/20"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>
          ))
        )}
      </div>

      {/* Upload Dialog */}
      <MediaUploadDialog
        open={showUploadDialog}
        onOpenChange={setShowUploadDialog}
        onMediaSelected={handleMediaSelected}
        title={`Add ${uploadType}`}
        acceptTypes={uploadType === "image" ? "image/*" : uploadType === "video" ? "video/*" : "audio/*"}
        urlPlaceholder={`https://example.com/${uploadType}.${uploadType === "image" ? "jpg" : uploadType === "video" ? "mp4" : "mp3"}`}
        multiple={true}
      />
    </div>
  )
}
