import type { PanelData } from "../types"

/**
 * Sort blocks by numeric ID
 */
export function sortBlocks(blocks: string[]): string[] {
  return blocks.sort((a, b) => {
    const numA = parseInt(a.slice(1))
    const numB = parseInt(b.slice(1))
    return numA - numB
  })
}

/**
 * Get all blocks from all panels
 */
export function getAllPanelBlocks(panels: PanelData[]): string[] {
  return panels.flatMap(p => p.blockIds)
}

/**
 * Find missing blocks (blocks that exist but aren't in any panel)
 */
export function findMissingBlocks(blocks: string[], panels: PanelData[]): string[] {
  const allPanelBlocks = getAllPanelBlocks(panels)
  return blocks.filter(b => !allPanelBlocks.includes(b))
}

/**
 * Find removed blocks (blocks in panels but don't exist anymore)
 */
export function findRemovedBlocks(blocks: string[], panels: PanelData[]): string[] {
  const allPanelBlocks = getAllPanelBlocks(panels)
  return allPanelBlocks.filter(b => !blocks.includes(b))
}

/**
 * Check if panel configuration needs sync
 */
export function needsSync(blocks: string[], panels: PanelData[]): boolean {
  const missingBlocks = findMissingBlocks(blocks, panels)
  const removedBlocks = findRemovedBlocks(blocks, panels)
  return missingBlocks.length > 0 || removedBlocks.length > 0
}
