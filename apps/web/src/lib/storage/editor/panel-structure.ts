/**
 * Panel Structure System
 * 
 * Nova estrutura que suporta painéis sequenciais (verticais)
 * Cada painel pode ser single ou multiple layout
 */

import type { SerializedEditorState } from "lexical"

export type PanelLayoutType = "single" | "multiple"

export interface PanelData {
  id: string
  type: PanelLayoutType
  name?: string // Nome opcional do painel (ex: "Introdução", "Capítulo 1")
  order: number // Ordem do painel na sequência
  
  // Blocos do painel (b1 para single, b1+b2+b3... para multiple, etc)
  blocks: Record<string, SerializedEditorState | string>
}

export type PreviewMode = "continuous" | "slide"

export interface SequentialPanelStructure {
  version: "sequential-v1"
  panels: PanelData[]
}

/**
 * Detecta se os dados são do formato sequencial novo
 */
export function isSequentialStructure(data: string): boolean {
  try {
    const parsed = JSON.parse(data)
    return parsed.version === "sequential-v1" && Array.isArray(parsed.panels)
  } catch {
    return false
  }
}

/**
 * Converte formato para estrutura sequencial
 * Espera dados já no formato blocks: {b1, b2, b3...}
 */
export function migrateToSequentialStructure(data: string): SequentialPanelStructure {
  try {
    const parsed = JSON.parse(data)
    
    // Se já é formato sequencial, retorna
    if (isSequentialStructure(data)) {
      return parsed as SequentialPanelStructure
    }
    
    // Se é formato com blocos
    if (parsed.blocks && typeof parsed.blocks === 'object') {
      const blocks = parsed.blocks
      return {
        version: "sequential-v1",
        panels: [
          {
            id: generatePanelId(),
            type: Object.keys(blocks).length > 1 ? "multiple" : "single",
            order: 0,
            blocks: Object.entries(blocks).reduce((acc, [key, value]: [string, any]) => {
              acc[key] = typeof value === 'string' ? value : JSON.stringify(value)
              return acc
            }, {} as Record<string, string>)
          }
        ]
      }
    }
    
    // Formato single (estado direto) - assume b1
    return {
      version: "sequential-v1",
      panels: [
        {
          id: generatePanelId(),
          type: "single",
          order: 0,
          blocks: {
            b1: typeof parsed === 'string' ? parsed : JSON.stringify(parsed)
          }
        }
      ]
    }
  } catch (error) {
    console.error("Failed to migrate to sequential structure:", error)
    // Retorna estrutura vazia em caso de erro
    return {
      version: "sequential-v1",
      panels: []
    }
  }
}

/**
 * Cria uma nova estrutura sequencial vazia
 */
export function createEmptySequentialStructure(): SequentialPanelStructure {
  const emptyState = {
    root: {
      children: [
        {
          children: [],
          direction: null,
          format: "",
          indent: 0,
          type: "paragraph",
          version: 1
        }
      ],
      direction: null,
      format: "",
      indent: 0,
      type: "root",
      version: 1
    }
  }
  
  return {
    version: "sequential-v1",
    panels: [
      {
        id: generatePanelId(),
        type: "single",
        order: 0,
        name: "Panel 1",
        blocks: {
          b1: JSON.stringify(emptyState)
        }
      }
    ]
  }
}

/**
 * Adiciona um novo painel à estrutura
 */
export function addPanel(
  structure: SequentialPanelStructure, 
  type: PanelLayoutType,
  position?: number
): SequentialPanelStructure {
  const emptyState = {
    root: {
      children: [
        {
          children: [],
          direction: null,
          format: "",
          indent: 0,
          type: "paragraph",
          version: 1
        }
      ],
      direction: null,
      format: "",
      indent: 0,
      type: "root",
      version: 1
    }
  }
  
  const newPanel: PanelData = {
    id: generatePanelId(),
    type,
    order: position !== undefined ? position : structure.panels.length,
    name: `Panel ${structure.panels.length + 1}`,
    blocks: type === "single" ? {
      b1: JSON.stringify(emptyState)
    } : {
      b1: JSON.stringify(emptyState),
      b2: JSON.stringify(emptyState)
    }
  }
  
  const newPanels = [...structure.panels]
  
  if (position !== undefined) {
    // Inserir na posição específica
    newPanels.splice(position, 0, newPanel)
    // Reordenar todos os painéis
    newPanels.forEach((panel, index) => {
      panel.order = index
    })
  } else {
    // Adicionar no final
    newPanels.push(newPanel)
  }
  
  return {
    ...structure,
    panels: newPanels
  }
}

/**
 * Remove um painel da estrutura
 */
export function removePanel(
  structure: SequentialPanelStructure,
  panelId: string
): SequentialPanelStructure {
  const newPanels = structure.panels
    .filter(p => p.id !== panelId)
    .map((panel, index) => ({
      ...panel,
      order: index
    }))
  
  return {
    ...structure,
    panels: newPanels
  }
}

/**
 * Reordena painéis
 */
export function reorderPanels(
  structure: SequentialPanelStructure,
  fromIndex: number,
  toIndex: number
): SequentialPanelStructure {
  const newPanels = [...structure.panels]
  const [movedPanel] = newPanels.splice(fromIndex, 1)
  
  if (!movedPanel) {
    return structure // No panel to move
  }
  
  newPanels.splice(toIndex, 0, movedPanel)
  
  // Atualizar ordem
  newPanels.forEach((panel, index) => {
    panel.order = index
  })
  
  return {
    ...structure,
    panels: newPanels
  }
}

/**
 * Atualiza o nome de um painel
 */
export function updatePanelName(
  structure: SequentialPanelStructure,
  panelId: string,
  name: string
): SequentialPanelStructure {
  return {
    ...structure,
    panels: structure.panels.map(panel =>
      panel.id === panelId ? { ...panel, name } : panel
    )
  }
}

/**
 * Atualiza o estado de um painel
 */
export function updatePanelState(
  structure: SequentialPanelStructure,
  panelId: string,
  blocks: Record<string, SerializedEditorState | string>
): SequentialPanelStructure {
  return {
    ...structure,
    panels: structure.panels.map(panel =>
      panel.id === panelId ? { ...panel, blocks } : panel
    )
  }
}

/**
 * Gera um ID único para painel
 */
function generatePanelId(): string {
  return `panel_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
}

/**
 * Converte estrutura sequencial para string JSON
 */
export function serializeSequentialStructure(structure: SequentialPanelStructure): string {
  return JSON.stringify(structure)
}

/**
 * Parse estrutura sequencial de string JSON
 */
export function parseSequentialStructure(data: string): SequentialPanelStructure {
  return JSON.parse(data) as SequentialPanelStructure
}
