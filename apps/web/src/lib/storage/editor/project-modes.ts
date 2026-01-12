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
    right: null  // todos permitidos
  },
  "code-page": {
    left: ['code-studio'],  // bloqueados na esquerda
    right: ['*'],  // todos bloqueados exceto code-studio
    rightAllowed: ['code-studio']
  },
  "quiz-page": {
    left: ['quiz'],  // bloqueados na esquerda
    right: ['*'],  // todos bloqueados exceto quiz
    rightAllowed: ['quiz']
  }
}

/**
 * Mode configurations with descriptions
 */
export const PROJECT_MODES: Record<ProjectMode, Omit<ProjectModeConfig, 'mode'>> = {
  "free-page": {
    layoutType: "type1",  // default, user can choose
    restrictions: NODE_RESTRICTIONS["free-page"],
    description: "Free mode - no restrictions, choose single or dual layout"
  },
  "code-page": {
    layoutType: "type2",  // always dual
    restrictions: NODE_RESTRICTIONS["code-page"],
    description: "Code mode - dual layout with code studio on right panel"
  },
  "quiz-page": {
    layoutType: "type2",  // always dual
    restrictions: NODE_RESTRICTIONS["quiz-page"],
    description: "Quiz mode - dual layout with quiz nodes on right panel"
  }
}

/**
 * Check if a node type is allowed in a specific panel for a given mode
 */
export function isNodeAllowed(
  nodeType: string,
  panel: "left" | "right",
  mode: ProjectMode
): boolean {
  const restrictions = NODE_RESTRICTIONS[mode]
  
  if (!restrictions) {
    return true  // no restrictions
  }

  const panelRestrictions = restrictions[panel]
  
  // No restrictions on this panel
  if (panelRestrictions === null) {
    return true
  }

  // Check if all nodes are blocked except specific ones
  if (panelRestrictions.includes('*')) {
    const allowedNodes = restrictions.rightAllowed || []
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
 */
export function canSelectLayoutType(mode: ProjectMode): boolean {
  return mode === "free-page"
}
