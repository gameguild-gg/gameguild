/**
 * Project Modes System
 * 
 * Defines the three project modes and their node restrictions:
 * - free-page: No restrictions
 * - code-page: Optimized for code-studio nodes
 * - quiz-page: Optimized for quiz nodes
 */

export type ProjectMode = "free-page" | "code-page" | "quiz-page"

type NodeList = string | string[] | null

export interface NodeRestrictions {
  blocks?: Record<string, [NodeList, NodeList]>  // {b1: [bloqueados, liberados]}
  // [bloqueados, liberados] - primeiro define bloqueio, segundo define permissão
  // '*' bloqueia/libera todos, string específico, ou null para nenhum
  // ex: {b1: ['*', 'code-studio']} = bloqueia todos exceto code-studio no b1
}

export interface ProjectModeConfig {
  mode: ProjectMode
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
    blocks: {
      b1: [null, null],   // nenhum bloqueio, todos permitidos
    }
  },
  "code-page": {
    blocks: {
      b1: ['code-studio', null],      // bloqueia code-studio no b1
    },
  },
  "quiz-page": {
    blocks: {
      b1: ['quiz', null],        // bloqueia quiz no b1
    },
  }
}

/**
 * Mode configurations with descriptions
 */
export const PROJECT_MODES: Record<ProjectMode, Omit<ProjectModeConfig, 'mode'>> = {
  "free-page": {
    restrictions: NODE_RESTRICTIONS["free-page"],
    description: "Free mode - no restrictions"
  },
  "code-page": {
    restrictions: NODE_RESTRICTIONS["code-page"],
    description: "Code mode - optimized for code studio"
  },
  "quiz-page": {
    restrictions: NODE_RESTRICTIONS["quiz-page"],
    description: "Quiz mode - optimized for quiz nodes"
  }
}

/**
 * Check if a node type is allowed in the single block for a given mode
 * @param nodeType - The type of node to check
 * @param mode - The project mode
 * @param customRestrictions - Optional custom restrictions that override defaults
 */
export function isNodeAllowed(
  nodeType: string,
  blockId: string,
  mode: ProjectMode,
  customRestrictions?: NodeRestrictions
): boolean {
  // Use custom restrictions if provided, otherwise use mode defaults
  const restrictions = customRestrictions || NODE_RESTRICTIONS[mode]
  
  if (!restrictions) {
    return true
  }

  let blockRestrictions: [NodeList, NodeList] | undefined

  if (restrictions.blocks?.[blockId]) {
    blockRestrictions = restrictions.blocks[blockId]
  }
  
  if (!blockRestrictions && restrictions.blocks) {
    blockRestrictions = restrictions.blocks.b1
  }
  
  if (!blockRestrictions) {
    return true
  }

  const [blocked, allowed] = blockRestrictions
  
  const isInList = (list: NodeList, type: string): boolean => {
    if (list === null) return false
    if (list === '*') return true
    if (typeof list === 'string') return list === type
    return list.includes(type)
  }

  if (allowed === '*') {
    return true
  }
  
  if (allowed !== null && allowed !== '*') {
    return isInList(allowed, nodeType)
  }
  
  if (blocked !== null) {
    if (blocked === '*') {
      return false
    }
    return !isInList(blocked, nodeType)
  }

  return true
}

/**
 * Create custom restrictions for a specific block
 */
export function setBlockRestriction(
  currentRestrictions: NodeRestrictions | undefined,
  blockId: string,
  blocked: NodeList,
  allowed: NodeList
): NodeRestrictions {
  const restrictions = currentRestrictions || { blocks: {} }
  return {
    ...restrictions,
    blocks: {
      ...restrictions.blocks,
      [blockId]: [blocked, allowed]
    }
  }
}

/**
 * Remove restrictions for a specific block
 */
export function removeBlockRestriction(
  currentRestrictions: NodeRestrictions | undefined,
  blockId: string
): NodeRestrictions {
  if (!currentRestrictions?.blocks) return currentRestrictions || { blocks: {} }
  
  const { [blockId]: _, ...remainingBlocks } = currentRestrictions.blocks
  return {
    ...currentRestrictions,
    blocks: remainingBlocks
  }
}

/**
 * Get restrictions for a specific block
 */
export function getRestrictions(
  restrictions: NodeRestrictions | undefined,
  blockId?: string,
): [NodeList, NodeList] | undefined {
  if (!restrictions) return undefined
  
  if (blockId && restrictions.blocks?.[blockId]) {
    return restrictions.blocks[blockId]
  }
  
  return undefined
}

/**
 * Get a human-readable description of restrictions
 */
export function describeRestrictions(restriction: [NodeList, NodeList] | undefined): string {
  if (!restriction) return "No restrictions"
  
  const [blocked, allowed] = restriction
  
  if (allowed === '*') return "All nodes allowed"
  if (allowed) {
    const allowedList = Array.isArray(allowed) ? allowed.join(', ') : allowed
    return `Only ${allowedList} allowed`
  }
  if (blocked === '*') return "All nodes blocked"
  if (blocked) {
    const blockedList = Array.isArray(blocked) ? blocked.join(', ') : blocked
    return `${blockedList} blocked`
  }
  
  return "No restrictions"
}
