"use client"

import { useState, useRef, useEffect } from "react"
import type { LexicalEditor } from "lexical"
import { Button } from "@/components/ui/button"
import { Plus } from "lucide-react"
import { toast } from "sonner"
import type { ProjectMode } from "@/lib/storage/editor/project-modes"
import type {
  SequentialPanelStructure,
  PanelLayoutType,
} from "@/lib/storage/editor/panel-structure"
import {
  addPanel,
  removePanel,
  reorderPanels,
  updatePanelName,
  updatePanelState,
} from "@/lib/storage/editor/panel-structure"
import { PanelNavigationSidebar } from "./panel-navigation-sidebar"
import { EditorLayoutType1 } from "./editor-layout-type1"
import { EditorLayoutType2 } from "./editor-layout-type2"

interface EditorLayoutSequentialProps {
  structure: SequentialPanelStructure
  onStructureChange: (structure: SequentialPanelStructure) => void
  currentPanelIndex: number
  onPanelIndexChange: (index: number) => void
  panelEditorRefs: Map<string, React.RefObject<LexicalEditor>>
  onPanelEditorRefsChange: (refs: Map<string, React.RefObject<LexicalEditor>>) => void
  onLoadingChange: (setLoading: (loading: boolean) => void) => void
  projectId: string
  mode: ProjectMode
  currentProjectType?: "type1" | "type2" | "type3"
  storageAdapter?: any
}

export function EditorLayoutSequential({
  structure,
  onStructureChange,
  currentPanelIndex,
  onPanelIndexChange,
  panelEditorRefs,
  onPanelEditorRefsChange,
  onLoadingChange,
  projectId,
  mode,
  currentProjectType,
  storageAdapter,
}: EditorLayoutSequentialProps) {
  const panelContainerRefs = useRef<Map<string, HTMLDivElement>>(new Map())

  const handleSidebarPanelSelect = (index: number) => {
    onPanelIndexChange(index)
    // Scroll only when clicking from sidebar
    const currentPanel = structure.panels[index]
    if (currentPanel) {
      const panelElement = panelContainerRefs.current.get(currentPanel.id)
      if (panelElement) {
        panelElement.scrollIntoView({
          behavior: "smooth",
          block: "start",
        })
      }
    }
  }

  const handlePanelAdd = (type: PanelLayoutType) => {
    const newStructure = addPanel(structure, type)
    onStructureChange(newStructure)
    // Initialize ref for new panel
    const lastPanel = newStructure.panels[newStructure.panels.length - 1]
    const newRefs = new Map(panelEditorRefs)
    if (lastPanel) {
      newRefs.set(lastPanel.id, { current: undefined as any })
    }
    onPanelEditorRefsChange(newRefs)
    // Navigate to new panel
    onPanelIndexChange(newStructure.panels.length - 1)
  }

  const handlePanelRemove = (panelId: string) => {
    if (structure.panels.length === 1) {
      toast.error("Cannot remove last panel", {
        description: "At least one panel is required",
        duration: 3000,
      })
      return
    }
    const newStructure = removePanel(structure, panelId)
    onStructureChange(newStructure)
    // Remove ref
    const newRefs = new Map(panelEditorRefs)
    newRefs.delete(panelId)
    onPanelEditorRefsChange(newRefs)
    // Adjust current index if needed
    if (currentPanelIndex >= newStructure.panels.length) {
      onPanelIndexChange(newStructure.panels.length - 1)
    }
  }

  const handlePanelReorder = (fromIndex: number, toIndex: number) => {
    const newStructure = reorderPanels(structure, fromIndex, toIndex)
    onStructureChange(newStructure)
    // Update current index to follow the moved panel
    if (currentPanelIndex === fromIndex) {
      onPanelIndexChange(toIndex)
    } else if (currentPanelIndex === toIndex) {
      onPanelIndexChange(fromIndex < toIndex ? toIndex - 1 : toIndex + 1)
    }
  }

  const handlePanelNameChange = (panelId: string, name: string) => {
    const newStructure = updatePanelName(structure, panelId, name)
    onStructureChange(newStructure)
  }

  const handleAddPanelAtPosition = (type: PanelLayoutType, position: number) => {
    const newStructure = addPanel(structure, type, position)
    onStructureChange(newStructure)
    const newPanel = newStructure.panels[position]
    const newRefs = new Map(panelEditorRefs)
    if (newPanel) {
      newRefs.set(newPanel.id, { current: undefined as any })
    }
    onPanelEditorRefsChange(newRefs)
    onPanelIndexChange(position)
  }

  return (
    <div className="flex gap-0 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-white dark:bg-gray-900">
      {/* Panel Navigation Sidebar */}
      <div className="w-64 border-r border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
        <PanelNavigationSidebar
          panels={structure.panels}
          currentPanelIndex={currentPanelIndex}
          onPanelSelect={handleSidebarPanelSelect}
          onPanelAdd={handlePanelAdd}
          onPanelRemove={handlePanelRemove}
          onPanelReorder={handlePanelReorder}
          onPanelNameChange={handlePanelNameChange}
        />
      </div>

      {/* Continuous Scroll Container - All panels visible */}
      <div className="flex-1 overflow-y-auto max-h-[calc(100vh-12rem)] bg-gray-50 dark:bg-gray-950">
        <div className="space-y-4 p-6">
          {structure.panels.map((panel, index) => (
            <div 
              key={panel.id}
              ref={(el) => {
                if (el) {
                  panelContainerRefs.current.set(panel.id, el)
                } else {
                  panelContainerRefs.current.delete(panel.id)
                }
              }}
            >
              <div
                className={`border-2 transition-all rounded-lg overflow-hidden ${
                  currentPanelIndex === index
                    ? "border-blue-500 shadow-lg ring-2 ring-blue-200 dark:ring-blue-800"
                    : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                }`}
                onClick={() => onPanelIndexChange(index)}
              >
                {/* Panel Header */}
                <div className="bg-white dark:bg-gray-900 px-4 py-2 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                      {panel.name || `Panel ${panel.order + 1}`}
                    </span>
                    <span className="text-xs px-2 py-0.5 rounded bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400">
                      {panel.type === "single" ? "Single" : "Dual"}
                    </span>
                  </div>
                </div>

                {/* Panel Content */}
                {panel.type === "single" ? (
                  <div className="bg-white dark:bg-gray-900">
                    <div className="max-w-4xl mx-auto">
                      <div className="sm:p-2 md:p-6">
                        <EditorLayoutType1
                          editorRef={panelEditorRefs.get(panel.id) as any}
                          editorState={
                            panel.blocks && Object.keys(panel.blocks).length > 0
                              ? (typeof Object.values(panel.blocks)[0] === "string"
                                  ? Object.values(panel.blocks)[0] as string
                                  : JSON.stringify(Object.values(panel.blocks)[0] || ""))
                              : ""
                          }
                          onEditorChange={(newState) => {
                            const firstBlockId = Object.keys(panel.blocks || {})[0] || "b1";
                            const newStructure = updatePanelState(structure, panel.id, {
                              [firstBlockId]: newState,
                            })
                            onStructureChange(newStructure)
                          }}
                          onLoadingChange={(setLoading) => {
                            if (currentPanelIndex === index) {
                              onLoadingChange(setLoading)
                            }
                          }}
                          projectId={projectId}
                          mode={mode}
                          currentProjectType={currentProjectType}
                          storageAdapter={storageAdapter}
                        />
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="bg-white dark:bg-gray-900">
                    <EditorLayoutType2
                      blockRefs={{ current: Object.keys(panel.blocks || {}).reduce((acc, blockId) => {
                        acc[blockId] = panelEditorRefs.get(`${panel.id}-${blockId}`)?.current || null;
                        return acc;
                      }, {} as Record<string, LexicalEditor | null>) }}
                      blockStates={Object.entries(panel.blocks || {}).reduce((acc, [blockId, blockState]) => {
                        acc[blockId] = typeof blockState === "string"
                          ? blockState
                          : JSON.stringify(blockState || "");
                        return acc;
                      }, {} as Record<string, string>)}
                      onBlockChange={(blockId: string, newState: string) => {
                        const newBlocks = { ...panel.blocks, [blockId]: newState };
                        const newStructure = updatePanelState(structure, panel.id, newBlocks)
                        onStructureChange(newStructure)
                      }}
                      onLoadingChange={(setLoading) => {
                        if (currentPanelIndex === index) {
                          onLoadingChange(setLoading)
                        }
                      }}
                      projectId={projectId}
                      mode={mode}
                      currentProjectType={currentProjectType}
                      storageAdapter={storageAdapter}
                    />
                  </div>
                )}
              </div>

              {/* Add Panel Button */}
              <div className="flex justify-center my-4">
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleAddPanelAtPosition("single", index + 1)}
                    className="gap-2 bg-white dark:bg-gray-800 border-dashed border-2 hover:border-blue-500 hover:bg-blue-50 dark:hover:bg-blue-950"
                  >
                    <Plus className="h-4 w-4" />
                    Add Single Panel
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleAddPanelAtPosition("dual", index + 1)}
                    className="gap-2 bg-white dark:bg-gray-800 border-dashed border-2 hover:border-blue-500 hover:bg-blue-50 dark:hover:bg-blue-950"
                  >
                    <Plus className="h-4 w-4" />
                    Add Dual Panel
                  </Button>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
