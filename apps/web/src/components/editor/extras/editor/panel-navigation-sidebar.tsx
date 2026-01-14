"use client"

import { Button } from "@/components/ui/button"
import { Plus, Trash2, GripVertical, Layout, LayoutGrid } from "lucide-react"
import { useState } from "react"
import { cn } from "@/lib/utils"
import type { PanelData, PanelLayoutType } from "@/lib/storage/editor/panel-structure"

interface PanelNavigationSidebarProps {
  panels: PanelData[]
  currentPanelIndex: number
  onPanelSelect: (index: number) => void
  onPanelAdd: (type: PanelLayoutType, position?: number) => void
  onPanelRemove: (panelId: string) => void
  onPanelReorder: (fromIndex: number, toIndex: number) => void
  onPanelNameChange: (panelId: string, name: string) => void
}

export function PanelNavigationSidebar({
  panels,
  currentPanelIndex,
  onPanelSelect,
  onPanelAdd,
  onPanelRemove,
  onPanelReorder,
  onPanelNameChange,
}: PanelNavigationSidebarProps) {
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null)
  const [editingPanelId, setEditingPanelId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState("")

  const handleDragStart = (index: number) => {
    setDraggedIndex(index)
  }

  const handleDragOver = (e: React.DragEvent, index: number) => {
    e.preventDefault()
    if (draggedIndex === null || draggedIndex === index) return
    
    onPanelReorder(draggedIndex, index)
    setDraggedIndex(index)
  }

  const handleDragEnd = () => {
    setDraggedIndex(null)
  }

  const handleNameEdit = (panel: PanelData) => {
    setEditingPanelId(panel.id)
    setEditingName(panel.name || `Panel ${panel.order + 1}`)
  }

  const handleNameSave = (panelId: string) => {
    if (editingName.trim()) {
      onPanelNameChange(panelId, editingName.trim())
    }
    setEditingPanelId(null)
  }

  const handleNameCancel = () => {
    setEditingPanelId(null)
    setEditingName("")
  }

  return (
    <div className="w-64 h-full bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800 flex flex-col">
      {/* Header */}
      <div className="p-4 border-b border-gray-200 dark:border-gray-800">
        <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
          Panels ({panels.length})
        </h3>
        
        {/* Add Panel Buttons */}
        <div className="flex gap-2">
          <Button
            onClick={() => onPanelAdd("single")}
            variant="outline"
            size="sm"
            className="flex-1 text-xs"
            title="Add Single Panel"
          >
            <Layout className="h-3 w-3 mr-1" />
            Single
          </Button>
          <Button
            onClick={() => onPanelAdd("dual")}
            variant="outline"
            size="sm"
            className="flex-1 text-xs"
            title="Add Dual Panel"
          >
            <LayoutGrid className="h-3 w-3 mr-1" />
            Dual
          </Button>
        </div>
      </div>

      {/* Panels List */}
      <div className="flex-1 overflow-y-auto p-2">
        {panels.length === 0 ? (
          <div className="text-center py-8 text-sm text-gray-500">
            No panels yet. Add one to get started.
          </div>
        ) : (
          <div className="space-y-2">
            {panels.map((panel, index) => (
              <div
                key={panel.id}
                draggable
                onDragStart={() => handleDragStart(index)}
                onDragOver={(e) => handleDragOver(e, index)}
                onDragEnd={handleDragEnd}
                onClick={() => onPanelSelect(index)}
                className={cn(
                  "group relative p-3 rounded-lg border-2 cursor-pointer transition-all",
                  currentPanelIndex === index
                    ? "border-blue-500 bg-blue-50 dark:bg-blue-900/20"
                    : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600",
                  draggedIndex === index && "opacity-50"
                )}
              >
                {/* Drag Handle */}
                <div className="absolute left-1 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 transition-opacity cursor-grab active:cursor-grabbing">
                  <GripVertical className="h-4 w-4 text-gray-400" />
                </div>

                <div className="pl-6">
                  {/* Panel Number and Type */}
                  <div className="flex items-center justify-between mb-1">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-semibold text-gray-500 dark:text-gray-400">
                        #{index + 1}
                      </span>
                      {panel.type === "dual" ? (
                        <LayoutGrid className="h-3 w-3 text-blue-500" />
                      ) : (
                        <Layout className="h-3 w-3 text-green-500" />
                      )}
                    </div>
                    
                    {/* Delete Button */}
                    {panels.length > 1 && (
                      <Button
                        onClick={(e) => {
                          e.stopPropagation()
                          onPanelRemove(panel.id)
                        }}
                        variant="ghost"
                        size="sm"
                        className="h-6 w-6 p-0 opacity-0 group-hover:opacity-100 transition-opacity"
                        title="Remove Panel"
                      >
                        <Trash2 className="h-3 w-3 text-red-500" />
                      </Button>
                    )}
                  </div>

                  {/* Panel Name (Editable) */}
                  {editingPanelId === panel.id ? (
                    <input
                      type="text"
                      value={editingName}
                      onChange={(e) => setEditingName(e.target.value)}
                      onBlur={() => handleNameSave(panel.id)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") handleNameSave(panel.id)
                        if (e.key === "Escape") handleNameCancel()
                      }}
                      onClick={(e) => e.stopPropagation()}
                      className="w-full px-2 py-1 text-xs border border-blue-500 rounded bg-white dark:bg-gray-800 focus:outline-none"
                      autoFocus
                    />
                  ) : (
                    <div
                      onDoubleClick={(e) => {
                        e.stopPropagation()
                        handleNameEdit(panel)
                      }}
                      className="text-sm font-medium text-gray-700 dark:text-gray-300 truncate"
                      title={panel.name || `Panel ${index + 1}`}
                    >
                      {panel.name || `Panel ${index + 1}`}
                    </div>
                  )}

                  {/* Panel Type Badge */}
                  <div className="mt-1">
                    <span className={cn(
                      "inline-block px-2 py-0.5 text-xs rounded",
                      panel.type === "dual"
                        ? "bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300"
                        : "bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300"
                    )}>
                      {panel.type === "dual" ? "Dual Panel" : "Single Panel"}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Footer with Add Button */}
      <div className="p-3 border-t border-gray-200 dark:border-gray-800">
        <Button
          onClick={() => onPanelAdd("single")}
          variant="outline"
          size="sm"
          className="w-full"
        >
          <Plus className="h-4 w-4 mr-2" />
          Add Panel
        </Button>
      </div>
    </div>
  )
}
