import type { CodeStudioData, DisplayConfig } from "./types"
import { buildTemplateTree } from "./templates/templates"

export function toggleLayoutEdit(draft: CodeStudioData): void {
  if (!draft.layout) return
  draft.layout.editMode = !draft.layout.editMode
}

export function selectDisplay(draft: CodeStudioData, displayId: string): void {
  if (!draft.layout) return
  draft.layout.activeDisplayId = displayId
}

/**
 * Append a new display seeded from a layout template.
 */
export function createDisplay(
  draft: CodeStudioData,
  name: string,
  templateId: string,
): void {
  if (!draft.layout) return

  const displayNumber = draft.layout.displays.length + 1
  const id = `display-${Date.now()}-${displayNumber}`
  const newDisplay: DisplayConfig = {
    id,
    name: name || `Display ${displayNumber}`,
    templateId,
    root: buildTemplateTree(templateId),
  }

  draft.layout.displays.push(newDisplay)
  draft.layout.activeDisplayId = newDisplay.id
}

/**
 * Replace the splitter tree of an existing display with a fresh template tree.
 * Any unique-editor tab state is cleared since the panel ids change.
 */
export function applyTemplateToDisplay(
  draft: CodeStudioData,
  displayId: string,
  templateId: string,
): void {
  if (!draft.layout) return
  const display = draft.layout.displays.find(d => d.id === displayId)
  if (!display) return
  display.templateId = templateId
  display.root = buildTemplateTree(templateId)
  display.uniqueOpenTabs = undefined
  display.uniqueActiveFileId = undefined
}

export function deleteDisplay(draft: CodeStudioData, displayId: string): void {
  if (!draft.layout || draft.layout.displays.length <= 1) return

  draft.layout.displays = draft.layout.displays.filter(d => d.id !== displayId)

  if (draft.layout.activeDisplayId === displayId) {
    // Select the most recently created remaining display (displays array is
    // append-only, so the last entry is the newest).
    const newest = draft.layout.displays[draft.layout.displays.length - 1]
    draft.layout.activeDisplayId = newest?.id || ""
  }
}

export function renameDisplay(
  draft: CodeStudioData,
  displayId: string,
  newName: string,
): void {
  if (!draft.layout) return
  const display = draft.layout.displays.find(d => d.id === displayId)
  if (display) display.name = newName
}

export function updateCurrentDisplay(
  draft: CodeStudioData,
  updatedDisplay: DisplayConfig,
): void {
  if (!draft.layout) return
  const display = draft.layout.displays.find(d => d.id === updatedDisplay.id)
  if (display) Object.assign(display, updatedDisplay)
}
