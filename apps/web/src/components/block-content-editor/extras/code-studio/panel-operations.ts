import type { CodeStudioData, DisplayConfig, PanelType } from "./types"
import {
  addPanelAtRoot,
  moveLeafRelativeTo,
  removeLeaf,
  setSplitSizes,
  splitLeafWithPanel,
  toggleLeafEditorInstance,
} from "./tree-operations"

/**
 * Add a panel by splitting either the targeted leaf or, if none is given, by
 * splitting at the root.
 */
export function addPanel(
  draft: CodeStudioData,
  activeDisplay: DisplayConfig,
  type: PanelType,
  targetLeafId?: string,
  direction: "horizontal" | "vertical" = "horizontal",
  position: "before" | "after" = "after",
): void {
  if (!draft.layout) return
  const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
  if (!display) return

  if (targetLeafId) {
    display.root = splitLeafWithPanel(display.root, targetLeafId, type, direction, position)
  } else {
    display.root = addPanelAtRoot(display.root, type, direction)
  }
}

/**
 * Apply new percent sizes to a split node after the user drags a divider.
 */
export function resizeSplit(
  draft: CodeStudioData,
  activeDisplay: DisplayConfig,
  splitId: string,
  sizes: number[],
): void {
  if (!draft.layout) return
  const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
  if (!display) return
  setSplitSizes(display.root, splitId, sizes)
}

export function removePanel(
  draft: CodeStudioData,
  activeDisplay: DisplayConfig,
  panelId: string,
): void {
  if (!draft.layout) return
  const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
  if (!display) return
  const next = removeLeaf(display.root, panelId)
  if (next == null) {
    // Refuse to remove the last leaf — keep something renderable.
    return
  }
  display.root = next
}

export function toggleEditorInstance(
  draft: CodeStudioData,
  activeDisplay: DisplayConfig,
  panelId: string,
): void {
  if (!draft.layout) return
  const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
  if (!display) return
  toggleLeafEditorInstance(display.root, panelId)
}

/**
 * Move an existing leaf next to another leaf via drag-and-drop quadrant docking.
 */
export function movePanel(
  draft: CodeStudioData,
  activeDisplay: DisplayConfig,
  sourcePanelId: string,
  targetPanelId: string,
  position: "top" | "right" | "bottom" | "left",
): void {
  if (!draft.layout) return
  const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
  if (!display) return
  display.root = moveLeafRelativeTo(display.root, sourcePanelId, targetPanelId, position)
}
