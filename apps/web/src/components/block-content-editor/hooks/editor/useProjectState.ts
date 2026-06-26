"use client"

/**
 * useProjectState
 *
 * Holds the runtime mutable state of the currently-open project — id, name,
 * storage type, tags, preferences, blocks, and timestamps. Pure state hook
 * with no side effects.
 */

import { useRef, useState, type Dispatch, type SetStateAction } from "react"
import type { BlockArray } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import type { ProjectPreferences } from "@/components/block-content-editor/lib/storage/editor/project-preferences"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"

export interface UseProjectStateReturn {
  projectId: string
  setProjectId: Dispatch<SetStateAction<string>>
  projectName: string
  setProjectName: Dispatch<SetStateAction<string>>
  storageType: StorageType
  setStorageType: Dispatch<SetStateAction<StorageType>>
  tags: string[]
  setTags: Dispatch<SetStateAction<string[]>>
  preferences: ProjectPreferences | undefined
  setPreferences: Dispatch<SetStateAction<ProjectPreferences | undefined>>
  isFirstTime: boolean
  setIsFirstTime: Dispatch<SetStateAction<boolean>>
  blocks: BlockArray
  setBlocks: Dispatch<SetStateAction<BlockArray>>
  lastProjectLoadTime: number
  setLastProjectLoadTime: Dispatch<SetStateAction<number>>
  /** Latest blocks ref — used by auto-save to avoid stale closures. */
  blocksRef: React.MutableRefObject<BlockArray>
}

export function useProjectState(): UseProjectStateReturn {
  const [projectId, setProjectId] = useState<string>("")
  const [projectName, setProjectName] = useState<string>("")
  const [storageType, setStorageType] = useState<StorageType>("local")
  const [tags, setTags] = useState<string[]>([])
  const [preferences, setPreferences] = useState<ProjectPreferences | undefined>(undefined)
  const [isFirstTime, setIsFirstTime] = useState(true)
  const [blocks, setBlocks] = useState<BlockArray>([])
  const [lastProjectLoadTime, setLastProjectLoadTime] = useState<number>(0)

  const blocksRef = useRef<BlockArray>(blocks)
  blocksRef.current = blocks

  return {
    projectId, setProjectId,
    projectName, setProjectName,
    storageType, setStorageType,
    tags, setTags,
    preferences, setPreferences,
    isFirstTime, setIsFirstTime,
    blocks, setBlocks,
    lastProjectLoadTime, setLastProjectLoadTime,
    blocksRef,
  }
}
