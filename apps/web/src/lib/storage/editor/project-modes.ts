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

export interface NodeRestrictions {
  left: string[] | null  // null = all allowed, array = blocked nodes
  right: string[] | null  // null = all allowed, ['*'] = all blocked except rightAllowed
  rightAllowed?: string[]  // nodes allowed on right when right = ['*']
  single?: string[] | null  // restrictions for single panel (type1), ['*'] = all blocked except singleAllowed
  singleAllowed?: string[]  // nodes allowed in single panel when single = ['*']
}

export interface ProjectModeConfig {
  mode: ProjectMode
  layoutType: ProjectLayoutType
  restrictions: NodeRestrictions
  description: string
}

/**
 * Node restrictions for each mode
 */
export const NODE_RESTRICTIONS: Record<ProjectMode, NodeRestrictions> = {
  "free-page": {
    left: null,  // todos permitidos
    right: null,  // todos permitidos
    single: null  // todos permitidos em single panel
  },
  "code-page": {
    left: ['code-studio'],  // bloqueados na esquerda
    right: ['*'],  // todos bloqueados exceto code-studio
    rightAllowed: ['code-studio'],
    single: ['*'],  // em single panel, só code-studio permitido
    singleAllowed: ['code-studio']
  },
  "quiz-page": {
    left: ['quiz'],  // bloqueados na esquerda
    right: ['*'],  // todos bloqueados exceto quiz
    rightAllowed: ['quiz'],
    single: ['*'],  // em single panel, só quiz permitido
    singleAllowed: ['quiz']
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
    return true  // no restrictions
  }

  // Handle single panel (type1 layouts)
  if (panel === "single") {
    const singleRestrictions = restrictions.single
    
    if (singleRestrictions === null) {
      return true  // no restrictions
    }
    
    if (singleRestrictions?.includes('*')) {
      const allowedNodes = restrictions.singleAllowed || []
      return allowedNodes.includes(nodeType)
    }
    
    return !singleRestrictions?.includes(nodeType)
  }

  // Handle dual panel (type2 layouts)
  const panelRestrictions = restrictions[panel]
  
  // No restrictions on this panel
  if (panelRestrictions === null) {
    return true
  }

  // Check if all nodes are blocked except specific ones
  if (panelRestrictions.includes('*')) {
    const allowedNodes = panel === "right" ? restrictions.rightAllowed || [] : []
    return allowedNodes.includes(nodeType)
  }

  // Check if this specific node is blocked
  return !panelRestrictions.includes(nodeType)
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
