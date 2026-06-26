"use client"

/**
 * useProjectOperations
 *
 * The behavior layer — save / saveAs / loadProject / createProject / title
 * editing / snapshot. Pure callbacks; depends on state setters from
 * `useProjectState`, the storage adapter, and the page-declared defaults
 * (project type / single-block mode / allowed block types) so structural
 * preferences are filled in on Save As when the project has none.
 */

import { useCallback } from "react"
import type { EnhancedStorageAdapter } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import { deserializeProject, serializeProject } from "@/components/block-content-editor/lib/storage/editor/block-storage"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"
import type { ProjectPreferences } from "@/components/block-content-editor/lib/storage/editor/project-preferences"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"
import type { ProjectType } from "@/components/block-content-editor/lib/storage/editor/project-types"
import type { BlockCellType } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import { generateProjectId } from "@/components/block-content-editor/lib/storage/editor/project-id"
import { handleSave as saveProject, handleSaveAs as saveAsProject } from "@/components/block-content-editor/extras/editor/project-save-operations"
import { handleTitleEdit as titleEdit, handleTitleSave as titleSave } from "@/components/block-content-editor/extras/editor/project-title-operations"

import type { UseProjectStateReturn } from "./useProjectState"
import type { StorageAdapterInterface, CreateProjectInput } from "../useProjectStorage"

export interface OperationDefaults {
  projectType?: ProjectType
  singleBlockMode?: boolean
  allowedBlockTypes?: BlockCellType[]
}

export interface UseProjectOperationsParams {
  state: UseProjectStateReturn
  storageAdapter: StorageAdapterInterface
  db: EnhancedStorageAdapter
  refreshProjects: () => Promise<void>
  recalcAssets: (projectId: string) => Promise<void>
  defaults?: OperationDefaults
}

export interface UseProjectOperationsReturn {
  save: () => Promise<{ needsSaveAs: boolean }>
  saveAs: (name: string, storageOption: StorageType, tags?: string[]) => Promise<void>
  loadProject: (projectData: ProjectData) => void
  createProject: (projectData: CreateProjectInput) => void
  titleEdit: (setEditingProjectName: (n: string) => void, setIsEditingTitle: (b: boolean) => void) => void
  titleSave: (editingName: string, setEditingProjectName: (n: string) => void, setIsEditingTitle: (b: boolean) => void) => Promise<void>
  createSnapshot: (name?: string) => Promise<void>
  generateProjectId: () => string
}

export function useProjectOperations({
  state,
  storageAdapter,
  db,
  refreshProjects,
  recalcAssets,
  defaults,
}: UseProjectOperationsParams): UseProjectOperationsReturn {
  const {
    projectId, projectName, storageType, tags, preferences, blocks,
    setProjectId, setProjectName, setStorageType, setTags, setPreferences,
    setBlocks, setIsFirstTime, setLastProjectLoadTime,
  } = state

  const buildSavePreferences = useCallback((): ProjectPreferences => ({
    global: {
      ...(defaults?.projectType !== undefined ? { projectType: defaults.projectType } : {}),
      ...(defaults?.singleBlockMode !== undefined ? { singleBlockMode: defaults.singleBlockMode } : {}),
      ...(defaults?.allowedBlockTypes !== undefined ? { allowedBlockTypes: defaults.allowedBlockTypes } : {}),
      ...preferences?.global,
    },
  }), [defaults, preferences])

  const save = useCallback(async (): Promise<{ needsSaveAs: boolean }> => {
    if (!projectId) return { needsSaveAs: true }
    const data = serializeProject(blocks)
    await saveProject({
      currentProjectId: projectId,
      currentProjectName: projectName,
      currentProjectStorageType: storageType,
      data,
      projectTags: tags,
      storageAdapter,
      calculateProjectAssetsSize: recalcAssets,
      setSaveAsDialogOpen: () => {},
      preferences: buildSavePreferences(),
    })
    return { needsSaveAs: false }
  }, [projectId, projectName, storageType, blocks, tags, storageAdapter, recalcAssets, buildSavePreferences])

  const saveAs = useCallback(async (name: string, storageOption: StorageType, nextTags?: string[]) => {
    const data = serializeProject(blocks)
    const tagsToSave = nextTags ?? tags
    await saveAsProject({
      newProjectName: name,
      data,
      projectTags: tagsToSave,
      storageOption,
      storageAdapter,
      generateProjectId,
      setCurrentProjectId: setProjectId,
      setCurrentProjectName: setProjectName,
      setCurrentProjectStorageType: setStorageType,
      setNewProjectName: () => {},
      setSaveAsDialogOpen: () => {},
      loadSavedProjectsList: refreshProjects,
      calculateProjectAssetsSize: recalcAssets,
      preferences: buildSavePreferences(),
    })
    if (nextTags) setTags(tagsToSave)
  }, [blocks, tags, storageAdapter, refreshProjects, recalcAssets, buildSavePreferences, setProjectId, setProjectName, setStorageType, setTags])

  const loadProject = useCallback((projectData: ProjectData) => {
    setBlocks(deserializeProject(projectData.data))
    setProjectId(projectData.id)
    setProjectName(projectData.name)
    setStorageType(projectData.storageType || "local")
    setTags(projectData.tags || [])
    setPreferences(projectData.preferences)
    setIsFirstTime(false)
    setLastProjectLoadTime(Date.now())
    window.history.pushState(null, "", `#${projectData.id}`)
  }, [setBlocks, setProjectId, setProjectName, setStorageType, setTags, setPreferences, setIsFirstTime, setLastProjectLoadTime])

  const createProject = useCallback((projectData: CreateProjectInput) => {
    setBlocks([])
    setLastProjectLoadTime(Date.now())
    setProjectId(projectData.id)
    setProjectName(projectData.name)
    setStorageType(projectData.storageType)
    setTags(projectData.tags)
    if (projectData.preferences) setPreferences(projectData.preferences)
    setIsFirstTime(false)
    window.history.pushState(null, "", `#${projectData.id}`)
  }, [setBlocks, setProjectId, setProjectName, setStorageType, setTags, setPreferences, setIsFirstTime, setLastProjectLoadTime])

  const handleTitleEdit = useCallback((
    setEditingProjectName: (n: string) => void,
    setIsEditingTitle: (b: boolean) => void,
  ) => {
    titleEdit({ currentProjectId: projectId, currentProjectName: projectName, setEditingProjectName, setIsEditingTitle })
  }, [projectId, projectName])

  const handleTitleSave = useCallback(async (
    editingName: string,
    setEditingProjectName: (n: string) => void,
    setIsEditingTitle: (b: boolean) => void,
  ) => {
    const data = serializeProject(blocks)
    await titleSave({
      editingProjectName: editingName,
      currentProjectName: projectName,
      currentProjectId: projectId,
      data,
      projectTags: tags,
      storageAdapter,
      setCurrentProjectName: setProjectName,
      setEditingProjectName,
      setIsEditingTitle,
      loadSavedProjectsList: refreshProjects,
    })
  }, [projectId, projectName, blocks, tags, storageAdapter, refreshProjects, setProjectName])

  const createSnapshot = useCallback(async (name?: string) => {
    if (!projectId) return
    await save()
    await db.createSnapshot(projectId, name)
  }, [projectId, save, db])

  return {
    save,
    saveAs,
    loadProject,
    createProject,
    titleEdit: handleTitleEdit,
    titleSave: handleTitleSave,
    createSnapshot,
    generateProjectId,
  }
}
