"use client"

import { ENGINE_TYPES } from "@/lib/storage/editor/project-types"
import { BlockArrayEditor } from "@/components/editor/engines/blocks/block-array-editor"
import { EditorLayoutType1 } from "@/components/editor/engines/lexical/editor-layout-type1"
import { EditorLayoutType2 } from "@/components/editor/engines/lexical/editor-layout-type2"
import { EditorLayoutSlideshow } from "@/components/editor/engines/lexical/editor-layout-slideshow"
import { useEditor } from "./editor-provider"

export function EditorField() {
  const { project, history, fieldConfig, ui } = useEditor()

  if (project.engine === ENGINE_TYPES.BLOCKS) {
    const noBlockTypes = !fieldConfig.allowedBlockTypes || fieldConfig.allowedBlockTypes.length <= 1
    const isQuizMode = fieldConfig.allowedModes?.includes("quiz-page")
    const hideBlocks = noBlockTypes || (isQuizMode && !fieldConfig.allowedBlockTypes?.length)
    return (
      <div className="border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 p-4">
        <BlockArrayEditor
          blocks={project.blockArrayBlocks}
          onChange={project.setBlockArrayBlocks}
          readOnly={history.isViewingHistory}
          allowedBlockTypes={fieldConfig.allowedBlockTypes}
          defaultPickerTab={hideBlocks || isQuizMode ? "templates" : "blocks"}
          hideBlockTypesTab={hideBlocks}
        />
      </div>
    )
  }

  if (project.layout === "slideshow" && project.slideshowStructure) {
    return (
      <EditorLayoutSlideshow
        structure={project.slideshowStructure}
        onStructureChange={project.setSlideshowStructure}
        deps={project.slideshowDeps}
        onDepsChange={project.setSlideshowDeps}
        currentSlideIndex={project.currentSlideIndex}
        onSlideIndexChange={project.setCurrentSlideIndex}
        slideEditorRefs={project.slideEditorRefs}
        onSlideEditorRefsChange={project.setSlideEditorRefs}
        onLoadingChange={(setLoading) => {
          project.setLoadingRef.current = setLoading
        }}
        projectId={project.projectId}
        mode={project.projectMode}
        currentProjectType={project.projectType}
        storageAdapter={project.storageAdapter}
        preferences={project.preferences}
        onPreferencesChange={project.setPreferences}
        readOnly={history.isViewingHistory}
        resolvedProjects={project.resolvedProjects}
        onConvertToIndependent={project.convertToIndependent}
        onConvertToDependent={project.convertToDependent}
        onImportProject={(slideId) => {
          ui.handleImportProject(slideId)
        }}
      />
    )
  }

  if (project.layout === "single") {
    return (
      <EditorLayoutType1
        editorRef={project.editorRef}
        editorState={project.editorState}
        onEditorChange={project.setEditorState}
        onLoadingChange={(setLoading) => {
          project.setLoadingRef.current = setLoading
        }}
        projectId={project.projectId}
        mode={project.projectMode}
        currentProjectType={project.projectType}
        storageAdapter={project.storageAdapter}
        readOnly={history.isViewingHistory}
      />
    )
  }

  return (
    <EditorLayoutType2
      blockRefs={project.blockRefs}
      blockStates={project.blockStates}
      onBlockChange={(blockId, newState) => {
        project.setBlockStates(prev => ({ ...prev, [blockId]: newState }))
      }}
      onBlockAdd={project.addBlock}
      onBlockRemove={project.removeBlock}
      onLoadingChange={(setLoading) => {
        project.setLoadingRef.current = setLoading
      }}
      projectId={project.projectId}
      mode={project.projectMode}
      currentProjectType={project.projectType}
      storageAdapter={project.storageAdapter}
      preferences={project.preferences}
      onPreferencesChange={project.setPreferences}
      currentProjectId={project.projectId}
      readOnly={history.isViewingHistory}
    />
  )
}
