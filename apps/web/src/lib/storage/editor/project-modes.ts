/**
 * Project Modes System
 * 
 * Defines the three project modes and their node restrictions:
 * - free-page: No restrictions, supports both type1 and type2 layouts
 * - code-page: Type2 layout with code-studio nodes only on right panel
 * - quiz-page: Type2 layout with quiz nodes only on right panel
 */

export type ProjectMode = "free-page" | "code-page" | "quiz-page"
export type ProjectLayoutType = "type1" | "type2"

type NodeList = string | string[] | null

export interface NodeRestrictions {
  left?: [NodeList, NodeList]   // [bloqueados, liberados] - primeiro define bloqueio, segundo define permissão
  right?: [NodeList, NodeList]  // '*' bloqueia/libera todos, string específico, ou null para nenhum
  single?: [NodeList, NodeList] // ex: ['*', 'code-studio'] = bloqueia todos exceto code-studio
}

export interface ProjectModeConfig {
  mode: ProjectMode
  layoutType: ProjectLayoutType
  restrictions: NodeRestrictions
  description: string
}

/**
 * Node restrictions for each mode
 * Formato: [bloqueados, liberados]
 * - '*' na primeira posição = bloqueia todos
 * - '*' na segunda posição = libera todos
 * - null = nenhum bloqueio/liberação específica
 */
export const NODE_RESTRICTIONS: Record<ProjectMode, NodeRestrictions> = {
  "free-page": {
    left: [null, null],   // nenhum bloqueio, todos permitidos
    right: [null, null],  // nenhum bloqueio, todos permitidos
    single: [null, null]  // nenhum bloqueio, todos permitidos
  },
  "code-page": {
    left: ['code-studio', null],      // bloqueia code-studio na esquerda
    right: ['*', 'code-studio'],      // bloqueia todos exceto code-studio
    single: ['*', 'code-studio']      // bloqueia todos exceto code-studio
  },
  "quiz-page": {
    left: ['quiz', null],        // bloqueia quiz na esquerda
    right: ['*', 'quiz'],        // bloqueia todos exceto quiz
    single: ['*', 'quiz']        // bloqueia todos exceto quiz
  }
}

/**
 * Mode configurations with descriptions
 */
export const PROJECT_MODES: Record<ProjectMode, Omit<ProjectModeConfig, 'mode'>> = {
  "free-page": {
    layoutType: "type1",  // default suggestion
    restrictions: NODE_RESTRICTIONS["free-page"],
    description: "Free mode - no restrictions, choose single or dual layout"
  },
  "code-page": {
    layoutType: "type2",  // default suggestion (works best with dual)
    restrictions: NODE_RESTRICTIONS["code-page"],
    description: "Code mode - optimized for code studio, works with both layouts"
  },
  "quiz-page": {
    layoutType: "type2",  // default suggestion (works best with dual)
    restrictions: NODE_RESTRICTIONS["quiz-page"],
    description: "Quiz mode - optimized for quiz nodes, works with both layouts"
  }
}

/**
 * Check if a node type is allowed in a specific panel for a given mode
 * @param nodeType - The type of node to check
 * @param panel - The panel to check ("left", "right", or "single" for type1 layouts)
 * @param mode - The project mode
 */
export function isNodeAllowed(
  nodeType: string,
  panel: "left" | "right" | "single",
  mode: ProjectMode
): boolean {
  const restrictions = NODE_RESTRICTIONS[mode]
  
  if (!restrictions) {
    return true
  }

  const panelRestrictions = restrictions[panel]
  
  if (!panelRestrictions) {
    return true  // sem restrições neste painel
  }

  const [blocked, allowed] = panelRestrictions
  
  // Helper para verificar se nodeType está em uma lista
  const isInList = (list: NodeList, type: string): boolean => {
    if (list === null) return false
    if (list === '*') return true
    if (typeof list === 'string') return list === type
    return list.includes(type)
  }

  // Primeiro verifica se está explicitamente permitido
  if (allowed !== null) {
    if (isInList(allowed, nodeType)) {
      return true  // explicitamente permitido
    }
    // Se há lista de permitidos mas node não está nela, verificar bloqueios
    if (allowed === '*') {
      return true  // todos permitidos
    }
  }

  // Verifica se está bloqueado
  if (blocked !== null) {
    if (isInList(blocked, nodeType)) {
      return false  // explicitamente bloqueado
    }
    if (blocked === '*' && allowed !== '*') {
      return false  // todos bloqueados e não está em allowed
    }
  }

  // Se não há restrições ou não está bloqueado, permitir
  return true
}

/**
 * Get default layout type for a mode
 */
export function getDefaultLayoutForMode(mode: ProjectMode): ProjectLayoutType {
  return PROJECT_MODES[mode].layoutType
}

/**
 * Check if mode supports layout type selection
 * All modes now support both type1 and type2
 */
export function canSelectLayoutType(mode: ProjectMode): boolean {
  return true  // All modes support layout selection
}
