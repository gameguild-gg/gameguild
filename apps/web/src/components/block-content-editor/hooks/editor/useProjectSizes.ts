"use client"

/**
 * useProjectSizes
 *
 * Derives the current project's serialized size + asset accounting from
 * the live `blocks` state. Recomputes whenever blocks change.
 */

import { useCallback, useEffect, useState } from "react"
import type { EnhancedStorageAdapter } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import { serializeProject } from "@/components/block-content-editor/lib/storage/editor/block-storage"
import type { BlockArray } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import { calculateProjectAssetsSize as calculateAssets } from "@/components/block-content-editor/extras/editor/project-assets-operations"

export interface ProjectAsset {
  id: string
  name: string
  size: number
  thumbnail?: string
  mimeType?: string
}

export interface UseProjectSizesReturn {
  projectSize: number
  assetsSize: number
  assets: ProjectAsset[]
  /** Imperative trigger to recompute asset stats for the given project id. */
  recalcAssets: (projectId: string) => Promise<void>
}

function estimateSize(data: string): number {
  return new Blob([data]).size / 1024
}

export function useProjectSizes(
  blocks: BlockArray,
  projectId: string,
  isDbInitialized: boolean,
  _db: EnhancedStorageAdapter,
): UseProjectSizesReturn {
  const [projectSize, setProjectSize] = useState<number>(0)
  const [assetsSize, setAssetsSize] = useState<number>(0)
  const [assets, setAssets] = useState<ProjectAsset[]>([])

  const recalcAssets = useCallback(async (id: string) => {
    await calculateAssets({
      projectId: id,
      setCurrentProjectAssetsSize: setAssetsSize,
      setCurrentProjectAssets: setAssets,
    })
  }, [])

  useEffect(() => {
    setProjectSize(estimateSize(serializeProject(blocks)))
  }, [blocks])

  useEffect(() => {
    if (projectId && isDbInitialized) {
      recalcAssets(projectId)
    } else {
      setAssetsSize(0)
    }
  }, [projectId, isDbInitialized, blocks, recalcAssets])

  return { projectSize, assetsSize, assets, recalcAssets }
}
