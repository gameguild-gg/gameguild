"use client"

import { useRef } from "react"
import type { LexicalEditor } from "lexical"
import { Button } from "@/components/ui/button"
import { Plus, Lock, Unlock, ExternalLink, Import } from "lucide-react"
import { toast } from "sonner"
import type { ProjectMode } from "@/lib/storage/editor/project-modes"
import type { ProjectPreferences } from "@/lib/storage/editor/project-preferences"
import type { ProjectData } from "@/lib/storage/editor/enhanced-storage-adapter"
import type {
  SlideshowStructure,
} from "@/lib/storage/editor/slideshow-structure"
import {
  addSlide,
  removeSlide,
  reorderSlides,
  updateSlideName,
  updateDependentProjectData,
  getDependentProject,
} from "@/lib/storage/editor/slideshow-structure"
import { SlideNavigationSidebar } from "./slide-navigation-sidebar"
import { EditorLayoutType2 } from "./editor-layout-type2"
import { type ProjectType } from "@/lib/storage/editor/project-types"

interface EditorLayoutSlideshowProps {
  structure: SlideshowStructure
  onStructureChange: (structure: SlideshowStructure) => void
  deps: ProjectData[]
  onDepsChange: (deps: ProjectData[]) => void
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
  readOnly?: boolean
  // For resolving independent projects
  resolvedProjects?: Map<string, ProjectData | null>
  onConvertToIndependent?: (slideId: string) => void
  onConvertToDependent?: (slideId: string) => void
  onImportProject?: (slideId: string) => void
}

export function EditorLayoutSlideshow({
  structure,
  onStructureChange,
  deps,
  onDepsChange,
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
  readOnly = false,
  resolvedProjects,
  onConvertToIndependent,
  onConvertToDependent,
  onImportProject,
}: EditorLayoutSlideshowProps) {
  const slideContainerRefs = useRef<Map<string, HTMLDivElement>>(new Map())

  const handleSidebarSlideSelect = (index: number) => {
    onSlideIndexChange(index)
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
    const result = addSlide(structure, projectId, deps)
    onStructureChange(result.structure)
    onDepsChange(result.deps)
    // Initialize ref for new slide
    const lastSlide = result.structure.slides[result.structure.slides.length - 1]
    const newRefs = new Map(slideEditorRefs)
    if (lastSlide) {
      newRefs.set(lastSlide.id, { current: undefined as any })
    }
    onSlideEditorRefsChange(newRefs)
    onSlideIndexChange(result.structure.slides.length - 1)
  }

  const handleSlideRemove = (slideId: string) => {
    if (structure.slides.length === 1) {
      toast.error("Cannot remove last slide", {
        description: "At least one slide is required",
        duration: 3000,
      })
      return
    }
    const result = removeSlide(structure, slideId, deps)
    onStructureChange(result.structure)
    onDepsChange(result.deps)
    const newRefs = new Map(slideEditorRefs)
    newRefs.delete(slideId)
    onSlideEditorRefsChange(newRefs)
    if (currentSlideIndex >= result.structure.slides.length) {
      onSlideIndexChange(result.structure.slides.length - 1)
    }
  }

  const handleSlideReorder = (fromIndex: number, toIndex: number) => {
    const newStructure = reorderSlides(structure, fromIndex, toIndex)
    onStructureChange(newStructure)
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
    const result = addSlide(structure, projectId, deps, position)
    onStructureChange(result.structure)
    onDepsChange(result.deps)
    const newSlide = result.structure.slides[position]
    const newRefs = new Map(slideEditorRefs)
    if (newSlide) {
      newRefs.set(newSlide.id, { current: undefined as any })
    }
    onSlideEditorRefsChange(newRefs)
    onSlideIndexChange(position)
  }

  /**
   * Gets the type2 project data for a slide.
   * Dependent: from deps. Independent: from resolvedProjects.
   */
  const getSlideProjectData = (slide: (typeof structure.slides)[0]): ProjectData | null => {
    const { projectRef } = slide
    if (projectRef.isDependent) {
      return getDependentProject(deps, projectRef.projectId) || null
    } else {
      return resolvedProjects?.get(slide.id) || null
    }
  }

  /**
   * Gets the block states from a type2 project's data string.
   * Returns data in cells format - Editor handles cells→Lexical conversion internally.
   */
  const getBlockStatesFromProject = (project: ProjectData | null): Record<string, string> => {
    const emptyCells = JSON.stringify([])
    if (!project?.data) {
      return { b1: emptyCells }
    }
    try {
      const parsed = JSON.parse(project.data)
      if (typeof parsed === 'object' && parsed !== null) {
        // Type2 data format: { b1: [...cells...], b2: [...cells...], ... }
        // Pass cells directly - Editor converts to Lexical internally
        const result: Record<string, string> = {}
        for (const [key, value] of Object.entries(parsed)) {
          // Keep as cells format string
          result[key] = typeof value === 'string' ? value : JSON.stringify(value)
        }
        return Object.keys(result).length > 0 ? result : { b1: emptyCells }
      }
      return { b1: emptyCells }
    } catch (e) {
      console.error(`[getBlockStatesFromProject] Error parsing:`, e)
      return { b1: emptyCells }
    }
  }

  return (
    <div className="flex gap-0 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-white dark:bg-gray-900">
      {/* Slide Navigation Sidebar */}
      <div className="w-64 border-r border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
        <SlideNavigationSidebar
          slides={structure.slides}
          deps={deps}
          currentSlideIndex={currentSlideIndex}
          onSlideSelect={handleSidebarSlideSelect}
          onSlideAdd={readOnly ? undefined : handleSlideAdd}
          onSlideRemove={readOnly ? undefined : handleSlideRemove}
          onSlideReorder={readOnly ? undefined : handleSlideReorder}
          onSlideNameChange={readOnly ? undefined : handleSlideNameChange}
          readOnly={readOnly}
        />
      </div>

      {/* Continuous Scroll Container - All slides visible */}
      <div className="flex-1 overflow-y-auto max-h-[calc(100vh-12rem)] bg-gray-50 dark:bg-gray-950">
        <div className="space-y-4 p-6">
          {structure.slides.map((slide, index) => {
            const slideProject = getSlideProjectData(slide)
            const blockStates = getBlockStatesFromProject(slideProject)
            const isIndependent = !slide.projectRef.isDependent
            const isSlideReadOnly = readOnly || isIndependent
            // For dependent slides: stable key to preserve editor state during edits
            // For independent slides: include project id to force re-render when data loads
            const slideKey = isIndependent 
              ? `${slide.id}-${slideProject?.id || 'loading'}`
              : slide.id

            return (
              <div
                key={slideKey}
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
                      {/* Dependency status badge */}
                      {isIndependent ? (
                        <span className="flex items-center gap-1 text-xs px-2 py-0.5 rounded bg-amber-100 dark:bg-amber-900/50 text-amber-700 dark:text-amber-300">
                          <Lock className="h-3 w-3" />
                          Independent (readonly)
                        </span>
                      ) : (
                        <span className="flex items-center gap-1 text-xs px-2 py-0.5 rounded bg-green-100 dark:bg-green-900/50 text-green-700 dark:text-green-300">
                          <Unlock className="h-3 w-3" />
                          Dependent
                        </span>
                      )}
                      <span className="text-xs px-2 py-0.5 rounded bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400">
                        {Object.keys(blockStates).length} block{Object.keys(blockStates).length !== 1 ? 's' : ''}
                      </span>
                    </div>
                    {/* Action buttons */}
                    {!readOnly && (
                      <div className="flex items-center gap-1">
                        {isIndependent && onConvertToDependent && (
                          <Button
                            variant="outline"
                            size="sm"
                            className="h-7 text-xs gap-1"
                            onClick={(e) => {
                              e.stopPropagation()
                              onConvertToDependent(slide.id)
                            }}
                            title="Unlock for editing (creates a dependent copy)"
                          >
                            <Unlock className="h-3 w-3" />
                            Unlock Edit
                          </Button>
                        )}
                        {!isIndependent && onConvertToIndependent && (
                          <Button
                            variant="outline"
                            size="sm"
                            className="h-7 text-xs gap-1"
                            onClick={(e) => {
                              e.stopPropagation()
                              onConvertToIndependent(slide.id)
                            }}
                            title="Make this slide's project independent"
                          >
                            <ExternalLink className="h-3 w-3" />
                            Make Independent
                          </Button>
                        )}
                        {onImportProject && (
                          <Button
                            variant="outline"
                            size="sm"
                            className="h-7 text-xs gap-1"
                            onClick={(e) => {
                              e.stopPropagation()
                              onImportProject(slide.id)
                            }}
                            title="Import an existing type2 project"
                          >
                            <Import className="h-3 w-3" />
                            Import
                          </Button>
                        )}
                      </div>
                    )}
                  </div>

                  {/* Slide Content - EditorLayoutType2 for the referenced project */}
                  <div className="bg-white dark:bg-gray-900">
                    {/* For independent slides, only render editor when data is loaded */}
                    {isIndependent && !slideProject ? (
                      <div className="flex items-center justify-center h-32 text-gray-500 dark:text-gray-400">
                        <div className="text-center">
                          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-gray-500 mx-auto mb-2"></div>
                          <span className="text-sm">Loading independent project...</span>
                        </div>
                      </div>
                    ) : (
                      <EditorLayoutType2
                        key={`editor-${slide.id}`}
                        blockRefs={{ current: Object.keys(blockStates).reduce((acc, blockId) => {
                          acc[blockId] = slideEditorRefs.get(`${slide.id}-${blockId}`)?.current || null;
                          return acc;
                        }, {} as Record<string, LexicalEditor | null>) }}
                      blockStates={blockStates}
                      onBlockChange={isSlideReadOnly ? () => {} : (blockId: string, newState: string) => {
                        if (!slideProject || !slide.projectRef.isDependent) return
                        // newState is already in cells format from Editor
                        try {
                          const cellsData = JSON.parse(newState)
                          const currentData = JSON.parse(slideProject.data || '{}')
                          const updatedData = { ...currentData, [blockId]: cellsData }
                          const newDeps = updateDependentProjectData(
                            deps,
                            slide.projectRef.projectId,
                            JSON.stringify(updatedData)
                          )
                          onDepsChange(newDeps)
                        } catch (e) {
                          console.error('[slideshow onBlockChange] Error:', e)
                        }
                      }}
                      onBlockAdd={isSlideReadOnly ? undefined : () => {
                        if (!slideProject || !slide.projectRef.isDependent) return
                        const currentData = JSON.parse(slideProject.data || '{}')
                        const blockNumbers = Object.keys(currentData).map(key => parseInt(key.slice(1)))
                        const nextNum = Math.max(...blockNumbers, 0) + 1
                        const newBlockId = `b${nextNum}`
                        // Empty cells array for new block
                        const updatedData = { ...currentData, [newBlockId]: [] }
                        const newDeps = updateDependentProjectData(
                          deps,
                          slide.projectRef.projectId,
                          JSON.stringify(updatedData)
                        )
                        onDepsChange(newDeps)
                      }}
                      onBlockRemove={isSlideReadOnly ? undefined : (blockId: string) => {
                        if (!slideProject || !slide.projectRef.isDependent) return
                        const currentData = JSON.parse(slideProject.data || '{}')
                        if (Object.keys(currentData).length <= 1) return
                        const { [blockId]: _, ...rest } = currentData
                        const newDeps = updateDependentProjectData(
                          deps,
                          slide.projectRef.projectId,
                          JSON.stringify(rest)
                        )
                        onDepsChange(newDeps)
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
                      onPreferencesChange={isSlideReadOnly ? undefined : onPreferencesChange}
                      readOnly={isSlideReadOnly}
                    />
                    )}
                  </div>
                </div>

                {/* Add Slide Button - hidden in readOnly mode */}
                {!readOnly && (
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
                )}
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
