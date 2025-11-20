"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Plus, Trash2, Edit2 } from "lucide-react"
import type { DisplayConfig, PanelType, AspectRatio } from "./types"
import { cn } from "@/lib/utils"

interface DisplayManagerProps {
  displays: DisplayConfig[]
  activeDisplayId: string
  onSelectDisplay: (displayId: string) => void
  onCreateDisplay: (name: string, aspectRatio: AspectRatio) => void
  onDeleteDisplay: (displayId: string) => void
  onRenameDisplay: (displayId: string, newName: string) => void
  onAddPanel: (type: PanelType, row?: number, col?: number) => void
}

export function DisplayManager({
  displays,
  activeDisplayId,
  onSelectDisplay,
  onCreateDisplay,
  onDeleteDisplay,
  onRenameDisplay,
  onAddPanel,
}: DisplayManagerProps) {
  const [isCreating, setIsCreating] = useState(false)
  const [newDisplayName, setNewDisplayName] = useState("")
  const [newAspectRatio, setNewAspectRatio] = useState<AspectRatio>("2:1")
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState("")
  const [draggedPanelType, setDraggedPanelType] = useState<PanelType | null>(null)

  const handleCreate = () => {
    if (newDisplayName.trim()) {
      onCreateDisplay(newDisplayName.trim(), newAspectRatio)
      setNewDisplayName("")
      setNewAspectRatio("2:1")
      setIsCreating(false)
    }
  }

  const handleStartRename = (display: DisplayConfig) => {
    setEditingId(display.id)
    setEditingName(display.name)
  }

  const handleFinishRename = () => {
    if (editingId && editingName.trim()) {
      onRenameDisplay(editingId, editingName.trim())
    }
    setEditingId(null)
    setEditingName("")
  }

  const handlePanelDragStart = (type: PanelType, e: React.DragEvent) => {
    setDraggedPanelType(type)
    e.dataTransfer.effectAllowed = 'copy'
    e.dataTransfer.setData('panelType', type)
    
    const dragImage = e.currentTarget.cloneNode(true) as HTMLElement
    dragImage.style.opacity = '0.8'
    document.body.appendChild(dragImage)
    e.dataTransfer.setDragImage(dragImage, 0, 0)
    setTimeout(() => document.body.removeChild(dragImage), 0)
  }

  const handlePanelDragEnd = () => {
    setDraggedPanelType(null)
  }

  return (
    <div className="flex items-center justify-between gap-3 flex-wrap">
      {/* Display Tabs */}
      <div className="flex items-center gap-2 flex-wrap">
        {displays.map(display => (
          <div key={display.id} className="flex items-center gap-1">
            {editingId === display.id ? (
              <div className="flex items-center gap-1">
                <Input
                  value={editingName}
                  onChange={(e) => setEditingName(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") handleFinishRename()
                    if (e.key === "Escape") setEditingId(null)
                  }}
                  className="h-7 w-28 text-xs"
                  autoFocus
                  onBlur={handleFinishRename}
                />
              </div>
            ) : (
              <button
                onClick={() => onSelectDisplay(display.id)}
                className={cn(
                  "px-3 py-1.5 rounded text-xs font-medium transition-colors flex items-center gap-2",
                  activeDisplayId === display.id
                    ? "bg-blue-600 text-white shadow-sm"
                    : "bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-400 hover:bg-gray-300 dark:hover:bg-gray-600"
                )}
                title={`Aspect ratio: ${display.aspectRatio}`}
              >
                <span className="text-[10px] opacity-60">
                  {display.aspectRatio === "2:1" ? "🖥" : display.aspectRatio === "1:2" ? "📱" : "⬜"}
                </span>
                {display.name}
                <span className="text-[10px] opacity-70">
                  ({display.panels.length})
                </span>
              </button>
            )}
            
            {activeDisplayId === display.id && (
              <div className="flex items-center gap-0.5">
                <button
                  onClick={() => handleStartRename(display)}
                  className="p-1 hover:bg-gray-200 dark:hover:bg-gray-700 rounded"
                  title="Rename display"
                >
                  <Edit2 className="h-3 w-3" />
                </button>
                {displays.length > 2 && (
                  <button
                    onClick={() => onDeleteDisplay(display.id)}
                    className="p-1 hover:bg-red-100 dark:hover:bg-red-900/30 text-red-600 dark:text-red-400 rounded"
                    title="Delete display"
                  >
                    <Trash2 className="h-3 w-3" />
                  </button>
                )}
              </div>
            )}
          </div>
        ))}

        {isCreating ? (
          <div className="flex items-center gap-2 bg-white dark:bg-gray-700 rounded px-2 py-1.5">
            <Input
              value={newDisplayName}
              onChange={(e) => setNewDisplayName(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleCreate()
                if (e.key === "Escape") {
                  setIsCreating(false)
                  setNewDisplayName("")
                  setNewAspectRatio("2:1")
                }
              }}
              placeholder="Display name"
              className="h-7 w-32 text-xs"
              autoFocus
            />
            <select
              value={newAspectRatio}
              onChange={(e) => setNewAspectRatio(e.target.value as AspectRatio)}
              className="h-7 px-2 text-xs bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded"
              title="Aspect ratio"
            >
              <option value="2:1">🖵 2:1</option>
              <option value="1:1">⬜ 1:1</option>
              <option value="1:2">📱 1:2</option>
            </select>
            <Button
              onClick={handleCreate}
              size="sm"
              className="h-7 px-2 text-xs"
            >
              Create
            </Button>
          </div>
        ) : displays.length < 4 ? (
          <button
            onClick={() => setIsCreating(true)}
            className="px-2 py-1.5 hover:bg-gray-200 dark:hover:bg-gray-700 rounded text-gray-600 dark:text-gray-400 flex items-center gap-1"
            title={`New display (max 4)`}
          >
            <Plus className="h-3.5 w-3.5" />
            <span className="text-xs">Display</span>
          </button>
        ) : null}
      </div>

      {/* Add Panel Buttons - Draggable */}
      <div className="flex items-center gap-1">
        <span className="text-xs text-gray-600 dark:text-gray-400 mr-1">Drag to add:</span>
        <button
          draggable
          data-panel-type="explorer"
          onDragStart={(e) => handlePanelDragStart("explorer", e)}
          onDragEnd={handlePanelDragEnd}
          className={cn(
            "px-2 py-1 text-xs bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded hover:bg-green-200 dark:hover:bg-green-900/50 cursor-grab active:cursor-grabbing transition-opacity",
            draggedPanelType === "explorer" && "opacity-50"
          )}
        >
          Explorer
        </button>
        <button
          draggable
          data-panel-type="editor"
          onDragStart={(e) => handlePanelDragStart("editor", e)}
          onDragEnd={handlePanelDragEnd}
          className={cn(
            "px-2 py-1 text-xs bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 rounded hover:bg-blue-200 dark:hover:bg-blue-900/50 cursor-grab active:cursor-grabbing transition-opacity",
            draggedPanelType === "editor" && "opacity-50"
          )}
        >
          Editor
        </button>
        <button
          draggable
          data-panel-type="output"
          onDragStart={(e) => handlePanelDragStart("output", e)}
          onDragEnd={handlePanelDragEnd}
          className={cn(
            "px-2 py-1 text-xs bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 rounded hover:bg-purple-200 dark:hover:bg-purple-900/50 cursor-grab active:cursor-grabbing transition-opacity",
            draggedPanelType === "output" && "opacity-50"
          )}
        >
          Output
        </button>
      </div>
    </div>
  )
}
