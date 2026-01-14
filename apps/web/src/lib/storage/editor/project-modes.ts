/**
 * Project Modes System
 * 
 * Defines the three project modes and their node restrictions:
 * - free-page: No restrictions, supports both single and dual layouts
 * - code-page: Optimized for code-studio nodes on right/single panel
 * - quiz-page: Optimized for quiz nodes on right/single panel
 * 
 * Note: Layout type (single/dual) is now automatically detected from project data structure.
 *       "type" field in ProjectData refers to project type (type1, type2, etc.), not layout.
 */

export type ProjectMode = "free-page" | "code-page" | "quiz-page"

type NodeList = string | string[] | null

export interface NodeRestrictions {
  left?: [NodeList, NodeList]   // [bloqueados, liberados] - primeiro define bloqueio, segundo define permissão
  right?: [NodeList, NodeList]  // '*' bloqueia/libera todos, string específico, ou null para nenhum
  single?: [NodeList, NodeList] // ex: ['*', 'code-studio'] = bloqueia todos exceto code-studio
}

export interface ProjectModeConfig {
  mode: ProjectMode
  restrictions: NodeRestrictions
  description: string
  suggestedLayout?: "single" | "dual"  // sugestão de layout, mas não obrigatório
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
    suggestedLayout: "single",
    restrictions: NODE_RESTRICTIONS["free-page"],
    description: "Free mode - no restrictions, choose single or dual layout"
  },
  "code-page": {
    suggestedLayout: "dual",
    restrictions: NODE_RESTRICTIONS["code-page"],
    description: "Code mode - optimized for code studio, works best with dual layout"
  },
  "quiz-page": {
    suggestedLayout: "dual",
    restrictions: NODE_RESTRICTIONS["quiz-page"],
    description: "Quiz mode - optimized for quiz nodes, works best with dual layout"
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
 * Get suggested layout type for a mode (optional, não obrigatório)
 */
export function getSuggestedLayoutForMode(mode: ProjectMode): "single" | "dual" {
  return PROJECT_MODES[mode].suggestedLayout || "single"
}
