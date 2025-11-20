import type { CodeStudioData, DisplayConfig, AspectRatio } from "./types"
import { getGridDimensions } from "./grid-utils"

export function toggleLayoutEdit(data: CodeStudioData): Partial<CodeStudioData> {
  if (!data.layout) return {}
  
  return {
    layout: {
      ...data.layout,
      editMode: !data.layout.editMode,
    },
  }
}

export function selectDisplay(data: CodeStudioData, displayId: string): Partial<CodeStudioData> {
  if (!data.layout) return {}
  
  return {
    layout: {
      ...data.layout,
      activeDisplayId: displayId,
    },
  }
}

export function createDisplay(
  data: CodeStudioData,
  name: string,
  aspectRatio: AspectRatio
): Partial<CodeStudioData> {
  if (!data.layout || data.layout.displays.length >= 4) return {}
  
  const { cols, rows } = getGridDimensions(aspectRatio)
  const displayNumber = data.layout.displays.length + 1
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

  return {
    layout: {
      ...data.layout,
      displays: [...data.layout.displays, newDisplay],
      activeDisplayId: newDisplay.id,
    },
  }
}

export function deleteDisplay(data: CodeStudioData, displayId: string): Partial<CodeStudioData> {
  if (!data.layout || data.layout.displays.length <= 2) return {}
  
  const updatedDisplays = data.layout.displays.filter(d => d.id !== displayId)
  const newActiveId = data.layout.activeDisplayId === displayId 
    ? updatedDisplays[0]?.id || ""
    : data.layout.activeDisplayId

  return {
    layout: {
      ...data.layout,
      displays: updatedDisplays,
      activeDisplayId: newActiveId,
    },
  }
}

export function renameDisplay(
  data: CodeStudioData,
  displayId: string,
  newName: string
): Partial<CodeStudioData> {
  if (!data.layout) return {}
  
  const updatedDisplays = data.layout.displays.map(d =>
    d.id === displayId ? { ...d, name: newName } : d
  )

  return {
    layout: {
      ...data.layout,
      displays: updatedDisplays,
    },
  }
}

export function changeAspectRatio(
  data: CodeStudioData,
  displayId: string,
  newAspectRatio: AspectRatio
): Partial<CodeStudioData> {
  if (!data.layout) return {}
  
  const display = data.layout.displays.find(d => d.id === displayId)
  if (!display) return {}

  const oldDimensions = getGridDimensions(display.aspectRatio)
  const newDimensions = getGridDimensions(newAspectRatio)

  // Calcular fatores de escala
  const colScale = newDimensions.cols / oldDimensions.cols
  const rowScale = newDimensions.rows / oldDimensions.rows

  // Reescalar todos os painéis
  const rescaledPanels = display.panels.map(panel => ({
    ...panel,
    col: Math.floor(panel.col * colScale),
    row: Math.floor(panel.row * rowScale),
    colSpan: Math.max(1, Math.min(Math.round(panel.colSpan * colScale), newDimensions.cols - Math.floor(panel.col * colScale))),
    rowSpan: Math.max(1, Math.min(Math.round(panel.rowSpan * rowScale), newDimensions.rows - Math.floor(panel.row * rowScale))),
  }))

  const updatedDisplays = data.layout.displays.map(d =>
    d.id === displayId ? { ...d, aspectRatio: newAspectRatio, panels: rescaledPanels } : d
  )

  return {
    layout: {
      ...data.layout,
      displays: updatedDisplays,
    },
  }
}

export function updateCurrentDisplay(
  data: CodeStudioData,
  updatedDisplay: DisplayConfig
): Partial<CodeStudioData> {
  if (!data.layout) return {}
  
  const updatedDisplays = data.layout.displays.map(d =>
    d.id === updatedDisplay.id ? updatedDisplay : d
  )

  return {
    layout: {
      ...data.layout,
      displays: updatedDisplays,
    },
  }
}
