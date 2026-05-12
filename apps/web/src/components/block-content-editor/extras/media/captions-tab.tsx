"use client"

import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Input } from "@/components/ui/input"
import type { BaseMediaData } from "@/components/block-content-editor/nodes/base/media-node-base"
import { AssetImage } from "./asset-image"

interface CaptionsTabProps {
  items: BaseMediaData[]
  onItemsChange: (items: BaseMediaData[]) => void
  globalCaption: string
  onGlobalCaptionChange: (caption: string) => void
}

export function CaptionsTab({ items, onItemsChange, globalCaption, onGlobalCaptionChange }: CaptionsTabProps) {
  const handleItemCaptionChange = (index: number, caption: string) => {
    const newItems = [...items]
    const item = newItems[index]
    if (item) {
      newItems[index] = { ...item, caption }
      onItemsChange(newItems)
    }
  }

  const handleItemAltChange = (index: number, alt: string) => {
    const newItems = [...items]
    const item = newItems[index]
    if (item) {
      newItems[index] = { ...item, alt }
      onItemsChange(newItems)
    }
  }

  return (
    <div className="space-y-6">
      {/* Global Caption */}
      <div className="space-y-2 pb-4 border-b border-gray-200 dark:border-gray-700">
        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Global Caption (Optional)
        </Label>
        <Textarea
          value={globalCaption}
          onChange={(e) => onGlobalCaptionChange(e.target.value)}
          placeholder="This caption will appear below the entire gallery..."
          rows={3}
          className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600"
        />
        <p className="text-xs text-gray-500 dark:text-gray-400">
          This caption applies to the entire gallery/collection
        </p>
      </div>

      {/* Individual Captions */}
      <div className="space-y-4">
        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Individual Item Captions
        </Label>
        
        <div className="space-y-4 max-h-[400px] overflow-y-auto pr-2">
          {items.length === 0 ? (
            <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 rounded-lg border-2 border-dashed">
              <p>No items to caption</p>
              <p className="text-sm">Add media in the Media tab first</p>
            </div>
          ) : (
            items.map((item, index) => (
              <div
                key={index}
                className="p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700 space-y-3"
              >
                {/* Item Preview */}
                <div className="flex items-center gap-3 mb-2">
                  <div className="w-12 h-12 bg-gray-200 dark:bg-gray-700 rounded overflow-hidden shrink-0">
                    {item.type === "image" && item.src && (
                      <AssetImage src={item.src} alt="" className="w-full h-full object-cover" />
                    )}
                    {item.type !== "image" && (
                      <div className="w-full h-full flex items-center justify-center text-xs text-gray-500">
                        {item.type}
                      </div>
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="text-sm font-medium text-gray-900 dark:text-gray-100 capitalize">
                      {item.type} #{index + 1}
                    </div>
                    <div className="text-xs text-gray-500 dark:text-gray-400 truncate">
                      {item.src || "No URL"}
                    </div>
                  </div>
                </div>

                {/* Alt Text (for images) */}
                {item.type === "image" && (
                  <div className="space-y-1">
                    <Label className="text-xs text-gray-600 dark:text-gray-400">
                      Alt Text (Accessibility)
                    </Label>
                    <Input
                      value={item.alt || ""}
                      onChange={(e) => handleItemAltChange(index, e.target.value)}
                      placeholder="Describe this image..."
                      className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 text-sm"
                    />
                  </div>
                )}

                {/* Caption */}
                <div className="space-y-1">
                  <Label className="text-xs text-gray-600 dark:text-gray-400">
                    Caption
                  </Label>
                  <Textarea
                    value={item.caption || ""}
                    onChange={(e) => handleItemCaptionChange(index, e.target.value)}
                    placeholder="Optional caption for this item..."
                    rows={2}
                    className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 text-sm"
                  />
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  )
}
