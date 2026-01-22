/**
 * Layout Detection System
 * 
 * Detecta automaticamente o tipo de layout baseado na estrutura dos dados do projeto.
 * - Single Panel: dados com estrutura {blocks: {b1}}
 * - Multi Panel: dados com estrutura {blocks: {b1, b2, b3...}}
 * - Sequential: array de painéis (estrutura v1 com panels[])
 */

import { isSequentialStructure, parseSequentialStructure, type SequentialPanelStructure } from './panel-structure'
import { getLayoutFromType, type ProjectType, type InternalLayout } from './project-types'

export interface LayoutDetectionResult {
  layoutType: InternalLayout
  isSinglePanel: boolean
  isMultiPanel: boolean
  isSequential: boolean
  sequentialData?: SequentialPanelStructure
  blockCount?: number // Number of blocks (b1, b2, b3...)
  blocks?: string[] // Block identifiers: ["b1", "b2", "b3"...]
}

export interface EditorStates {
  blocks: Record<string, any> // {b1: state, b2: state, b3: state...} - single usa apenas b1
}

/**
 * Analisa os dados do projeto e determina qual layout usar
 * @param data - String JSON com os dados do projeto
 * @returns Informações sobre o layout detectado
 */
export function detectProjectLayout(data: string): LayoutDetectionResult {
  try {
    // Check if it's sequential structure first
    if (isSequentialStructure(data)) {
      const sequentialData = parseSequentialStructure(data)
      return {
        layoutType: "sequential",
        isSinglePanel: false,
        isMultiPanel: false,
        isSequential: true,
        sequentialData,
      }
    }
    
    const parsed = JSON.parse(data)
    
    // Check for block structure (b1, b2, b3...)
    const blockKeys = Object.keys(parsed).filter(key => /^b\d+$/.test(key))
    if (blockKeys.length >= 2) {
      return {
        layoutType: "multiple",
        isSinglePanel: false,
        isMultiPanel: true,
        isSequential: false,
        blockCount: blockKeys.length,
        blocks: blockKeys.sort((a, b) => {
          const numA = parseInt(a.slice(1))
          const numB = parseInt(b.slice(1))
          return numA - numB
        }),
      }
    }
    
    // Single panel: anything else (uses b1)
    return {
      layoutType: "single",
      isSinglePanel: true,
      isMultiPanel: false,
      isSequential: false,
      blockCount: 1,
      blocks: ["b1"],
    }
  } catch (error) {
    console.error("Failed to parse project data for layout detection:", error)
    return {
      layoutType: "single",
      isSinglePanel: true,
      isMultiPanel: false,
      isSequential: false,
    }
  }
}

/**
 * Extrai os estados dos editores baseado no tipo de projeto
 * @param data - String JSON com os dados do projeto
 * @param projectType - Tipo de projeto (type1, type2, type3)
 * @returns Objetos com os estados dos editores
 */
export function extractEditorStates(data: string, projectType: ProjectType): EditorStates {
  const layoutType = getLayoutFromType(projectType)
  try {
    const parsed = JSON.parse(data)
    
    if (layoutType === "multiple") {
      // Extract block structure (b1, b2, b3...)
      const blockKeys = Object.keys(parsed).filter(key => /^b\d+$/.test(key))
      
      if (blockKeys.length >= 1) {
        const blocks: Record<string, any> = {}
        blockKeys.forEach(key => {
          blocks[key] = typeof parsed[key] === 'string' ? JSON.parse(parsed[key]) : parsed[key]
        })
        
        return {
          blocks,
        }
      }
      
      // No valid blocks found
      return {
        blocks: {},
      }
    } else {
      // Single panel: dados diretos em b1
      return {
        blocks: {
          b1: parsed,
        },
      }
    }
  } catch (error) {
    console.error("Failed to extract editor states:", error)
    return {
      blocks: {},
    }
  }
}

/**
 * Cria a estrutura de dados correta baseado no tipo de projeto
 * @param projectType - Tipo de projeto
 * @param states - Estados dos editores (ou estrutura sequencial)
 * @returns String JSON formatada corretamente
 */
export function createProjectData(projectType: ProjectType, states: Partial<EditorStates> | SequentialPanelStructure, blockCount?: number): string {
  const layoutType = getLayoutFromType(projectType)
  // Se for estrutura sequencial completa, apenas serializar
  if ('version' in states && 'panels' in states) {
    return JSON.stringify(states)
  }
  
  if (layoutType === "multiple") {
    const editorStates = states as EditorStates
    
    // If blocks are provided, use them
    if (editorStates.blocks && Object.keys(editorStates.blocks).length > 0) {
      return JSON.stringify(editorStates.blocks)
    }
    
    // Create new block structure with specified count (default 2)
    const count = blockCount || 2
    const blocks: Record<string, any> = {}
    
    for (let i = 1; i <= count; i++) {
      blocks[`b${i}`] = createEmptyEditorState()
    }
    
    return JSON.stringify(blocks)
  } else {
    // Single panel: usa b1
    const editorStates = states as EditorStates
    const b1State = editorStates.blocks?.b1 || createEmptyEditorState()
    return JSON.stringify(b1State)
  }
}

/**
 * Cria um estado vazio no formato cells (basilar)
 */
function createEmptyEditorState() {
  return {
    cells: []
  }
}
