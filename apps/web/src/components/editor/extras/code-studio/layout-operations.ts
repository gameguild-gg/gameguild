import type { CodeStudioData, DisplayConfig, AspectRatio } from "./types"
import { getGridDimensions } from "./grid-utils"

export function toggleLayoutEdit(draft: CodeStudioData): void {
  if (!draft.layout) return
  draft.layout.editMode = !draft.layout.editMode
}

export function selectDisplay(draft: CodeStudioData, displayId: string): void {
  if (!draft.layout) return
  draft.layout.activeDisplayId = displayId
}

export function createDisplay(
  draft: CodeStudioData,
  name: string,
  aspectRatio: AspectRatio
): void {
  if (!draft.layout || draft.layout.displays.length >= 4) return
  
  const { cols, rows } = getGridDimensions(aspectRatio)
  const displayNumber = draft.layout.displays.length + 1
  const newDisplay: DisplayConfig = {
    id: `display-${displayNumber}`,
    name: name || `Display ${displayNumber}`,
    aspectRatio,
    panels: [
      { 
        id: `editor-${Date.now()}`, 
        type: "editor", 
        row: 0, 
        col: 0, 
        rowSpan: rows, 
        colSpan: cols, 
        editorInstance: "multiple" 
      },
    ],
  }

  draft.layout.displays.push(newDisplay)
  draft.layout.activeDisplayId = newDisplay.id
}

export function deleteDisplay(draft: CodeStudioData, displayId: string): void {
  if (!draft.layout || draft.layout.displays.length <= 2) return
  
  draft.layout.displays = draft.layout.displays.filter(d => d.id !== displayId)
  
  if (draft.layout.activeDisplayId === displayId) {
    draft.layout.activeDisplayId = draft.layout.displays[0]?.id || ""
  }
}

export function renameDisplay(
  draft: CodeStudioData,
  displayId: string,
  newName: string
): void {
  if (!draft.layout) return
  
  const display = draft.layout.displays.find(d => d.id === displayId)
  if (display) {
    display.name = newName
  }
}

export function changeAspectRatio(
  draft: CodeStudioData,
  displayId: string,
  newAspectRatio: AspectRatio
): void {
  if (!draft.layout) return
  
  const display = draft.layout.displays.find(d => d.id === displayId)
  if (!display) return

  const oldDimensions = getGridDimensions(display.aspectRatio)
  const newDimensions = getGridDimensions(newAspectRatio)

  // Calcular fatores de escala
  const colScale = newDimensions.cols / oldDimensions.cols
  const rowScale = newDimensions.rows / oldDimensions.rows

  // Reescalar todos os painéis
  display.panels.forEach(panel => {
    panel.col = Math.floor(panel.col * colScale)
    panel.row = Math.floor(panel.row * rowScale)
    panel.colSpan = Math.max(1, Math.min(Math.round(panel.colSpan * colScale), newDimensions.cols - Math.floor(panel.col * colScale)))
    panel.rowSpan = Math.max(1, Math.min(Math.round(panel.rowSpan * rowScale), newDimensions.rows - Math.floor(panel.row * rowScale)))
  })

  display.aspectRatio = newAspectRatio
}

export function updateCurrentDisplay(
  draft: CodeStudioData,
  updatedDisplay: DisplayConfig
): void {
  if (!draft.layout) return
  
  const display = draft.layout.displays.find(d => d.id === updatedDisplay.id)
  if (display) {
    Object.assign(display, updatedDisplay)
  }
}
