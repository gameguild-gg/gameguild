"use client"

import { useState } from "react"
import { Input } from "@/components/ui/input"
import { Plus, Trash2, Edit2 } from "lucide-react"
import type { DisplayConfig, PanelType } from "./types"
import { getTemplatesByScope, type LayoutTemplate as Template, type TemplateSilhouette } from "./templates/templates"
import { DeleteConfirmDialog } from "../dialogs/delete-confirm-dialog"
import { cn } from "@/lib/utils"

interface DisplayManagerProps {
  displays: DisplayConfig[]
  activeDisplayId: string
  /**
   * Scope of the currently active display, used to filter the template strip.
   * - "compact": Base display (embed-sized).
   * - "expanded": secondary displays (Mirror, Test, custom).
   */
  activeDisplayScope: "compact" | "expanded"
  /**
   * Panel types already present in the active display — used to disable the
   * add-panel button for types capped at one per layout (full-editor and
   * focus-editor).
   */
  existingPanelTypes?: Set<PanelType>
  onSelectDisplay: (displayId: string) => void
  onCreateDisplay: (name: string, templateId: string) => void
  onDeleteDisplay: (displayId: string) => void
  onRenameDisplay: (displayId: string, newName: string) => void
  onApplyTemplate: (displayId: string, templateId: string) => void
  onAddPanel: (type: PanelType) => void
}

/**
 * Inline editing toolbar shown on the Title row while the layout is in edit
 * mode. Contains, all visible at once (no popups):
 *   - Display tabs with rename / delete inline actions
 *   - "+ Display" button (creates from the first expanded template)
 *   - Template strip filtered by the active display's scope, applies on click
 *   - Add-panel quick buttons for the active display
 */
export function DisplayManager({
  displays,
  activeDisplayId,
  activeDisplayScope,
  existingPanelTypes,
  onSelectDisplay,
  onCreateDisplay,
  onDeleteDisplay,
  onRenameDisplay,
  onApplyTemplate,
  onAddPanel,
}: DisplayManagerProps) {
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState("")
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)

  const pendingDeleteDisplay = pendingDeleteId
    ? displays.find(d => d.id === pendingDeleteId)
    : undefined

  const confirmDelete = () => {
    if (pendingDeleteId) {
      onDeleteDisplay(pendingDeleteId)
      setPendingDeleteId(null)
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

  const handleCreateDisplay = () => {
    const expanded = getTemplatesByScope("expanded")
    const fallback = expanded[0]
    if (!fallback) return
    onCreateDisplay(fallback.name, fallback.id)
  }

  const activeDisplay = displays.find(d => d.id === activeDisplayId)
  const templates = getTemplatesByScope(activeDisplayScope)

  return (
    <div className="flex items-center gap-2 flex-wrap">
      {/* Display tabs */}
      <div className="flex items-center gap-1 flex-wrap">
        {displays.map(display => (
          <div key={display.id} className="flex items-center gap-0.5">
            {editingId === display.id ? (
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
            ) : (
              <button
                onClick={() => onSelectDisplay(display.id)}
                className={cn(
                  "px-2.5 py-1 rounded text-xs font-medium transition-colors",
                  activeDisplayId === display.id
                    ? "bg-blue-600 text-white shadow-sm"
                    : "bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-400 hover:bg-gray-300 dark:hover:bg-gray-600",
                )}
                title={display.name}
              >
                {display.name}
              </button>
            )}

            {activeDisplayId === display.id && editingId !== display.id && (
              <>
                <button
                  onClick={() => handleStartRename(display)}
                  className="p-1 hover:bg-gray-200 dark:hover:bg-gray-700 rounded text-gray-600 dark:text-gray-400"
                  title="Rename display"
                >
                  <Edit2 className="h-3 w-3" />
                </button>
                {displays.length > 1 && (
                  <button
                    onClick={() => setPendingDeleteId(display.id)}
                    className="p-1 hover:bg-red-100 dark:hover:bg-red-900/30 text-red-600 dark:text-red-400 rounded"
                    title="Delete display"
                  >
                    <Trash2 className="h-3 w-3" />
                  </button>
                )}
              </>
            )}
          </div>
        ))}

        <button
          onClick={handleCreateDisplay}
          className="px-1.5 py-1 hover:bg-gray-200 dark:hover:bg-gray-700 rounded text-gray-600 dark:text-gray-400 flex items-center gap-0.5"
          title="New display"
        >
          <Plus className="h-3.5 w-3.5" />
          <span className="text-xs">Display</span>
        </button>
      </div>

      <div className="h-6 w-px bg-gray-300 dark:bg-gray-700" />

      {/* Template strip — filtered by scope of active display */}
      <div className="flex items-center gap-1 flex-wrap">
        <span className="text-[11px] uppercase tracking-wide text-gray-500 dark:text-gray-400 mr-1">
          Templates
        </span>
        {templates.map(template => {
          const isActive = template.id === activeDisplay?.templateId
          return (
            <button
              key={template.id}
              onClick={() => onApplyTemplate(activeDisplayId, template.id)}
              className={cn(
                "flex items-center gap-1.5 p-1 rounded border transition-colors",
                isActive
                  ? "border-blue-500 bg-blue-50 dark:bg-blue-900/20"
                  : "border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-blue-400 dark:hover:border-blue-500",
              )}
              title={`${template.name} — ${template.description}`}
            >
              <div className="h-5 w-8 bg-gray-100 dark:bg-gray-950 rounded overflow-hidden">
                <TemplatePreview schema={template.preview.schema} />
              </div>
              <span className="text-[11px] font-medium text-gray-700 dark:text-gray-300">
                {template.name}
              </span>
            </button>
          )
        })}
      </div>

      <div className="h-6 w-px bg-gray-300 dark:bg-gray-700" />

      {/* Add panel quick buttons */}
      <div className="flex items-center gap-1 flex-wrap">
        <span className="text-[11px] uppercase tracking-wide text-gray-500 dark:text-gray-400 mr-1">
          Add panel
        </span>
        <PanelAddButton label="Explorer" tone="green" onClick={() => onAddPanel("explorer")} />
        <PanelAddButton
          label="Full Editor"
          tone="blue"
          onClick={() => onAddPanel("full-editor")}
          disabled={existingPanelTypes?.has("full-editor")}
          disabledTitle="Only one Full Editor allowed per display"
        />
        <PanelAddButton
          label="Focus Editor"
          tone="cyan"
          onClick={() => onAddPanel("focus-editor")}
          disabled={existingPanelTypes?.has("focus-editor")}
          disabledTitle="Only one Focus Editor allowed per display"
        />
        <PanelAddButton label="Output" tone="purple" onClick={() => onAddPanel("output")} />
      </div>

      <DeleteConfirmDialog
        open={pendingDeleteId !== null}
        onOpenChange={(open) => {
          if (!open) setPendingDeleteId(null)
        }}
        title="Delete display"
        itemName={pendingDeleteDisplay?.name}
        itemType="display"
        onConfirm={confirmDelete}
      />
    </div>
  )
}

// ---------------------------------------------------------------------------
// Add-panel button

function PanelAddButton({
  label,
  tone,
  onClick,
  disabled = false,
  disabledTitle,
}: {
  label: string
  tone: "green" | "blue" | "cyan" | "purple"
  onClick: () => void
  disabled?: boolean
  disabledTitle?: string
}) {
  const toneClass = {
    green: "bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 hover:bg-green-200 dark:hover:bg-green-900/50",
    blue: "bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 hover:bg-blue-200 dark:hover:bg-blue-900/50",
    cyan: "bg-cyan-100 dark:bg-cyan-900/30 text-cyan-700 dark:text-cyan-400 hover:bg-cyan-200 dark:hover:bg-cyan-900/50",
    purple: "bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 hover:bg-purple-200 dark:hover:bg-purple-900/50",
  }[tone]
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={cn(
        "px-2 py-1 text-[11px] rounded transition-colors",
        toneClass,
        disabled && "opacity-40 cursor-not-allowed hover:bg-transparent",
      )}
      title={disabled ? (disabledTitle ?? `${label} already present`) : `Add ${label} panel`}
    >
      + {label}
    </button>
  )
}

// ---------------------------------------------------------------------------
// Template silhouette preview

function TemplatePreview({ schema }: { schema: TemplateSilhouette }) {
  if (schema.kind === "leaf") {
    return <div className={cn("h-full w-full", leafToneFor(schema.type))} />
  }
  const isHorizontal = schema.direction === "horizontal"
  return (
    <div className={cn("h-full w-full flex gap-px", isHorizontal ? "flex-row" : "flex-col")}>
      {schema.children.map((child, idx) => (
        <div
          key={idx}
          style={isHorizontal ? { width: `${schema.sizes[idx]}%` } : { height: `${schema.sizes[idx]}%` }}
          className="overflow-hidden"
        >
          <TemplatePreview schema={child} />
        </div>
      ))}
    </div>
  )
}

function leafToneFor(type: PanelType): string {
  switch (type) {
    case "explorer":
      return "bg-green-300 dark:bg-green-700"
    case "full-editor":
      return "bg-blue-300 dark:bg-blue-700"
    case "focus-editor":
      return "bg-cyan-300 dark:bg-cyan-700"
    case "output":
      return "bg-purple-300 dark:bg-purple-700"
    default:
      return "bg-gray-300 dark:bg-gray-700"
  }
}
