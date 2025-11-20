import type { CodeStudioData, DisplayConfig, PanelType, PanelConfig, EditorInstance } from "./types"
import { getGridDimensions } from "./grid-utils"

export function addPanel(
  data: CodeStudioData,
  activeDisplay: DisplayConfig,
  type: PanelType,
  row?: number,
  col?: number
): Partial<CodeStudioData> {
  if (!data.layout) return {}
  
  const { cols, rows } = getGridDimensions(activeDisplay.aspectRatio)
  
  // Se tiver coordenadas de drag-drop, usar elas
  // Senão, encontrar primeira célula vazia no grid
  let targetRow = row ?? 0
  let targetCol = col ?? 0
  let found = row !== undefined && col !== undefined

  if (!found) {
    // Buscar primeira célula vazia
    for (let r = 0; r < rows && !found; r++) {
      for (let c = 0; c < cols && !found; c++) {
        const occupied = activeDisplay.panels.some(p =>
          r >= p.row && r < p.row + p.rowSpan &&
          c >= p.col && c < p.col + p.colSpan
        )
        if (!occupied) {
          targetRow = r
          targetCol = c
          found = true
        }
      }
    }
  }

  // Garantir que o painel não saia do grid (tamanho padrão 4 linhas x metade das colunas)
  const defaultColSpan = Math.min(8, Math.floor(cols / 2))
  const rowSpan = Math.min(4, rows - targetRow)
  const colSpan = Math.min(defaultColSpan, cols - targetCol)

  const newPanel: PanelConfig = {
    id: `${type}-${Date.now()}`,
    type,
    row: targetRow,
    col: targetCol,
    rowSpan,
    colSpan,
    ...(type === "editor" && { editorInstance: "multiple" as EditorInstance }),
  }

  const updatedDisplays = data.layout.displays.map(d =>
    d.id === activeDisplay.id
      ? { ...d, panels: [...d.panels, newPanel] }
      : d
  )

  return {
    layout: {
      ...data.layout,
      displays: updatedDisplays,
    },
  }
}

export function resizePanel(
  data: CodeStudioData,
  activeDisplay: DisplayConfig,
  panelId: string,
  row: number,
  col: number,
  rowSpan: number,
  colSpan: number
): Partial<CodeStudioData> {
  if (!data.layout) return {}
  
  const updatedPanels = activeDisplay.panels.map(p =>
    p.id === panelId ? { ...p, row, col, rowSpan, colSpan } : p
  )
  
  const updatedDisplays = data.layout.displays.map(d =>
    d.id === activeDisplay.id
      ? { ...d, panels: updatedPanels }
      : d
  )

  return {
    layout: {
      ...data.layout,
      displays: updatedDisplays,
    },
  }
}

export function movePanel(
  data: CodeStudioData,
  activeDisplay: DisplayConfig,
  panelId: string,
  row: number,
  col: number
): Partial<CodeStudioData> {
  if (!data.layout) return {}
  
  const updatedPanels = activeDisplay.panels.map(p =>
    p.id === panelId ? { ...p, row, col } : p
  )
  
  const updatedDisplays = data.layout.displays.map(d =>
    d.id === activeDisplay.id
      ? { ...d, panels: updatedPanels }
      : d
  )

  return {
    layout: {
      ...data.layout,
      displays: updatedDisplays,
    },
  }
}

export function removePanel(
  data: CodeStudioData,
  activeDisplay: DisplayConfig,
  panelId: string
): Partial<CodeStudioData> {
  if (!data.layout) return {}
  
  const updatedPanels = activeDisplay.panels.filter(p => p.id !== panelId)
  
  const updatedDisplays = data.layout.displays.map(d =>
    d.id === activeDisplay.id
      ? { ...d, panels: updatedPanels }
      : d
  )

  return {
    layout: {
      ...data.layout,
      displays: updatedDisplays,
    },
  }
}

export function toggleEditorInstance(
  data: CodeStudioData,
  activeDisplay: DisplayConfig,
  panelId: string
): Partial<CodeStudioData> {
  if (!data.layout) return {}

  const updatedPanels = activeDisplay.panels.map(p => {
    if (p.id === panelId && p.type === "editor") {
      return {
        ...p,
        editorInstance: (p.editorInstance === "multiple" ? "unique" : "multiple") as EditorInstance,
      }
    }
    return p
  })

  const updatedDisplays = data.layout.displays.map(d =>
    d.id === activeDisplay.id
      ? { ...d, panels: updatedPanels }
      : d
  )

  return {
    layout: {
      ...data.layout,
      displays: updatedDisplays,
    },
  }
}

// Funções para drag start/end - apenas para logging/feedback visual
export function onPanelDragStart(panelId: string): void {
  console.log('Dragging panel:', panelId)
}

export function onPanelDragEnd(): void {
  console.log('Drag ended')
}
