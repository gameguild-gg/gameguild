"use client"

import { useState, useEffect, useRef } from "react"
import type { SerializedEditorState } from "lexical"
import type { ProjectPreferences, PanelData } from "@/lib/storage/editor/project-preferences"
import { PreviewRenderer } from "./preview-renderer"
import { 
  Maximize2, Minimize2, GripVertical,
  ChevronLeft, ChevronRight,
  ChevronsLeft, ChevronsRight
} from "lucide-react"
import { Button } from "@/components/ui/button"
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

interface AdvancedMultiBlockPreviewProps {
  blockStates: Record<string, SerializedEditorState>
  projectId?: string
  storageAdapter?: {
    load: (id: string) => Promise<any>
  }
  preferences?: ProjectPreferences
  isEditable?: boolean
  onLayoutChange?: (panels: PanelData[], direction: "horizontal" | "vertical") => void
}

export function AdvancedMultiBlockPreview({
  blockStates,
  projectId,
  storageAdapter,
  preferences,
  isEditable = true,
  onLayoutChange,
}: AdvancedMultiBlockPreviewProps) {
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
    // Default: 2 panels with blocks split
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

  const [direction, setDirection] = useState<"horizontal" | "vertical">(
    preferences?.global?.multiBlockDirection || "horizontal"
  )
  const [maximizedBlock, setMaximizedBlock] = useState<string | null>(null)
  const [collapsedPanels, setCollapsedPanels] = useState<Set<string>>(new Set())
  const [activeId, setActiveId] = useState<string | null>(null)
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

  // Sync panels when blocks change
  useEffect(() => {
    const allPanelBlocks = panels.flatMap(p => p.blockIds)
    const missingBlocks = blocks.filter(b => !allPanelBlocks.includes(b))
    const removedBlocks = allPanelBlocks.filter(b => !blocks.includes(b))

    if (missingBlocks.length > 0 || removedBlocks.length > 0) {
      setPanels(prev => {
        let updated = prev.map(p => ({
          ...p,
          blockIds: p.blockIds.filter(b => blocks.includes(b))
        })).filter(p => p.blockIds.length > 0)

        if (missingBlocks.length > 0 && updated.length > 0) {
          updated[0] = {
            ...updated[0]!,
            blockIds: [...updated[0]!.blockIds, ...missingBlocks]
          }
        }

        if (updated.length === 0 && blocks.length > 0) {
          updated = [{ id: 'panel-1', blockIds: blocks, defaultSize: 100 }]
        }

        return updated
      })
    }
  }, [blocks])

  const handleDragStart = (event: DragStartEvent) => {
    setActiveId(event.active.id as string)
  }

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event
    setActiveId(null)

    if (!over) return

    const activeBlockId = active.id as string
    const overPanelId = over.id as string

    // Find source panel
    const sourcePanel = panels.find(p => p.blockIds.includes(activeBlockId))
    if (!sourcePanel) return

    // Check if dropping on another panel
    const targetPanel = panels.find(p => p.id === overPanelId)
    
    if (targetPanel && targetPanel.id !== sourcePanel.id) {
      // Move block to another panel
      if (sourcePanel.blockIds.length <= 1) {
        return
      }

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
      if (onLayoutChange) {
        onLayoutChange(newPanels, direction)
      }
    } else if (sourcePanel) {
      // Reorder within same panel
      const oldIndex = sourcePanel.blockIds.indexOf(activeBlockId)
      const overBlockId = over.id as string
      const newIndex = sourcePanel.blockIds.indexOf(overBlockId)

      if (oldIndex !== -1 && newIndex !== -1 && oldIndex !== newIndex) {
        const newBlockIds = arrayMove(sourcePanel.blockIds, oldIndex, newIndex)
        const newPanels = panels.map(p =>
          p.id === sourcePanel.id ? { ...p, blockIds: newBlockIds } : p
        )
        setPanels(newPanels)
        if (onLayoutChange) {
          onLayoutChange(newPanels, direction)
        }
      }
    }
  }

  const handleMoveBlockToNewPanel = (blockId: string, fromPanelId: string) => {
    setPanels(prev => {
      const fromPanel = prev.find(p => p.id === fromPanelId)
      if (!fromPanel || fromPanel.blockIds.length <= 1) return prev

      const newPanelId = `panel-${Date.now()}`
      const updated = prev.map(p => 
        p.id === fromPanelId 
          ? { ...p, blockIds: p.blockIds.filter(id => id !== blockId) }
          : p
      )
      
      const newPanels = [
        ...updated,
        { id: newPanelId, blockIds: [blockId], defaultSize: 30 }
      ]
      
      if (onLayoutChange) {
        onLayoutChange(newPanels, direction)
      }
      return newPanels
    })
  }

  const handleMoveBlockToPanel = (blockId: string, fromPanelId: string, toPanelId: string) => {
    setPanels(prev => {
      const fromPanel = prev.find(p => p.id === fromPanelId)
      if (!fromPanel || fromPanel.blockIds.length <= 1) return prev

      const updated = prev.map(p => {
        if (p.id === fromPanelId) {
          return { ...p, blockIds: p.blockIds.filter(id => id !== blockId) }
        }
        if (p.id === toPanelId) {
          return { ...p, blockIds: [...p.blockIds, blockId] }
        }
        return p
      })
      
      if (onLayoutChange) {
        onLayoutChange(updated, direction)
      }
      return updated
    })
  }

  const toggleDirection = () => {
    const newDirection = direction === "horizontal" ? "vertical" : "horizontal"
    setDirection(newDirection)
    if (onLayoutChange) {
      onLayoutChange(panels, newDirection)
    }
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
  }

  if (maximizedBlock) {
    const activeBlockState = blockStates[maximizedBlock]
    if (!activeBlockState) return null

    return (
      <div className="fixed inset-0 z-50 bg-white dark:bg-gray-900 flex flex-col">
        <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
          <span className="text-sm font-medium">Block {parseInt(maximizedBlock.slice(1))} (Fullscreen)</span>
          <Button size="sm" variant="ghost" onClick={() => setMaximizedBlock(null)}>
            <Minimize2 className="h-4 w-4" />
          </Button>
        </div>
        <div className="flex-1 overflow-auto p-6 sm:p-8 md:p-12">
          <PreviewRenderer
            serializedState={activeBlockState}
            projectId={projectId}
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
      <div className="flex flex-col h-full w-full bg-white dark:bg-gray-900">
        {/* Panels */}
        <div className="flex-1 overflow-hidden">
          <PanelGroup direction={direction}>
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
                    <PreviewPanelContent
                      panel={panel}
                      panels={panels}
                      blockStates={blockStates}
                      projectId={projectId}
                      storageAdapter={storageAdapter}
                      isEditable={isEditable}
                      onMoveBlockToPanel={handleMoveBlockToPanel}
                      onToggleMaximizeBlock={handleToggleMaximizeBlock}
                      onToggleCollapse={() => handleToggleCollapsePanel(panel.id)}
                      showCollapseButton={isFirstPanel || isLastPanel}
                      isFirstPanel={isFirstPanel}
                      activeId={activeId}
                    />
                  )}
                </Panel>
                
                {panelIndex < panels.length - 1 && (
                  <PanelResizeHandle className={`group ${direction === "horizontal" ? "w-2" : "h-2"} bg-gray-200 dark:bg-gray-700 hover:bg-blue-500 dark:hover:bg-blue-500 transition-colors relative flex items-center justify-center`}>
                    <div className={`${direction === "horizontal" ? "w-1 h-12" : "h-1 w-12"} bg-gray-400 dark:bg-gray-600 rounded-full group-hover:bg-blue-600 transition-colors`} />
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

interface PreviewPanelContentProps {
  panel: PanelData
  panels: PanelData[]
  blockStates: Record<string, SerializedEditorState>
  projectId?: string
  storageAdapter?: {
    load: (id: string) => Promise<any>
  }
  isEditable: boolean
  onMoveBlockToPanel: (blockId: string, fromPanelId: string, toPanelId: string) => void
  onToggleMaximizeBlock: (blockId: string) => void
  onToggleCollapse?: () => void
  showCollapseButton?: boolean
  isFirstPanel?: boolean
  activeId: string | null
}

function PreviewPanelContent({
  panel,
  panels,
  blockStates,
  projectId,
  storageAdapter,
  isEditable,
  onMoveBlockToPanel,
  onToggleMaximizeBlock,
  onToggleCollapse,
  showCollapseButton,
  isFirstPanel,
  activeId,
}: PreviewPanelContentProps) {
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
        className={`flex flex-col items-center justify-center h-full p-8 text-center bg-gray-50 dark:bg-gray-900 border border-dashed border-gray-300 dark:border-gray-700 transition-colors ${
          isOver ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20' : ''
        }`}
      >
        <p className="text-sm text-gray-500 dark:text-gray-400">Empty Panel</p>
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
        // Single block: no tabs
        <>
          <div className="flex items-center justify-between p-2 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
            <PreviewTab blockId={panel.blockIds[0]!} isDragging={activeId === panel.blockIds[0]} />
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
                
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => onToggleMaximizeBlock(panel.blockIds[0]!)}
                  className="h-7 w-7 p-0"
                  title="Fullscreen"
                >
                  <Maximize2 className="h-3.5 w-3.5" />
                </Button>
              </div>
            )}
          </div>
          <div className="flex-1 overflow-auto p-6 sm:p-8 md:p-12">
            {blockStates[panel.blockIds[0]!] && (
              <PreviewRenderer
                serializedState={blockStates[panel.blockIds[0]!]!}
                projectId={projectId}
                storageAdapter={storageAdapter}
              />
            )}
          </div>
        </>
      ) : (
        // Multiple blocks: use tabs
        <>
          <div className="flex items-center justify-between border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 px-2">
            <SortableContext items={panel.blockIds} strategy={horizontalListSortingStrategy}>
              <div className="flex items-center gap-1 overflow-x-auto flex-1 py-2">
                {panel.blockIds.map(blockId => (
                  <PreviewTabButton
                    key={blockId}
                    blockId={blockId}
                    isActive={activeTab === blockId}
                    isDragging={activeId === blockId}
                    onClick={() => setActiveTab(blockId)}
                  />
                ))}
              </div>
            </SortableContext>
            
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
                
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => onToggleMaximizeBlock(activeTab)}
                  className="h-7 w-7 p-0"
                  title="Fullscreen"
                >
                  <Maximize2 className="h-3.5 w-3.5" />
                </Button>
              </div>
            )}
          </div>
          
          {panel.blockIds.map(blockId => (
            <div
              key={blockId}
              className={`flex-1 overflow-auto p-6 sm:p-8 md:p-12 ${
                activeTab === blockId ? 'block' : 'hidden'
              }`}
            >
              {blockStates[blockId] && (
                <PreviewRenderer
                  serializedState={blockStates[blockId]!}
                  projectId={projectId}
                  storageAdapter={storageAdapter}
                />
              )}
            </div>
          ))}
        </>
      )}
    </div>
  )
}

function PreviewTab({ blockId, isDragging }: { blockId: string; isDragging: boolean }) {
  return (
    <div className={`flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300 ${
      isDragging ? 'opacity-50' : ''
    }`}>
      <GripVertical className="h-4 w-4 text-gray-400" />
      <span>Block {parseInt(blockId.slice(1))}</span>
    </div>
  )
}

function PreviewTabButton({ 
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
