/**
 * Project Resolver
 * 
 * Resolves slide project references to actual ProjectData.
 * - Dependent projects: resolved from ProjectData.deps
 * - Independent projects (head): resolved via git history HEAD commit
 * - Independent projects (snapshot): resolved via git history snapshot tag
 */

import type { ProjectData } from './enhanced-storage-adapter'
import type { SlideProjectRef } from './slideshow-structure'
import { getHistoryManager } from '../git'

/**
 * Resolves a slide's project reference to its actual ProjectData
 * 
 * @param projectRef - The slide's reference to a type2 project
 * @param type3ProjectData - The full type3 project (with deps)
 * @param storageAdapter - Optional storage adapter for loading independent projects directly
 * @returns The resolved ProjectData, or null if not found
 */
export async function resolveSlideProject(
  projectRef: SlideProjectRef,
  type3ProjectData: ProjectData,
  storageAdapter?: { load: (id: string) => Promise<ProjectData | null> }
): Promise<ProjectData | null> {
  if (projectRef.isDependent) {
    // Dependent: find in deps
    return resolveDependentProject(projectRef.projectId, type3ProjectData)
  } else {
    // Independent: load from git history
    return resolveIndependentProject(projectRef, storageAdapter)
  }
}

/**
 * Resolves a dependent project from type3's deps array
 */
function resolveDependentProject(
  projectId: string,
  type3ProjectData: ProjectData
): ProjectData | null {
  if (!type3ProjectData.deps || type3ProjectData.deps.length === 0) {
    console.warn(`No deps found for type3 project ${type3ProjectData.id}`)
    return null
  }
  
  const depProject = type3ProjectData.deps.find(d => d.id === projectId)
  if (!depProject) {
    console.warn(`Dependent project ${projectId} not found in deps of ${type3ProjectData.id}`)
    return null
  }
  
  return depProject
}

/**
 * Resolves an independent project from git history
 * - loadMode='head': loads the latest commit (HEAD)
 * - loadMode='snapshot': loads from a specific snapshot tag
 */
async function resolveIndependentProject(
  projectRef: SlideProjectRef,
  storageAdapter?: { load: (id: string) => Promise<ProjectData | null> }
): Promise<ProjectData | null> {
  const historyManager = getHistoryManager()
  const { projectId, loadMode, snapshotTag } = projectRef
  
  try {
    let serializedData: string | null = null
    
    if (loadMode === 'snapshot' && snapshotTag) {
      // Load from specific snapshot tag
      serializedData = await historyManager.loadSnapshot(projectId, snapshotTag)
    } else {
      // Load from HEAD (latest commit)
      const history = await historyManager.listHistory(projectId, 1)
      if (history.length > 0 && history[0]) {
        serializedData = await historyManager.loadCommit(projectId, history[0].sha)
      }
    }
    
    if (serializedData) {
      try {
        const parsed = JSON.parse(serializedData)
        return parsed as ProjectData
      } catch {
        console.warn(`Failed to parse git data for project ${projectId}`)
      }
    }
    
    // Fallback: try loading directly from storage adapter
    if (storageAdapter) {
      return await storageAdapter.load(projectId)
    }
    
    return null
  } catch (error) {
    console.error(`Failed to resolve independent project ${projectId}:`, error)
    
    // Fallback to storage adapter
    if (storageAdapter) {
      try {
        return await storageAdapter.load(projectId)
      } catch {
        return null
      }
    }
    
    return null
  }
}

/**
 * Resolves all slide projects for a type3 project
 * Returns a map of slideId -> ProjectData
 */
export async function resolveAllSlideProjects(
  type3ProjectData: ProjectData,
  storageAdapter?: { load: (id: string) => Promise<ProjectData | null> }
): Promise<Map<string, ProjectData | null>> {
  const results = new Map<string, ProjectData | null>()
  
  try {
    const slideshowData = JSON.parse(type3ProjectData.data)
    if (!slideshowData.slides || !Array.isArray(slideshowData.slides)) {
      return results
    }
    
    const resolvePromises = slideshowData.slides.map(async (slide: any) => {
      if (slide.projectRef) {
        const project = await resolveSlideProject(
          slide.projectRef,
          type3ProjectData,
          storageAdapter
        )
        results.set(slide.id, project)
      } else {
        results.set(slide.id, null)
      }
    })
    
    await Promise.all(resolvePromises)
  } catch (error) {
    console.error('Failed to resolve all slide projects:', error)
  }
  
  return results
}

/**
 * Creates a snapshot for an independent project before importing.
 * If HEAD doesn't have a tag, creates one automatically.
 * 
 * @returns The snapshot tag name
 */
export async function ensureSnapshotForImport(
  projectId: string,
  projectName: string
): Promise<string | null> {
  const historyManager = getHistoryManager()
  
  try {
    const hasHistory = await historyManager.hasHistory(projectId)
    if (!hasHistory) {
      return null
    }
    
    // Check if HEAD already has a snapshot tag
    const snapshots = await historyManager.listSnapshots(projectId)
    const history = await historyManager.listHistory(projectId, 1)
    
    if (history.length === 0 || !history[0]) {
      return null
    }
    
    const headSha = history[0].sha
    const existingTag = snapshots.find(s => s.sha === headSha)
    
    if (existingTag) {
      return existingTag.tag
    }
    
    // Create a snapshot tag for import
    const nextVersion = await historyManager.getNextVersionNumber(projectId, projectName)
    const tagName = `${projectName}-v${nextVersion}`
    
    await historyManager.createSnapshot(
      projectId,
      tagName,
      `Snapshot for import into slideshow`
    )
    
    return tagName
  } catch (error) {
    console.error(`Failed to ensure snapshot for import of ${projectId}:`, error)
    return null
  }
}
