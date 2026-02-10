/**
 * Slideshow Structure System (type3)
 * 
 * Each slide references a type2 project instead of containing blocks inline.
 * Type2 projects can be:
 *   - "dependent" (isDependent=true): stored in ProjectData.deps of the type3 project
 *   - "independent" (isDependent=false): standalone projects, loaded via git history
 * 
 * loadMode determines how independent projects are loaded:
 *   - 'head': always loads the latest commit from git history
 *   - 'snapshot': loads a specific tagged version from git history
 */

import type { ProjectData } from './enhanced-storage-adapter'
import { PROJECT_TYPES } from './project-types'
import type { StorageType } from './storage-types'
import { STORAGE_TYPES } from './storage-types'

/**
 * Reference from a slide to its type2 project
 */
export interface SlideProjectRef {
  projectId: string        // ID of the type2 project
  isDependent: boolean     // true = in deps, false = standalone project
  snapshotTag?: string     // tag name for loadMode='snapshot'
  loadMode: 'snapshot' | 'head'  // how to load independent projects
}

/**
 * A single slide in the slideshow
 */
export interface SlideData {
  id: string             // format: s1, s2, s3...
  name?: string          // Optional slide name
  projectRef: SlideProjectRef  // Reference to the type2 project
}

export type PreviewMode = "continuous" | "slide"

/**
 * Top-level slideshow structure (stored in ProjectData.data)
 */
export interface SlideshowStructure {
  version: "slideshow-v1"
  slides: SlideData[]
}

/**
 * Detects if data is in slideshow format
 */
export function isSlideshowStructure(data: string): boolean {
  try {
    const parsed = JSON.parse(data)
    return parsed.version === "slideshow-v1" && Array.isArray(parsed.slides)
  } catch {
    return false
  }
}

/**
 * Creates the default empty type2 project data (multi-block with b1)
 */
function createEmptyType2Data(): string {
  return JSON.stringify({ b1: JSON.stringify([]) })
}

/**
 * Creates a new dependent type2 project
 */
export function createDependentProject(
  parentProjectId: string,
  slideId: string,
  name?: string
): ProjectData {
  const now = new Date().toISOString()
  const projectId = `${parentProjectId}-${slideId}`
  
  return {
    id: projectId,
    name: name || `Slide ${slideId}`,
    type: PROJECT_TYPES.TYPE2,
    data: createEmptyType2Data(),
    tags: [],
    size: 0,
    createdAt: now,
    updatedAt: now,
    hash: '',
    storageType: STORAGE_TYPES.LOCAL as StorageType,
  }
}

/**
 * Creates a new empty slideshow structure with one slide.
 * Returns both the structure and the deps array with the dependent type2 project.
 */
export function createEmptySlideshowStructure(
  parentProjectId: string
): { structure: SlideshowStructure; deps: ProjectData[] } {
  const slideId = 's1'
  const depProject = createDependentProject(parentProjectId, slideId, 'Slide 1')
  
  const structure: SlideshowStructure = {
    version: "slideshow-v1",
    slides: [
      {
        id: slideId,
        name: "Slide 1",
        projectRef: {
          projectId: depProject.id,
          isDependent: true,
          loadMode: 'head',
        }
      }
    ]
  }
  
  return { structure, deps: [depProject] }
}

/**
 * Adds a new slide with a new dependent type2 project.
 * Returns updated structure, deps, and the new project.
 */
export function addSlide(
  structure: SlideshowStructure,
  parentProjectId: string,
  deps: ProjectData[],
  position?: number
): { structure: SlideshowStructure; deps: ProjectData[]; newProject: ProjectData } {
  const slideId = generateSlideId(structure)
  const depProject = createDependentProject(
    parentProjectId,
    slideId,
    `Slide ${structure.slides.length + 1}`
  )
  
  const newSlide: SlideData = {
    id: slideId,
    name: `Slide ${structure.slides.length + 1}`,
    projectRef: {
      projectId: depProject.id,
      isDependent: true,
      loadMode: 'head',
    }
  }
  
  const newSlides = [...structure.slides]
  if (position !== undefined) {
    newSlides.splice(position, 0, newSlide)
  } else {
    newSlides.push(newSlide)
  }
  
  return {
    structure: { ...structure, slides: newSlides },
    deps: [...deps, depProject],
    newProject: depProject,
  }
}

/**
 * Removes a slide and its dependent project (if dependent)
 */
export function removeSlide(
  structure: SlideshowStructure,
  slideId: string,
  deps: ProjectData[]
): { structure: SlideshowStructure; deps: ProjectData[] } {
  const slide = structure.slides.find(s => s.id === slideId)
  const newSlides = structure.slides.filter(s => s.id !== slideId)
  
  let newDeps = deps
  if (slide?.projectRef.isDependent) {
    newDeps = deps.filter(d => d.id !== slide.projectRef.projectId)
  }
  
  return {
    structure: { ...structure, slides: newSlides },
    deps: newDeps,
  }
}

/**
 * Reorders slides (deps are unaffected — they're referenced by projectId)
 */
export function reorderSlides(
  structure: SlideshowStructure,
  fromIndex: number,
  toIndex: number
): SlideshowStructure {
  const newSlides = [...structure.slides]
  const [movedSlide] = newSlides.splice(fromIndex, 1)
  
  if (!movedSlide) {
    return structure
  }
  
  newSlides.splice(toIndex, 0, movedSlide)
  
  return { ...structure, slides: newSlides }
}

/**
 * Updates a slide's name
 */
export function updateSlideName(
  structure: SlideshowStructure,
  slideId: string,
  name: string
): SlideshowStructure {
  return {
    ...structure,
    slides: structure.slides.map(slide =>
      slide.id === slideId ? { ...slide, name } : slide
    )
  }
}

/**
 * Converts a dependent type2 project to independent.
 * Removes from deps, assigns new ID, updates slide reference.
 * Caller must save the extracted project as standalone.
 */
export function convertToIndependent(
  structure: SlideshowStructure,
  slideId: string,
  deps: ProjectData[],
  newIndependentId: string
): {
  structure: SlideshowStructure
  deps: ProjectData[]
  extractedProject: ProjectData
} {
  const slide = structure.slides.find(s => s.id === slideId)
  if (!slide || !slide.projectRef.isDependent) {
    throw new Error(`Slide ${slideId} is not dependent or does not exist`)
  }
  
  const depProject = deps.find(d => d.id === slide.projectRef.projectId)
  if (!depProject) {
    throw new Error(`Dependent project ${slide.projectRef.projectId} not found in deps`)
  }
  
  const extractedProject: ProjectData = {
    ...depProject,
    id: newIndependentId,
    updatedAt: new Date().toISOString(),
  }
  
  const newStructure: SlideshowStructure = {
    ...structure,
    slides: structure.slides.map(s =>
      s.id === slideId
        ? {
            ...s,
            projectRef: {
              projectId: newIndependentId,
              isDependent: false,
              loadMode: 'head',
            }
          }
        : s
    )
  }
  
  const newDeps = deps.filter(d => d.id !== slide.projectRef.projectId)
  
  return { structure: newStructure, deps: newDeps, extractedProject }
}

/**
 * Converts an independent type2 project to dependent (creates a copy in deps).
 * Original independent project remains unchanged.
 */
export function convertToDependent(
  structure: SlideshowStructure,
  slideId: string,
  deps: ProjectData[],
  independentProject: ProjectData,
  parentProjectId: string
): {
  structure: SlideshowStructure
  deps: ProjectData[]
} {
  const slide = structure.slides.find(s => s.id === slideId)
  if (!slide) {
    throw new Error(`Slide ${slideId} does not exist`)
  }
  
  const depId = `${parentProjectId}-${slideId}`
  const depProject: ProjectData = {
    ...independentProject,
    id: depId,
    updatedAt: new Date().toISOString(),
  }
  
  const newStructure: SlideshowStructure = {
    ...structure,
    slides: structure.slides.map(s =>
      s.id === slideId
        ? {
            ...s,
            projectRef: {
              projectId: depId,
              isDependent: true,
              loadMode: 'head',
            }
          }
        : s
    )
  }
  
  return {
    structure: newStructure,
    deps: [...deps, depProject],
  }
}

/**
 * Imports an independent type2 project into a slide (reference only, no copy).
 */
export function importProjectToSlide(
  structure: SlideshowStructure,
  slideId: string,
  independentProjectId: string,
  loadMode: 'snapshot' | 'head',
  snapshotTag?: string
): SlideshowStructure {
  return {
    ...structure,
    slides: structure.slides.map(s =>
      s.id === slideId
        ? {
            ...s,
            projectRef: {
              projectId: independentProjectId,
              isDependent: false,
              loadMode,
              snapshotTag: loadMode === 'snapshot' ? snapshotTag : undefined,
            }
          }
        : s
    )
  }
}

/**
 * Updates the data of a dependent project in deps
 */
export function updateDependentProjectData(
  deps: ProjectData[],
  projectId: string,
  newData: string
): ProjectData[] {
  return deps.map(dep =>
    dep.id === projectId
      ? { ...dep, data: newData, updatedAt: new Date().toISOString() }
      : dep
  )
}

/**
 * Gets a dependent project from deps by projectId
 */
export function getDependentProject(
  deps: ProjectData[],
  projectId: string
): ProjectData | undefined {
  return deps.find(d => d.id === projectId)
}

/**
 * Generates a unique slide ID in format s1, s2, s3...
 */
export function generateSlideId(structure?: SlideshowStructure): string {
  if (!structure || structure.slides.length === 0) {
    return "s1"
  }
  
  const existingNumbers = structure.slides
    .map(slide => {
      const match = slide.id.match(/^s(\d+)$/)
      return match && match[1] ? parseInt(match[1], 10) : 0
    })
    .filter(n => n > 0)
  
  const maxNumber = existingNumbers.length > 0 ? Math.max(...existingNumbers) : 0
  return `s${maxNumber + 1}`
}

/**
 * Serializes slideshow structure to JSON string
 */
export function serializeSlideshowStructure(structure: SlideshowStructure): string {
  return JSON.stringify(structure)
}

/**
 * Parses slideshow structure from JSON string
 */
export function parseSlideshowStructure(data: string): SlideshowStructure {
  return JSON.parse(data) as SlideshowStructure
}
