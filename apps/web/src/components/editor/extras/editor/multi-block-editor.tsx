"use client"

import { Editor } from "@/components/editor/lexical-editor"
import type { LexicalEditor } from "lexical"
import type React from "react"
import { useEffect, useRef, useState } from "react"
import type { ProjectMode } from "@/lib/storage/editor/project-modes"
import type { ProjectPreferences } from "@/lib/storage/editor/project-preferences"
import { Button } from "@/components/ui/button"
import { 
  Plus, Trash2, X, Maximize2, Minimize2, GripVertical, MoreVertical,
  ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight, Layers, LayoutGrid
} from "lucide-react"
import { toast } from "sonner"
import { type ProjectType } from "@/lib/storage/editor/project-types"
import { Panel, PanelGroup, PanelResizeHandle, ImperativePanelHandle } from "react-resizable-panels"
import {
  DndContext,
  DragOverlay,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core'
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  horizontalListSortingStrategy,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { DeleteConfirmDialog } from "../dialogs/delete-confirm-dialog"
import { RestrictionsConfigDialog } from "./restrictions-config-dialog"
import { 
  DraggableTab, 
  DraggableTabButton, 
  type PanelData,
  EmptyPanel,
  PanelHeader,
  type PanelHeaderAction,
  usePanelCollapse,
  usePanelSync,
  generatePanelId,
  sortBlocks,
  DEFAULT_PANEL_SIZE,
  NEW_PANEL_SIZE,
} from "../multi-block"

interface AdvancedMultiBlockEditorProps {
  blockRefs: React.MutableRefObject<Record<string, LexicalEditor | null>>
  blockStates: Record<string, string>
  onBlockChange: (blockId: string, state: string) => void
  onBlockAdd?: () => void
  onBlockRemove?: (blockId: string) => void
  onLoadingChange?: (setLoading: (loading: boolean) => void) => void
  projectId: string
  mode?: ProjectMode
  currentProjectType?: ProjectType
  storageAdapter?: any
  preferences?: ProjectPreferences
  onPreferencesChange?: (preferences: ProjectPreferences) => void
  currentProjectId?: string
}

export function AdvancedMultiBlockEditor({
  blockRefs,
  blockStates,
  onBlockChange,
  onBlockAdd,
  onBlockRemove,
  onLoadingChange,
  projectId,
  mode = "free-page",
  currentProjectType,
  storageAdapter,
  preferences,
  onPreferencesChange,
  currentProjectId,
}: AdvancedMultiBlockEditorProps) {
  const blocks = sortBlocks(Object.keys(blockStates))

  const [panels, setPanels] = useState<PanelData[]>(() => {
    const saved = preferences?.global?.advancedMultiBlockPanels
    if (saved && saved.length > 0) {
      return saved
    }
    // Default: Always start with 1 panel containing all blocks
    // Single Panel Mode will be applied automatically when panels.length === 1
    return [
      { id: 'panel-1', blockIds: blocks, defaultSize: DEFAULT_PANEL_SIZE }
    ]
  })

  // Update panels when preferences change (e.g., when project is loaded)
  useEffect(() => {
    const saved = preferences?.global?.advancedMultiBlockPanels
    if (saved && saved.length > 0) {
      // Only update if different to avoid unnecessary re-renders
      const currentPanelsJson = JSON.stringify(panels)
      const savedPanelsJson = JSON.stringify(saved)
      if (currentPanelsJson !== savedPanelsJson) {
        setPanels(saved)
      }
    }
  }, [preferences?.global?.advancedMultiBlockPanels, currentProjectId])

  const [activeId, setActiveId] = useState<string | null>(null)
  const [maximizedBlock, setMaximizedBlock] = useState<string | null>(null)
  const [deleteBlockConfirm, setDeleteBlockConfirm] = useState<{ open: boolean; blockId: string | null }>({ open: false, blockId: null })
  const [deletePanelConfirm, setDeletePanelConfirm] = useState<{ open: boolean; panelId: string | null }>({ open: false, panelId: null })
  const localRefs = useRef<Record<string, React.RefObject<LexicalEditor | null>>>({})

  // Use shared hooks
  const {
    collapsedPanels,
    panelRefs,
    handleToggleCollapse: handleToggleCollapsePanel,
    handleCollapsedTabClick,
    onPanelCollapse,
    onPanelExpand,
  } = usePanelCollapse()

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8,
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  )

  useEffect(() => {
    blocks.forEach(blockId => {
      if (!localRefs.current[blockId]) {
        localRefs.current[blockId] = { current: null }
      }
    })
  }, [blocks])

  useEffect(() => {
    blocks.forEach(blockId => {
      const localRef = localRefs.current[blockId]
      if (localRef && blockRefs.current) {
        blockRefs.current[blockId] = localRef.current
      }
    })
  })

  const [pendingBlockPanel, setPendingBlockPanel] = useState<string | null>(null)

  // Use shared sync hook
  usePanelSync({
    blocks,
    panels,
    setPanels,
    pendingBlockPanel,
    projectId: currentProjectId,
    preferences,
  })

  // When blocks change, assign pending block to target panel
  useEffect(() => {
    if (!pendingBlockPanel) return
    
    const allPanelBlocks = panels.flatMap(p => p.blockIds)
    const newBlock = blocks.find(b => !allPanelBlocks.includes(b))
    
    if (newBlock) {
      setPanels(prev => {
        const newPanels = prev.map(p => {
          if (p.id === pendingBlockPanel) {
            return { ...p, blockIds: [...p.blockIds, newBlock] }
          }
          // Remove from panel 1 if it was auto-added there
          return { ...p, blockIds: p.blockIds.filter(id => id !== newBlock) }
        })
        saveLayout(newPanels)
        return newPanels
      })
      setPendingBlockPanel(null)
    }
  }, [blocks, pendingBlockPanel])

  const saveLayout = (newPanels: PanelData[]) => {
    if (!onPreferencesChange || !preferences) return
    
    const updatedPreferences: ProjectPreferences = {
      ...preferences,
      global: {
        ...preferences.global,
        advancedMultiBlockPanels: newPanels,
      },
    }
    onPreferencesChange(updatedPreferences)
  }

  const handleAddBlock = (panelId?: string) => {
    if (panelId) {
      setPendingBlockPanel(panelId)
    }
    if (onBlockAdd) {
      onBlockAdd()
      toast.success("Block added")
    }
  }

  const handleDragStart = (event: DragStartEvent) => {
    setActiveId(event.active.id as string)
  }

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event
    setActiveId(null)

    if (!over) return

    const activeBlockId = active.id as string
    const overId = over.id as string

    // Find source panel
    const sourcePanel = panels.find(p => p.blockIds.includes(activeBlockId))
    if (!sourcePanel) return

    // Check if dropping on another panel (by panel ID)
    let targetPanel = panels.find(p => p.id === overId)
    
    // If not dropping directly on panel, check if dropping on a block and find its panel
    if (!targetPanel) {
      targetPanel = panels.find(p => p.blockIds.includes(overId))
    }
    
    if (targetPanel && targetPanel.id !== sourcePanel.id) {
      // Move block to another panel - now allowed even if source panel has only 1 block
      const newPanels = panels.map(p => {
        if (p.id === sourcePanel.id) {
          return { ...p, blockIds: p.blockIds.filter(id => id !== activeBlockId) }
        }
        if (p.id === targetPanel.id) {
          return { ...p, blockIds: [...p.blockIds, activeBlockId] }
        }
        return p
      })

      setPanels(newPanels)
      saveLayout(newPanels)
      toast.success("Block moved")
    } else if (sourcePanel) {
      // Reorder within same panel
      const oldIndex = sourcePanel.blockIds.indexOf(activeBlockId)
      const overBlockId = overId
      const newIndex = sourcePanel.blockIds.indexOf(overBlockId)

      if (oldIndex !== -1 && newIndex !== -1 && oldIndex !== newIndex) {
        const newBlockIds = arrayMove(sourcePanel.blockIds, oldIndex, newIndex)
        const newPanels = panels.map(p =>
          p.id === sourcePanel.id ? { ...p, blockIds: newBlockIds } : p
        )
        setPanels(newPanels)
        saveLayout(newPanels)
      }
    }
  }

  const handleRemoveBlock = (blockId: string) => {
    setDeleteBlockConfirm({ open: true, blockId })
  }

  const confirmRemoveBlock = () => {
    if (deleteBlockConfirm.blockId && onBlockRemove) {
      onBlockRemove(deleteBlockConfirm.blockId)
      toast.success("Block removed")
      setDeleteBlockConfirm({ open: false, blockId: null })
    }
  }

  const handleCreatePanel = () => {
    const newPanelId = generatePanelId(panels)
    
    const newPanels = [
      ...panels,
      { id: newPanelId, blockIds: [], defaultSize: NEW_PANEL_SIZE }
    ]
    setPanels(newPanels)
    saveLayout(newPanels)
    toast.success("Panel created")
  }

  const handleRemovePanel = (panelId: string) => {
    if (panels.length <= 1) {
      toast.error("Cannot remove", { description: "Must have at least one panel" })
      return
    }
    setDeletePanelConfirm({ open: true, panelId })
  }

  const confirmRemovePanel = () => {
    if (!deletePanelConfirm.panelId) return

    setPanels(prev => {
      const panelToRemove = prev.find(p => p.id === deletePanelConfirm.panelId)
      if (!panelToRemove) return prev

      const remaining = prev.filter(p => p.id !== deletePanelConfirm.panelId)
      
      if (remaining.length > 0 && panelToRemove.blockIds.length > 0) {
        remaining[0] = {
          ...remaining[0]!,
          blockIds: [...remaining[0]!.blockIds, ...panelToRemove.blockIds]
        }
      }

      saveLayout(remaining)
      toast.success("Panel removed")
      setDeletePanelConfirm({ open: false, panelId: null })
      return remaining
    })
  }

  const handleToggleMaximizeBlock = (blockId: string) => {
    setMaximizedBlock(prev => prev === blockId ? null : blockId)
  }

  const handleTogglePanelDirection = (panelId: string) => {
    setPanels(prev => {
      const newPanels = prev.map(p => {
        if (p.id === panelId) {
          const currentDirection = p.direction || "horizontal"
          const newDirection: "horizontal" | "vertical" = currentDirection === "horizontal" ? "vertical" : "horizontal"
          return { ...p, direction: newDirection }
        }
        return p
      })
      saveLayout(newPanels)
      toast.success("Layout updated", {
        description: `Blocks layout set to ${newPanels.find(p => p.id === panelId)?.direction}`,
        duration: 2000,
      })
      return newPanels
    })
  }

  const handleWidthToggle = () => {
    if (!onPreferencesChange || !preferences) return
    
    const currentWidth = preferences.global?.type2SingleBlockWidth || "wide"
    const newWidth = currentWidth === "wide" ? "narrow" : "wide"
    const updatedPreferences: ProjectPreferences = {
      ...preferences,
      global: {
        ...preferences.global,
        type2SingleBlockWidth: newWidth,
      },
    }
    onPreferencesChange(updatedPreferences)
    
    toast.success("Width updated", {
      description: `Layout set to ${newWidth}`,
      duration: 2000,
    })
  }

  const handleRestrictionsChange = (newRestrictions: any) => {
    if (!onPreferencesChange || !preferences) return
    
    const updatedPreferences: ProjectPreferences = {
      ...preferences,
      global: {
        ...preferences.global,
        restrictions: newRestrictions,
      },
    }
    onPreferencesChange(updatedPreferences)
  }

  // Check if we're in single panel mode (1 panel, any number of blocks)
  const isSinglePanelMode = panels.length === 1

  if (maximizedBlock) {
    const blockRef = localRefs.current[maximizedBlock]
    if (!blockRef) return null

    return (
      <div className="fixed inset-0 z-50 bg-white dark:bg-gray-900 flex flex-col">
        <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700">
          <span className="text-sm font-medium">Block {parseInt(maximizedBlock.slice(1))} (Fullscreen)</span>
          <Button size="sm" variant="ghost" onClick={() => setMaximizedBlock(null)}>
            <Maximize2 className="h-4 w-4" />
          </Button>
        </div>
        <div className="flex-1 overflow-auto p-4 sm:p-6 md:p-8 lg:p-12">
          <Editor
            editorRef={blockRef}
            initialState={blockStates[maximizedBlock]}
            onChange={(state) => onBlockChange(maximizedBlock, state)}
            onLoadingChange={onLoadingChange}
            projectId={projectId}
            mode={mode}
            blockId={maximizedBlock}
            currentProjectType={currentProjectType}
            storageAdapter={storageAdapter}
          />
        </div>
      </div>
    )
  }

  // Single Panel Mode - simplified interface for 1 panel (any number of blocks)
  if (isSinglePanelMode) {
    const panel = panels[0]!
    const singleBlockWidth = preferences?.global?.type2SingleBlockWidth || "wide"

    return (
      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <div className="flex flex-col h-full bg-white dark:bg-gray-900">
        <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
              {panel.blockIds.length} {panel.blockIds.length === 1 ? "Block" : "Blocks"}
            </span>
            <Button
              size="sm"
              variant="outline"
              onClick={handleWidthToggle}
              className="gap-2 h-8"
              title={singleBlockWidth === "wide" ? "Switch to narrow layout" : "Switch to wide layout"}
            >
              {singleBlockWidth === "wide" ? (
                <>
                  <Minimize2 className="h-4 w-4" />
                  Narrow
                </>
              ) : (
                <>
                  <Maximize2 className="h-4 w-4" />
                  Wide
                </>
              )}
            </Button>
          </div>
          <div className="flex items-center gap-2">
            <RestrictionsConfigDialog
              currentRestrictions={preferences?.global?.restrictions}
              onRestrictionsChange={handleRestrictionsChange}
              availableBlocks={blocks}
              availablePanels={panels.map(p => p.id)}
            />
            <Button
              size="sm"
              variant="outline"
              onClick={handleCreatePanel}
              className="gap-2 h-8"
            >
              <Plus className="h-4 w-4" />
              New Panel
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => handleAddBlock(panel.id)}
              className="gap-2 h-8"
            >
              <Plus className="h-4 w-4" />
              Add Block
            </Button>
          </div>
        </div>

        <div className={`flex-1 overflow-hidden ${singleBlockWidth === "narrow" ? "flex justify-center" : ""}`}>
          <div className={`flex flex-col h-full ${singleBlockWidth === "narrow" ? "w-full max-w-4xl" : "w-full"}`}>
            <DraggablePanelContent
              panel={panel}
              panels={panels}
              blocks={blocks}
              blockStates={blockStates}
              blockRefs={localRefs.current}
              onBlockChange={onBlockChange}
              onLoadingChange={onLoadingChange}
              projectId={projectId}
              mode={mode}
              currentProjectType={currentProjectType}
              storageAdapter={storageAdapter}
              onRemoveBlock={handleRemoveBlock}
              onRemovePanel={handleRemovePanel}
              onToggleMaximizeBlock={handleToggleMaximizeBlock}
              onAddBlock={handleAddBlock}
              onToggleCollapse={undefined}
              showCollapseButton={false}
              isFirstPanel={false}
              activeId={activeId}
              onTogglePanelDirection={handleTogglePanelDirection}
              customRestrictions={preferences?.global?.restrictions}
            />
          </div>
        </div>

        <DeleteConfirmDialog
          open={deleteBlockConfirm.open}
          onOpenChange={(open) => setDeleteBlockConfirm({ open, blockId: null })}
          title="Remove Block"
          itemName={deleteBlockConfirm.blockId ? `Block ${parseInt(deleteBlockConfirm.blockId.slice(1))}` : undefined}
          itemType="block"
          onConfirm={confirmRemoveBlock}
        />

        <DeleteConfirmDialog
          open={deletePanelConfirm.open}
          onOpenChange={(open) => setDeletePanelConfirm({ open, panelId: null })}
          title="Remove Panel"
          itemName={deletePanelConfirm.panelId ? `Panel` : undefined}
          itemType="panel"
          onConfirm={confirmRemovePanel}
          description={deletePanelConfirm.panelId ? "Are you sure you want to remove this panel? All blocks in this panel will be moved to the first panel." : undefined}
        />

        <DragOverlay>
          {activeId ? (
            <div className="bg-blue-500 text-white px-4 py-2 rounded shadow-lg font-medium">
              Block {parseInt(activeId.slice(1))}
            </div>
          ) : null}
        </DragOverlay>
      </div>
      </DndContext>
    )
  }

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <div className="flex flex-col h-full bg-white dark:bg-gray-900">
        <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
              {blocks.length} Blocks • {panels.length} Panels
            </span>
          </div>
          <div className="flex items-center gap-2">
            <RestrictionsConfigDialog
              currentRestrictions={preferences?.global?.restrictions}
              onRestrictionsChange={handleRestrictionsChange}
              availableBlocks={blocks}
              availablePanels={panels.map(p => p.id)}
            />
            <Button
              size="sm"
              variant="outline"
              onClick={handleCreatePanel}
              className="gap-2 h-8"
            >
              <Plus className="h-4 w-4" />
              New Panel
            </Button>
          </div>
        </div>

        <div className="flex-1 overflow-hidden">
          <PanelGroup direction="horizontal">
            {panels.map((panel, panelIndex) => {
              const isCollapsed = collapsedPanels.has(panel.id)
              const isFirstPanel = panelIndex === 0
              const isLastPanel = panelIndex === panels.length - 1

              return (
                <>
                  <Panel
                    key={panel.id}
                    ref={(ref) => { panelRefs.current[panel.id] = ref }}
                    defaultSize={panel.defaultSize || 50}
                    minSize={10}
                    collapsible={true}
                    collapsedSize={3}
                    onCollapse={() => onPanelCollapse(panel.id)}
                    onExpand={() => onPanelExpand(panel.id)}
                  >
                    {isCollapsed ? (
                      <div className="w-12 bg-gray-100 dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 flex flex-col h-full">
                        <div className="flex-1 overflow-y-auto py-2">
                          {panel.blockIds.map(blockId => (
                            <button
                              key={blockId}
                              onClick={() => handleCollapsedTabClick(panel.id)}
                              className="w-full px-2 py-3 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors writing-mode-vertical transform rotate-180"
                              style={{ writingMode: 'vertical-rl' }}
                            >
                              Block {parseInt(blockId.slice(1))}
                            </button>
                          ))}
                        </div>
                        <button
                          onClick={() => handleToggleCollapsePanel(panel.id)}
                          className="w-full p-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors border-t border-gray-200 dark:border-gray-700"
                        >
                          {isFirstPanel ? <ChevronRight className="h-4 w-4 mx-auto" /> : <ChevronLeft className="h-4 w-4 mx-auto" />}
                        </button>
                      </div>
                    ) : (
                      <DraggablePanelContent
                        panel={panel}
                        panels={panels}
                        blocks={blocks}
                        blockStates={blockStates}
                        blockRefs={localRefs.current}
                        onBlockChange={onBlockChange}
                        onLoadingChange={onLoadingChange}
                        projectId={projectId}
                        mode={mode}
                        currentProjectType={currentProjectType}
                        storageAdapter={storageAdapter}
                        onRemoveBlock={handleRemoveBlock}
                        onRemovePanel={handleRemovePanel}
                        onToggleMaximizeBlock={handleToggleMaximizeBlock}
                        onAddBlock={handleAddBlock}
                        onToggleCollapse={() => handleToggleCollapsePanel(panel.id)}
                        showCollapseButton={isFirstPanel || isLastPanel}
                        isFirstPanel={isFirstPanel}
                        activeId={activeId}
                        onTogglePanelDirection={handleTogglePanelDirection}
                        customRestrictions={preferences?.global?.restrictions}
                      />
                    )}
                  </Panel>
                
                  {panelIndex < panels.length - 1 && (
                    <PanelResizeHandle className="group w-2 bg-gray-200 dark:bg-gray-700 hover:bg-blue-500 dark:hover:bg-blue-500 transition-colors relative flex items-center justify-center">
                      <div className="w-1 h-12 bg-gray-400 dark:bg-gray-600 rounded-full group-hover:bg-blue-600 transition-colors" />
                    </PanelResizeHandle>
                  )}
                </>
              )
            })}
          </PanelGroup>
        </div>
      </div>

      <DragOverlay>
        {activeId ? (
          <div className="bg-blue-500 text-white px-4 py-2 rounded shadow-lg font-medium">
            Block {parseInt(activeId.slice(1))}
          </div>
        ) : null}
      </DragOverlay>

      <DeleteConfirmDialog
        open={deleteBlockConfirm.open}
        onOpenChange={(open) => setDeleteBlockConfirm({ open, blockId: null })}
        title="Remove Block"
        itemName={deleteBlockConfirm.blockId ? `Block ${parseInt(deleteBlockConfirm.blockId.slice(1))}` : undefined}
        itemType="block"
        onConfirm={confirmRemoveBlock}
      />

      <DeleteConfirmDialog
        open={deletePanelConfirm.open}
        onOpenChange={(open) => setDeletePanelConfirm({ open, panelId: null })}
        title="Remove Panel"
        itemName={deletePanelConfirm.panelId ? `Panel` : undefined}
        itemType="panel"
        onConfirm={confirmRemovePanel}
        description={deletePanelConfirm.panelId ? "Are you sure you want to remove this panel? All blocks in this panel will be moved to the first panel." : undefined}
      />
    </DndContext>
  )
}

interface DraggablePanelContentProps {
  panel: PanelData
  panels: PanelData[]
  blocks: string[]
  blockStates: Record<string, string>
  blockRefs: Record<string, React.RefObject<LexicalEditor | null>>
  onBlockChange: (blockId: string, state: string) => void
  onLoadingChange?: (setLoading: (loading: boolean) => void) => void
  projectId: string
  mode?: ProjectMode
  currentProjectType?: ProjectType
  storageAdapter?: any
  onRemoveBlock: (blockId: string) => void
  onRemovePanel: (panelId: string) => void
  onToggleMaximizeBlock: (blockId: string) => void
  onAddBlock: (panelId?: string) => void
  onToggleCollapse?: () => void
  showCollapseButton?: boolean
  isFirstPanel?: boolean
  activeId: string | null
  onTogglePanelDirection: (panelId: string) => void
  customRestrictions?: any
}

function DraggablePanelContent({
  panel,
  panels,
  blocks,
  blockStates,
  blockRefs,
  onBlockChange,
  onLoadingChange,
  projectId,
  mode,
  currentProjectType,
  storageAdapter,
  onRemoveBlock,
  onRemovePanel,
  onToggleMaximizeBlock,
  onAddBlock,
  onToggleCollapse,
  showCollapseButton,
  isFirstPanel,
  activeId,
  onTogglePanelDirection,
  customRestrictions,
}: DraggablePanelContentProps) {
  const [activeTab, setActiveTab] = useState(panel.blockIds[0] || "")
  const panelDirection = panel.direction || "horizontal"
  
  const isSinglePanelMode = panels.length === 1
  
  const { setNodeRef, isOver } = useSortable({
    id: panel.id,
    data: { type: 'panel' },
  })

  useEffect(() => {
    if (!panel.blockIds.includes(activeTab) && panel.blockIds.length > 0) {
      setActiveTab(panel.blockIds[0]!)
    }
  }, [panel.blockIds, activeTab])

  if (panel.blockIds.length === 0) {
    return (
      <div 
        ref={setNodeRef}
        className={`flex flex-col h-full bg-white dark:bg-gray-900 transition-colors ${
          isOver ? 'ring-2 ring-blue-500' : ''
        }`}
      >
        <div className="flex items-center justify-between p-2 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
          <span className="text-sm font-medium text-gray-500 dark:text-gray-400">Empty Panel</span>
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
            
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button size="sm" variant="ghost" className="h-7 w-7 p-0">
                  <MoreVertical className="h-3.5 w-3.5" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => onAddBlock(panel.id)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Block
                </DropdownMenuItem>
                {panels.length > 1 && (
                  <>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem onClick={() => onRemovePanel(panel.id)} className="text-red-600">
                      <Trash2 className="h-4 w-4 mr-2" />
                      Remove Panel
                    </DropdownMenuItem>
                  </>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
        <div className={`flex flex-col items-center justify-center h-full p-8 text-center bg-gray-50 dark:bg-gray-900 border-2 border-dashed border-gray-300 dark:border-gray-700 transition-colors m-4 rounded-lg ${
          isOver ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20' : ''
        }`}>
          <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">Drag blocks here</p>
          <p className="text-xs text-gray-400 dark:text-gray-500">or use the menu to add a new block</p>
        </div>
      </div>
    )
  }

  return (
    <div 
      ref={setNodeRef}
      className={`flex flex-col h-full bg-white dark:bg-gray-900 transition-colors ${
        isOver ? 'ring-2 ring-blue-500' : ''
      }`}
    >
      {panel.blockIds.length === 1 ? (
        <>
          <div className="flex items-center justify-between p-2 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
            {isSinglePanelMode ? (
              <div className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300">
                <span>Block {parseInt(panel.blockIds[0]!.slice(1))}</span>
              </div>
            ) : (
              <DraggableTab blockId={panel.blockIds[0]!} isDragging={activeId === panel.blockIds[0]} />
            )}
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
              
              <Button
                size="sm"
                variant="ghost"
                onClick={() => onToggleMaximizeBlock(panel.blockIds[0]!)}
                className="h-7 w-7 p-0"
              >
                <Maximize2 className="h-3.5 w-3.5" />
              </Button>
              
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button size="sm" variant="ghost" className="h-7 w-7 p-0">
                    <MoreVertical className="h-3.5 w-3.5" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={() => onAddBlock(panel.id)}>
                    <Plus className="h-4 w-4 mr-2" />
                    Add Block
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={() => onRemoveBlock(panel.blockIds[0]!)} className="text-red-600">
                    <Trash2 className="h-4 w-4 mr-2" />
                    Remove Block
                  </DropdownMenuItem>
                  {panels.length > 1 && (
                    <DropdownMenuItem onClick={() => onRemovePanel(panel.id)} className="text-red-600">
                      <Trash2 className="h-4 w-4 mr-2" />
                      Remove Panel
                    </DropdownMenuItem>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
          <div className="flex-1 overflow-auto p-4 sm:p-6 md:p-8 lg:p-12">
            <Editor
              editorRef={blockRefs[panel.blockIds[0]!]}
              initialState={blockStates[panel.blockIds[0]!]}
              onChange={(state) => onBlockChange(panel.blockIds[0]!, state)}
              onLoadingChange={onLoadingChange}
              projectId={projectId}
              mode={mode}
              blockId={panel.blockIds[0]!}
              currentProjectType={currentProjectType}
              storageAdapter={storageAdapter}
              panelId={panel.id}
              customRestrictions={customRestrictions}
            />
          </div>
        </>
      ) : panelDirection === "vertical" ? (
        <>
          {/* Vertical layout - blocks stacked - painel tem barra de rolagem única */}
          <div className="shrink-0 flex items-center justify-between p-2 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
            <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
              {panel.blockIds.length} Blocks (Stacked)
            </span>
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
              
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button size="sm" variant="ghost" className="h-7 w-7 p-0">
                    <MoreVertical className="h-3.5 w-3.5" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={() => onTogglePanelDirection(panel.id)}>
                    Use Tabs
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={() => onAddBlock(panel.id)}>
                    <Plus className="h-4 w-4 mr-2" />
                    Add Block
                  </DropdownMenuItem>
                  {panels.length > 1 && (
                    <>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem onClick={() => onRemovePanel(panel.id)} className="text-red-600">
                        <Trash2 className="h-4 w-4 mr-2" />
                        Remove Panel
                      </DropdownMenuItem>
                    </>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
          
          <div className="flex flex-col overflow-auto">
            {panel.blockIds.map((blockId, index) => (
              <div
                key={blockId}
                className={`flex-1 ${index < panel.blockIds.length - 1 ? "border-b border-gray-200 dark:border-gray-700" : ""}`}
              >
                <div className="p-2 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
                  <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Block {parseInt(blockId.slice(1))}
                  </span>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => onRemoveBlock(blockId)}
                    className="h-6 w-6 p-0 hover:bg-red-50 dark:hover:bg-red-950 hover:text-red-600 dark:hover:text-red-400"
                    title="Remove block"
                  >
                    <Trash2 className="h-3 w-3" />
                  </Button>
                </div>
                <div className="p-4 sm:p-6 md:p-8 lg:p-12">
                  <Editor
                    editorRef={blockRefs[blockId]}
                    initialState={blockStates[blockId]}
                    onChange={(state) => onBlockChange(blockId, state)}
                    onLoadingChange={onLoadingChange}
                    projectId={projectId}
                    mode={mode}
                    blockId={blockId}
                    currentProjectType={currentProjectType}
                    storageAdapter={storageAdapter}
                    panelId={panel.id}
                    customRestrictions={customRestrictions}
                  />
                </div>
              </div>
            ))}
          </div>
        </>
      ) : (
        <>
          {/* Horizontal layout (tabs) - tabs fixas, cada block com barra própria */}
          <div className="shrink-0 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 px-2">
            <SortableContext items={panel.blockIds} strategy={horizontalListSortingStrategy}>
              <div className="flex items-center gap-1 overflow-x-auto flex-1 py-2">
                {panel.blockIds.map(blockId => (
                  <DraggableTabButton
                    key={blockId}
                    blockId={blockId}
                    isActive={activeTab === blockId}
                    isDragging={activeId === blockId}
                    onClick={() => setActiveTab(blockId)}
                  />
                ))}
              </div>
            </SortableContext>
            
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
              
              <Button
                size="sm"
                variant="ghost"
                onClick={() => onToggleMaximizeBlock(activeTab)}
                className="h-7 w-7 p-0"
              >
                <Maximize2 className="h-3.5 w-3.5" />
              </Button>
              
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button size="sm" variant="ghost" className="h-7 w-7 p-0">
                    <MoreVertical className="h-3.5 w-3.5" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  {panel.blockIds.length > 1 && (
                    <>
                      <DropdownMenuItem onClick={() => onTogglePanelDirection(panel.id)}>
                        Stack Blocks
                      </DropdownMenuItem>
                      <DropdownMenuSeparator />
                    </>
                  )}
                  <DropdownMenuItem onClick={() => onAddBlock(panel.id)}>
                    <Plus className="h-4 w-4 mr-2" />
                    Add Block
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={() => onRemoveBlock(activeTab)} className="text-red-600">
                    <Trash2 className="h-4 w-4 mr-2" />
                    Remove Block
                  </DropdownMenuItem>
                  {panels.length > 1 && (
                    <DropdownMenuItem onClick={() => onRemovePanel(panel.id)} className="text-red-600">
                      <Trash2 className="h-4 w-4 mr-2" />
                      Remove Panel
                    </DropdownMenuItem>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
          
          {panel.blockIds.map(blockId => (
            <div
              key={blockId}
              className={`flex-1 overflow-auto p-4 sm:p-6 md:p-8 lg:p-12 ${
                activeTab === blockId ? 'block' : 'hidden'
              }`}
            >
              <Editor
                editorRef={blockRefs[blockId]}
                initialState={blockStates[blockId]}
                onChange={(state) => onBlockChange(blockId, state)}
                onLoadingChange={onLoadingChange}
                projectId={projectId}
                mode={mode}
                blockId={blockId}
                currentProjectType={currentProjectType}
                storageAdapter={storageAdapter}
                panelId={panel.id}
                customRestrictions={customRestrictions}
              />
            </div>
          ))}
        </>
      )}
    </div>
  )
}
