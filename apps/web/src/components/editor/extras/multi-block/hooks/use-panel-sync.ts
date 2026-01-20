"use client"

import { useEffect, useRef } from "react"
import type { PanelData } from "../types"
import { findMissingBlocks, findRemovedBlocks } from "../utils/panel-helpers"

interface UsePanelSyncProps {
  blocks: string[]
  panels: PanelData[]
  setPanels: (panels: PanelData[] | ((prev: PanelData[]) => PanelData[])) => void
  pendingBlockPanel?: string | null
  projectId?: string
  preferences?: any
}

export function usePanelSync({
  blocks,
  panels,
  setPanels,
  pendingBlockPanel,
  projectId,
  preferences,
}: UsePanelSyncProps) {
  const isLoadingProject = useRef(false)

  // Track when preferences change to prevent auto-sync during project load
  useEffect(() => {
    if (preferences?.global?.advancedMultiBlockPanels) {
      isLoadingProject.current = true
      // Reset flag after panels are applied
      setTimeout(() => {
        isLoadingProject.current = false
      }, 500)
    }
  }, [preferences?.global?.advancedMultiBlockPanels, projectId])

  // Sync panels when blocks change
  useEffect(() => {
    // Skip auto-sync if we're loading a project with saved panel configuration
    if (isLoadingProject.current) {
      return
    }

    const missingBlocks = findMissingBlocks(blocks, panels)
    const removedBlocks = findRemovedBlocks(blocks, panels)

    if (missingBlocks.length > 0 || removedBlocks.length > 0) {
      setPanels(prev => {
        // Remove deleted blocks from panels, but keep empty panels
        let updated = prev.map(p => ({
          ...p,
          blockIds: p.blockIds.filter(b => blocks.includes(b))
        }))

        // Only auto-add missing blocks to panel 1 if there's no pending panel target
        if (missingBlocks.length > 0 && updated.length > 0 && !pendingBlockPanel) {
          updated[0] = {
            ...updated[0]!,
            blockIds: [...updated[0]!.blockIds, ...missingBlocks]
          }
        }

        // Only create default panel if all panels were removed and there are blocks
        if (updated.length === 0 && blocks.length > 0) {
          updated = [{ id: 'panel-1', blockIds: blocks, defaultSize: 100 }]
        }

        return updated
      })
    }
  }, [blocks, pendingBlockPanel])

  return {
    isLoadingProject,
  }
}
