import { produce } from "immer"
import type { CodeStudioData, DisplayConfig, PanelType, PanelConfig, EditorInstance } from "./types"
import { getGridDimensions } from "./grid-utils"

export function addPanel(
  data: CodeStudioData,
  activeDisplay: DisplayConfig,
  type: PanelType,
  row?: number,
  col?: number
): Partial<CodeStudioData> {
  return produce(data, draft => {
    if (!draft.layout) return
    
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

    const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
    if (display) {
      display.panels.push(newPanel)
    }
  })
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
  return produce(data, draft => {
    if (!draft.layout) return
    
    const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
    if (!display) return
    
    const panel = display.panels.find(p => p.id === panelId)
    if (panel) {
      panel.row = row
      panel.col = col
      panel.rowSpan = rowSpan
      panel.colSpan = colSpan
    }
  })
}

export function movePanel(
  data: CodeStudioData,
  activeDisplay: DisplayConfig,
  panelId: string,
  row: number,
  col: number
): Partial<CodeStudioData> {
  return produce(data, draft => {
    if (!draft.layout) return
    
    const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
    if (!display) return
    
    const panel = display.panels.find(p => p.id === panelId)
    if (panel) {
      panel.row = row
      panel.col = col
    }
  })
}

export function removePanel(
  data: CodeStudioData,
  activeDisplay: DisplayConfig,
  panelId: string
): Partial<CodeStudioData> {
  return produce(data, draft => {
    if (!draft.layout) return
    
    const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
    if (display) {
      display.panels = display.panels.filter(p => p.id !== panelId)
    }
  })
}

export function toggleEditorInstance(
  data: CodeStudioData,
  activeDisplay: DisplayConfig,
  panelId: string
): Partial<CodeStudioData> {
  return produce(data, draft => {
    if (!draft.layout) return

    const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
    if (!display) return
    
    const panel = display.panels.find(p => p.id === panelId)
    if (panel && panel.type === "editor") {
      panel.editorInstance = (panel.editorInstance === "multiple" ? "unique" : "multiple") as EditorInstance
    }
  })
}

// Funções para drag start/end - apenas para logging/feedback visual
export function onPanelDragStart(panelId: string): void {
  console.log('Dragging panel:', panelId)
}

export function onPanelDragEnd(): void {
  console.log('Drag ended')
}
