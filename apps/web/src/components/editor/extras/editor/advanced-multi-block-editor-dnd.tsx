"use client"

import { Editor } from "@/components/editor/lexical-editor"
import type { LexicalEditor } from "lexical"
import type React from "react"
import { useEffect, useRef, useState } from "react"
import type { ProjectMode } from "@/lib/storage/editor/project-modes"
import type { ProjectPreferences } from "@/lib/storage/editor/project-preferences"
import { Button } from "@/components/ui/button"
import { 
  Plus, Trash2, X, Maximize2, GripVertical, MoreVertical,
  ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight
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

interface PanelData {
  id: string
  blockIds: string[]
  defaultSize?: number
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
  const blocks = Object.keys(blockStates).sort((a, b) => {
    const numA = parseInt(a.slice(1))
    const numB = parseInt(b.slice(1))
    return numA - numB
  })

  const [panels, setPanels] = useState<PanelData[]>(() => {
    const saved = preferences?.global?.advancedMultiBlockPanels
    if (saved && saved.length > 0) {
      return saved
    }
    if (blocks.length === 3) {
      return [
        { id: 'panel-1', blockIds: [blocks[0]!, blocks[1]!], defaultSize: 50 },
        { id: 'panel-2', blockIds: [blocks[2]!], defaultSize: 50 },
      ]
    }
    const mid = Math.ceil(blocks.length / 2)
    return [
      { id: 'panel-1', blockIds: blocks.slice(0, mid), defaultSize: 50 },
      { id: 'panel-2', blockIds: blocks.slice(mid), defaultSize: 50 },
    ]
  })

  const [activeId, setActiveId] = useState<string | null>(null)
  const [maximizedBlock, setMaximizedBlock] = useState<string | null>(null)
  const [collapsedPanels, setCollapsedPanels] = useState<Set<string>>(new Set())
  const localRefs = useRef<Record<string, React.RefObject<LexicalEditor | null>>>({})
  const panelRefs = useRef<Record<string, ImperativePanelHandle | null>>({})

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

  useEffect(() => {
    const allPanelBlocks = panels.flatMap(p => p.blockIds)
    const missingBlocks = blocks.filter(b => !allPanelBlocks.includes(b))
    const removedBlocks = allPanelBlocks.filter(b => !blocks.includes(b))

    if (missingBlocks.length > 0 || removedBlocks.length > 0) {
      setPanels(prev => {
        // Remove deleted blocks from panels, but keep empty panels
        let updated = prev.map(p => ({
          ...p,
          blockIds: p.blockIds.filter(b => blocks.includes(b))
        }))

        // Only auto-add missing blocks to panel 1 if there's no pending panel target
        if (missingBlocks.length > 0 && updated.length > 0 && !pendingBlockPanel) {
          updated[0] = {
            ...updated[0]!,
            blockIds: [...updated[0]!.blockIds, ...missingBlocks]
          }
        }

        // Only create default panel if all panels were removed and there are blocks
        if (updated.length === 0 && blocks.length > 0) {
          updated = [{ id: 'panel-1', blockIds: blocks, defaultSize: 100 }]
        }

        return updated
      })
    }
  }, [blocks, pendingBlockPanel])

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
    if (onBlockRemove) {
      onBlockRemove(blockId)
      toast.success("Block removed")
    }
  }

  const handleCreatePanel = () => {
    const newPanelId = `panel-${Date.now()}`
    const newPanels = [
      ...panels,
      { id: newPanelId, blockIds: [], defaultSize: 30 }
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

    setPanels(prev => {
      const panelToRemove = prev.find(p => p.id === panelId)
      if (!panelToRemove) return prev

      const remaining = prev.filter(p => p.id !== panelId)
      
      if (remaining.length > 0 && panelToRemove.blockIds.length > 0) {
        remaining[0] = {
          ...remaining[0]!,
          blockIds: [...remaining[0]!.blockIds, ...panelToRemove.blockIds]
        }
      }

      saveLayout(remaining)
      toast.success("Panel removed")
      return remaining
    })
  }

  const handleToggleMaximizeBlock = (blockId: string) => {
    setMaximizedBlock(prev => prev === blockId ? null : blockId)
  }

  const handleToggleCollapsePanel = (panelId: string) => {
    const panelRef = panelRefs.current[panelId]
    if (!panelRef) return

    const isCollapsed = collapsedPanels.has(panelId)
    
    if (isCollapsed) {
      panelRef.expand()
    } else {
      panelRef.collapse()
    }
  }

  const handleCollapsedTabClick = (panelId: string, blockId: string) => {
    const panelRef = panelRefs.current[panelId]
    if (!panelRef) return

    panelRef.expand()
    
    // Set active tab after a short delay to ensure panel is expanded
    setTimeout(() => {
      const panel = panels.find(p => p.id === panelId)
      if (panel && panel.blockIds.includes(blockId)) {
        // This will be handled by the panel content component
      }
    }, 100)
  }

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

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <div className="flex flex-col h-full border border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-900">
        <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
              {blocks.length} Blocks • {panels.length} Panels
            </span>
            <span className="text-xs text-gray-500 dark:text-gray-400">
              Drag tabs to reorder
            </span>
          </div>
          <div className="flex items-center gap-2">
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
                    onCollapse={() => {
                      setCollapsedPanels(prev => new Set(prev).add(panel.id))
                    }}
                    onExpand={() => {
                      setCollapsedPanels(prev => {
                        const next = new Set(prev)
                        next.delete(panel.id)
                        return next
                      })
                    }}
                  >
                    {isCollapsed ? (
                      <div className="w-12 bg-gray-100 dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 flex flex-col h-full">
                        <div className="flex-1 overflow-y-auto py-2">
                          {panel.blockIds.map(blockId => (
                            <button
                              key={blockId}
                              onClick={() => handleCollapsedTabClick(panel.id, blockId)}
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
}: DraggablePanelContentProps) {
  const [activeTab, setActiveTab] = useState(panel.blockIds[0] || "")
  
  const { setNodeRef, isOver } = useSortable({
    id: panel.id,
    data: { type: 'panel' }
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
              </DropdownMenuContent>
            </DropdownMenu>
            
            {panels.length > 1 && (
              <Button
                size="sm"
                variant="ghost"
                onClick={() => onRemovePanel(panel.id)}
                className="h-7 w-7 p-0 hover:bg-red-50 dark:hover:bg-red-950 hover:text-red-600"
              >
                <X className="h-3.5 w-3.5" />
              </Button>
            )}
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
            <DraggableTab blockId={panel.blockIds[0]!} isDragging={activeId === panel.blockIds[0]} />
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
                </DropdownMenuContent>
              </DropdownMenu>
              
              {panels.length > 1 && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => onRemovePanel(panel.id)}
                  className="h-7 w-7 p-0 hover:bg-red-50 dark:hover:bg-red-950 hover:text-red-600"
                >
                  <X className="h-3.5 w-3.5" />
                </Button>
              )}
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
            />
          </div>
        </>
      ) : (
        <>
          <div className="flex items-center justify-between border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 px-2">
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
                  <DropdownMenuItem onClick={() => onAddBlock(panel.id)}>
                    <Plus className="h-4 w-4 mr-2" />
                    Add Block
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={() => onRemoveBlock(activeTab)} className="text-red-600">
                    <Trash2 className="h-4 w-4 mr-2" />
                    Remove Block
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
              
              {panels.length > 1 && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => onRemovePanel(panel.id)}
                  className="h-7 w-7 p-0 hover:bg-red-50 dark:hover:bg-red-950 hover:text-red-600"
                >
                  <X className="h-3.5 w-3.5" />
                </Button>
              )}
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
              />
            </div>
          ))}
        </>
      )}
    </div>
  )
}

function DraggableTab({ blockId, isDragging }: { blockId: string; isDragging: boolean }) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
  } = useSortable({ id: blockId })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  }

  return (
    <div 
      ref={setNodeRef}
      style={style}
      className={`flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300 cursor-grab active:cursor-grabbing ${
        isDragging ? 'opacity-50' : ''
      }`}
      {...attributes}
      {...listeners}
    >
      <GripVertical className="h-4 w-4 text-gray-400" />
      <span>Block {parseInt(blockId.slice(1))}</span>
    </div>
  )
}

function DraggableTabButton({ 
  blockId, 
  isActive, 
  isDragging, 
  onClick 
}: { 
  blockId: string
  isActive: boolean
  isDragging: boolean
  onClick: () => void
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
  } = useSortable({ id: blockId })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  }

  return (
    <button
      ref={setNodeRef}
      style={style}
      onClick={onClick}
      className={`flex items-center gap-2 px-3 py-1.5 text-sm font-medium rounded transition-colors whitespace-nowrap cursor-grab active:cursor-grabbing ${
        isActive
          ? "bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 shadow-sm"
          : "text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800"
      } ${isDragging ? 'opacity-50' : ''}`}
      {...attributes}
      {...listeners}
    >
      <GripVertical className="h-3.5 w-3.5 text-gray-400" />
      Block {parseInt(blockId.slice(1))}
    </button>
  )
}
