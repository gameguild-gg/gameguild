"use client"

import { useState, useEffect } from "react"
import type { SerializedEditorState } from "lexical"
import type { ProjectPreferences } from "@/lib/storage/editor/project-preferences"
import { PreviewRenderer } from "./preview-renderer"
import { 
  Maximize2, Minimize2,
  ChevronsLeft, ChevronsRight
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
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
import {
  DraggableTab,
  DraggableTabButton,
  EmptyPanel,
  CollapsedPanel,
  usePanelCollapse,
  usePanelSync,
  sortBlocks,
  DRAG_ACTIVATION_DISTANCE,
  type PanelData,
} from "../multi-block"

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
  const blocks = sortBlocks(Object.keys(blockStates))

  const [panels, setPanels] = useState<PanelData[]>(() => {
    const saved = preferences?.global?.advancedMultiBlockPanels
    if (saved && saved.length > 0) {
      return saved
    }
    // Default: Always start with 1 panel containing all blocks
    // Single Panel Mode will be applied automatically when panels.length === 1
    return [
      { id: 'panel-1', blockIds: blocks, defaultSize: 100 }
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
  }, [preferences?.global?.advancedMultiBlockPanels, projectId])

  const [maximizedBlock, setMaximizedBlock] = useState<string | null>(null)
  const [activeId, setActiveId] = useState<string | null>(null)

  // Use shared collapse hook
  const {
    collapsedPanels,
    panelRefs,
    handleCollapsedTabClick,
    onPanelCollapse,
    onPanelExpand,
  } = usePanelCollapse()

  // Use shared sync hook
  usePanelSync({
    blocks,
    panels,
    setPanels,
    preferences,
    projectId,
  })

  // Check if we're in single panel mode
  const isSinglePanelMode = panels.length === 1

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: DRAG_ACTIVATION_DISTANCE,
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  )

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
        onLayoutChange(newPanels, "horizontal")
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
          onLayoutChange(newPanels, "horizontal")
        }
      }
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

  // Single Panel Mode
  if (isSinglePanelMode) {
    const panel = panels[0]!
    const singleBlockWidth = preferences?.global?.type2SingleBlockWidth || "wide"
    const panelDirection = panel.direction || "horizontal"

    return (
      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <div className="flex flex-col h-full w-full bg-white dark:bg-gray-900">
        <div className="flex-1 overflow-hidden">
          <div className={singleBlockWidth === "narrow" ? "flex justify-center w-full h-full" : "h-full"}>
            <div className={`flex flex-col h-full bg-white shadow-sm dark:bg-gray-900 ${
              singleBlockWidth === "narrow" ? "w-full max-w-4xl" : "w-full"
            }`}>
              <PreviewPanelContent
                panel={panel}
                panels={panels}
                blockStates={blockStates}
                projectId={projectId}
                storageAdapter={storageAdapter}
                isEditable={isEditable}
                onToggleMaximizeBlock={handleToggleMaximizeBlock}
                onToggleCollapse={undefined}
                showCollapseButton={false}
                isFirstPanel={false}
                activeId={activeId}
              />
            </div>
          </div>
        </div>

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

  // Multi Panel Mode
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
                    <CollapsedPanel
                      panelId={panel.id}
                      blockIds={panel.blockIds}
                      isFirstPanel={isFirstPanel}
                      onTabClick={handleCollapsedTabClick}
                      onToggleCollapse={handleToggleCollapsePanel}
                    />
                  ) : (
                    <PreviewPanelContent
                      panel={panel}
                      panels={panels}
                      blockStates={blockStates}
                      projectId={projectId}
                      storageAdapter={storageAdapter}
                      isEditable={isEditable}
                      onToggleMaximizeBlock={handleToggleMaximizeBlock}
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
// Single Panel Tabs Component
function PreviewSinglePanelTabs({
  blockIds,
  blockStates,
  projectId,
  storageAdapter,
  onToggleMaximize,
}: {
  blockIds: string[]
  blockStates: Record<string, SerializedEditorState>
  projectId?: string
  storageAdapter?: { load: (id: string) => Promise<any> }
  onToggleMaximize: (blockId: string) => void
}) {
  const [activeTab, setActiveTab] = useState(blockIds[0] || "")

  useEffect(() => {
    if (!blockIds.includes(activeTab) && blockIds.length > 0) {
      setActiveTab(blockIds[0]!)
    }
  }, [blockIds, activeTab])

  return (
    <>
      <div className="flex items-center justify-between border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 px-2">
        <div className="flex items-center gap-1 overflow-x-auto flex-1 py-2">
          {blockIds.map(blockId => (
            <button
              key={blockId}
              onClick={() => setActiveTab(blockId)}
              className={`px-3 py-1.5 text-sm font-medium rounded transition-colors whitespace-nowrap ${
                activeTab === blockId
                  ? "bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 shadow-sm"
                  : "text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800"
              }`}
            >
              Block {parseInt(blockId.slice(1))}
            </button>
          ))}
        </div>
        
        <Button
          size="sm"
          variant="ghost"
          onClick={() => onToggleMaximize(activeTab)}
          className="h-7 w-7 p-0"
          title="Fullscreen"
        >
          <Maximize2 className="h-3.5 w-3.5" />
        </Button>
      </div>
      
      {blockIds.map(blockId => (
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
  onToggleMaximizeBlock,
  onToggleCollapse,
  showCollapseButton,
  isFirstPanel,
  activeId,
}: PreviewPanelContentProps) {
  const [activeTab, setActiveTab] = useState(panel.blockIds[0] || "")
  
  const isSinglePanelMode = panels.length === 1
  
  const { setNodeRef, isOver } = useSortable({
    id: panel.id,
    data: { type: 'panel' }
  })

  const panelDirection = panel.direction || "horizontal"

  useEffect(() => {
    if (!panel.blockIds.includes(activeTab) && panel.blockIds.length > 0) {
      setActiveTab(panel.blockIds[0]!)
    }
  }, [panel.blockIds, activeTab])

  if (panel.blockIds.length === 0) {
    return <EmptyPanel panelId={panel.id} isOver={isOver} showAddButton={false} />
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
            {isSinglePanelMode ? (
              <div className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300">
                <span>Block {parseInt(panel.blockIds[0]!.slice(1))}</span>
              </div>
            ) : (
              <DraggableTab blockId={panel.blockIds[0]!} isDragging={activeId === panel.blockIds[0]} />
            )}
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
      ) : panelDirection === "vertical" ? (
        // Multiple blocks: vertical layout (stacked) - painel tem barra de rolagem única
        <>
          <div className="shrink-0 flex items-center justify-end p-2 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
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
              </div>
            )}
          </div>
          <div className="flex-1 overflow-auto">
            {panel.blockIds.map((blockId, index) => (
              <div key={blockId} className={index > 0 ? "border-t border-gray-200 dark:border-gray-700" : ""}>
                <div className="p-6 sm:p-8 md:p-12">
                  {blockStates[blockId] && (
                    <PreviewRenderer
                      serializedState={blockStates[blockId]!}
                      projectId={projectId}
                      storageAdapter={storageAdapter}
                    />
                  )}
                </div>
              </div>
            ))}
          </div>
        </>
      ) : (
        // Multiple blocks: horizontal layout (tabs) - tabs fixas, cada block com barra própria
        <>
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
