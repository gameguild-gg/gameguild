"use client"

import { useState, useRef, useEffect } from "react"
import type { LexicalEditor } from "lexical"
import { Button } from "@/components/ui/button"
import { Plus } from "lucide-react"
import { toast } from "sonner"
import type { ProjectMode } from "@/lib/storage/editor/project-modes"
import type { ProjectPreferences } from "@/lib/storage/editor/project-preferences"
import type {
  SlideshowStructure,
} from "@/lib/storage/editor/slideshow-structure"
import {
  addSlide,
  removeSlide,
  reorderSlides,
  updateSlideName,
  updateSlideState,
} from "@/lib/storage/editor/slideshow-structure"
import { SlideNavigationSidebar } from "./slide-navigation-sidebar"
import { EditorLayoutType2 } from "./editor-layout-type2"
import { type ProjectType} from "@/lib/storage/editor/project-types"

interface EditorLayoutSlideshowProps {
  structure: SlideshowStructure
  onStructureChange: (structure: SlideshowStructure) => void
  currentSlideIndex: number
  onSlideIndexChange: (index: number) => void
  slideEditorRefs: Map<string, React.RefObject<LexicalEditor>>
  onSlideEditorRefsChange: (refs: Map<string, React.RefObject<LexicalEditor>>) => void
  onLoadingChange: (setLoading: (loading: boolean) => void) => void
  projectId: string
  mode: ProjectMode
  currentProjectType?: ProjectType
  storageAdapter?: any
  preferences?: ProjectPreferences
  onPreferencesChange?: (preferences: ProjectPreferences) => void
}

export function EditorLayoutSlideshow({
  structure,
  onStructureChange,
  currentSlideIndex,
  onSlideIndexChange,
  slideEditorRefs,
  onSlideEditorRefsChange,
  onLoadingChange,
  projectId,
  mode,
  currentProjectType,
  storageAdapter,
  preferences,
  onPreferencesChange,
}: EditorLayoutSlideshowProps) {
  const slideContainerRefs = useRef<Map<string, HTMLDivElement>>(new Map())

  const handleSidebarSlideSelect = (index: number) => {
    onSlideIndexChange(index)
    // Scroll only when clicking from sidebar
    const currentSlide = structure.slides[index]
    if (currentSlide) {
      const slideElement = slideContainerRefs.current.get(currentSlide.id)
      if (slideElement) {
        slideElement.scrollIntoView({
          behavior: "smooth",
          block: "start",
        })
      }
    }
  }

  const handleSlideAdd = () => {
    const newStructure = addSlide(structure)
    onStructureChange(newStructure)
    // Initialize ref for new slide
    const lastSlide = newStructure.slides[newStructure.slides.length - 1]
    const newRefs = new Map(slideEditorRefs)
    if (lastSlide) {
      newRefs.set(lastSlide.id, { current: undefined as any })
    }
    onSlideEditorRefsChange(newRefs)
    // Navigate to new slide
    onSlideIndexChange(newStructure.slides.length - 1)
  }

  const handleSlideRemove = (slideId: string) => {
    if (structure.slides.length === 1) {
      toast.error("Cannot remove last slide", {
        description: "At least one slide is required",
        duration: 3000,
      })
      return
    }
    const newStructure = removeSlide(structure, slideId)
    onStructureChange(newStructure)
    // Remove ref
    const newRefs = new Map(slideEditorRefs)
    newRefs.delete(slideId)
    onSlideEditorRefsChange(newRefs)
    // Adjust current index if needed
    if (currentSlideIndex >= newStructure.slides.length) {
      onSlideIndexChange(newStructure.slides.length - 1)
    }
  }

  const handleSlideReorder = (fromIndex: number, toIndex: number) => {
    const newStructure = reorderSlides(structure, fromIndex, toIndex)
    onStructureChange(newStructure)
    // Update current index to follow the moved slide
    if (currentSlideIndex === fromIndex) {
      onSlideIndexChange(toIndex)
    } else if (currentSlideIndex === toIndex) {
      onSlideIndexChange(fromIndex < toIndex ? toIndex - 1 : toIndex + 1)
    }
  }

  const handleSlideNameChange = (slideId: string, name: string) => {
    const newStructure = updateSlideName(structure, slideId, name)
    onStructureChange(newStructure)
  }

  const handleAddSlideAtPosition = (position: number) => {
    const newStructure = addSlide(structure, position)
    onStructureChange(newStructure)
    const newSlide = newStructure.slides[position]
    const newRefs = new Map(slideEditorRefs)
    if (newSlide) {
      newRefs.set(newSlide.id, { current: undefined as any })
    }
    onSlideEditorRefsChange(newRefs)
    onSlideIndexChange(position)
  }

  return (
    <div className="flex gap-0 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-white dark:bg-gray-900">
      {/* Slide Navigation Sidebar */}
      <div className="w-64 border-r border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
        <SlideNavigationSidebar
          slides={structure.slides}
          currentSlideIndex={currentSlideIndex}
          onSlideSelect={handleSidebarSlideSelect}
          onSlideAdd={handleSlideAdd}
          onSlideRemove={handleSlideRemove}
          onSlideReorder={handleSlideReorder}
          onSlideNameChange={handleSlideNameChange}
        />
      </div>

      {/* Continuous Scroll Container - All slides visible */}
      <div className="flex-1 overflow-y-auto max-h-[calc(100vh-12rem)] bg-gray-50 dark:bg-gray-950">
        <div className="space-y-4 p-6">
          {structure.slides.map((slide, index) => (
            <div 
              key={slide.id}
              ref={(el) => {
                if (el) {
                  slideContainerRefs.current.set(slide.id, el)
                } else {
                  slideContainerRefs.current.delete(slide.id)
                }
              }}
            >
              <div
                className={`border-2 transition-all rounded-lg overflow-hidden ${
                  currentSlideIndex === index
                    ? "border-blue-500 shadow-lg ring-2 ring-blue-200 dark:ring-blue-800"
                    : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                }`}
                onClick={() => onSlideIndexChange(index)}
              >
                {/* Slide Header */}
                <div className="bg-white dark:bg-gray-900 px-4 py-2 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                      {slide.name || `Slide ${index + 1}`}
                    </span>
                    <span className="text-xs px-2 py-0.5 rounded bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400">
                      {Object.keys(slide.blocks || {}).length} block{Object.keys(slide.blocks || {}).length !== 1 ? 's' : ''}
                    </span>
                  </div>
                </div>

                {/* Slide Content - Always uses EditorLayoutType2 (multi-block system) */}
                <div className="bg-white dark:bg-gray-900">
                  <EditorLayoutType2
                    blockRefs={{ current: Object.keys(slide.blocks || {}).reduce((acc, blockId) => {
                      acc[blockId] = slideEditorRefs.get(`${slide.id}-${blockId}`)?.current || null;
                      return acc;
                    }, {} as Record<string, LexicalEditor | null>) }}
                    blockStates={Object.entries(slide.blocks || {}).reduce((acc, [blockId, blockState]) => {
                      acc[blockId] = typeof blockState === "string"
                        ? blockState
                        : JSON.stringify(blockState || "");
                      return acc;
                    }, {} as Record<string, string>)}
                    onBlockChange={(blockId: string, newState: string) => {
                      const newBlocks = { ...slide.blocks, [blockId]: newState };
                      const newStructure = updateSlideState(structure, slide.id, newBlocks)
                      onStructureChange(newStructure)
                    }}
                    onBlockAdd={() => {
                      // Find next block number
                      const blockNumbers = Object.keys(slide.blocks || {}).map(key => parseInt(key.slice(1)))
                      const nextNum = Math.max(...blockNumbers, 0) + 1
                      const newBlockId = `b${nextNum}`
                      
                      // Create empty cells structure
                      const emptyCells = JSON.stringify([])
                      
                      // Add new block
                      const newBlocks = { ...slide.blocks, [newBlockId]: emptyCells }
                      const newStructure = updateSlideState(structure, slide.id, newBlocks)
                      onStructureChange(newStructure)
                    }}
                    onBlockRemove={(blockId: string) => {
                      if (Object.keys(slide.blocks || {}).length <= 1) {
                        return // Prevent removing last block
                      }
                      
                      // Remove block
                      const newBlocks = { ...slide.blocks }
                      delete newBlocks[blockId]
                      const newStructure = updateSlideState(structure, slide.id, newBlocks)
                      onStructureChange(newStructure)
                    }}
                    onLoadingChange={(setLoading) => {
                      if (currentSlideIndex === index) {
                        onLoadingChange(setLoading)
                      }
                    }}
                    projectId={projectId}
                    mode={mode}
                    currentProjectType={currentProjectType}
                    storageAdapter={storageAdapter}
                    preferences={preferences}
                    onPreferencesChange={onPreferencesChange}
                  />
                </div>
              </div>

              {/* Add Slide Button */}
              <div className="flex justify-center my-4">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleAddSlideAtPosition(index + 1)}
                  className="gap-2 bg-white dark:bg-gray-800 border-dashed border-2 hover:border-blue-500 hover:bg-blue-50 dark:hover:bg-blue-950"
                >
                  <Plus className="h-4 w-4" />
                  Add Slide
                </Button>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
