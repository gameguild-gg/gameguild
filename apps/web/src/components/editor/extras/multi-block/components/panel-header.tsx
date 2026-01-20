"use client"

import { Button } from "@/components/ui/button"
import { 
  ChevronsLeft, ChevronsRight, Maximize2, MoreVertical, 
  Plus, Trash2, Layers, LayoutGrid
} from "lucide-react"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { DraggableTab } from "../draggable-tab"

export interface PanelHeaderAction {
  label: string
  icon: React.ReactNode
  onClick: () => void
  variant?: "default" | "destructive"
}

interface PanelHeaderProps {
  blockId?: string
  blockCount?: number
  isDragging?: boolean
  isSinglePanelMode: boolean
  isEditable?: boolean
  showCollapseButton?: boolean
  isFirstPanel?: boolean
  onToggleMaximize?: () => void
  onToggleCollapse?: () => void
  onTogglePanelDirection?: () => void
  actions?: PanelHeaderAction[]
  panelDirection?: "horizontal" | "vertical"
}

export function PanelHeader({
  blockId,
  blockCount,
  isDragging,
  isSinglePanelMode,
  isEditable = true,
  showCollapseButton,
  isFirstPanel,
  onToggleMaximize,
  onToggleCollapse,
  onTogglePanelDirection,
  actions,
  panelDirection = "horizontal",
}: PanelHeaderProps) {
  return (
    <div className="flex items-center justify-between p-2 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
      {isSinglePanelMode && blockId ? (
        <div className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300">
          <span>Block {parseInt(blockId.slice(1))}</span>
        </div>
      ) : blockId && isDragging !== undefined ? (
        <DraggableTab blockId={blockId} isDragging={isDragging} />
      ) : blockCount !== undefined ? (
        <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
          {blockCount} {blockCount === 1 ? "Block" : "Blocks"} {panelDirection === "vertical" && "(Stacked)"}
        </span>
      ) : null}

      {isEditable && (
        <div className="flex items-center gap-1">
          {showCollapseButton && onToggleCollapse && (
            <Button
              size="sm"
              variant="ghost"
              onClick={onToggleCollapse}
              className="h-7 w-7 p-0"
              title={isFirstPanel ? "Collapse left panel" : "Collapse right panel"}
            >
              {isFirstPanel ? <ChevronsLeft className="h-3.5 w-3.5" /> : <ChevronsRight className="h-3.5 w-3.5" />}
            </Button>
          )}
          
          {onToggleMaximize && (
            <Button
              size="sm"
              variant="ghost"
              onClick={onToggleMaximize}
              className="h-7 w-7 p-0"
              title="Fullscreen"
            >
              <Maximize2 className="h-3.5 w-3.5" />
            </Button>
          )}

          {actions && actions.length > 0 && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button size="sm" variant="ghost" className="h-7 w-7 p-0">
                  <MoreVertical className="h-3.5 w-3.5" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {onTogglePanelDirection && (
                  <>
                    <DropdownMenuItem onClick={onTogglePanelDirection}>
                      {panelDirection === "horizontal" ? (
                        <>
                          <Layers className="h-3.5 w-3.5 mr-2" />
                          Stack Blocks Vertically
                        </>
                      ) : (
                        <>
                          <LayoutGrid className="h-3.5 w-3.5 mr-2" />
                          Show Blocks as Tabs
                        </>
                      )}
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                  </>
                )}
                {actions.map((action, idx) => (
                  <DropdownMenuItem
                    key={idx}
                    onClick={action.onClick}
                    className={action.variant === "destructive" ? "text-red-600" : ""}
                  >
                    {action.icon}
                    {action.label}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      )}
    </div>
  )
}
