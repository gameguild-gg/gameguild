/**
 * Project Modes System
 * 
 * Defines the three project modes and their node restrictions:
 * - free-page: No restrictions, supports both single and multiple layouts
 * - code-page: Optimized for code-studio nodes on right/single panel
 * - quiz-page: Optimized for quiz nodes on right/single panel
 * 
 * Note: Layout type (single/multiple) is now automatically detected from project data structure.
 *       "type" field in ProjectData refers to project type (type1, type2, etc.), not layout.
 */

export type ProjectMode = "free-page" | "code-page" | "quiz-page"

type NodeList = string | string[] | null

export interface NodeRestrictions {
  blocks?: Record<string, [NodeList, NodeList]>  // {b1: [bloqueados, liberados], b2: [...], ...}
  panels?: Record<string, [NodeList, NodeList]>  // {panel-1: [bloqueados, liberados], panel-2: [...], ...}
  // [bloqueados, liberados] - primeiro define bloqueio, segundo define permissão
  // '*' bloqueia/libera todos, string específico, ou null para nenhum
  // ex: {b1: ['*', 'code-studio']} = bloqueia todos exceto code-studio no b1
  // ex: {panel-1: ['*', 'code-studio']} = bloqueia todos exceto code-studio em todos blocks do panel-1
}

export interface ProjectModeConfig {
  mode: ProjectMode
  restrictions: NodeRestrictions
  description: string
  suggestedLayout?: "single" | "multiple"  // sugestão de layout, mas não obrigatório
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
      // Outros blocos herdam a mesma regra por padrão
    }
  },
  "code-page": {
    blocks: {
      b1: ['code-studio', null],      // bloqueia code-studio no b1 (painel esquerdo/principal)
      b2: ['*', 'code-studio'],        // bloqueia todos exceto code-studio no b2
      // Outros blocos herdam regra do b2
    },
    panels: {
      // Painéis podem ter suas próprias restrições que se aplicam a todos os blocks dentro deles
      // Exemplo: 'panel-2': ['*', 'code-studio'] = apenas code-studio permitido em todos blocks do panel-2
    }
  },
  "quiz-page": {
    blocks: {
      b1: ['quiz', null],        // bloqueia quiz no b1 (painel esquerdo/principal)
      b2: ['*', 'quiz'],         // bloqueia todos exceto quiz no b2
      // Outros blocos herdam regra do b2
    },
    panels: {
      // Exemplo: 'panel-2': ['*', 'quiz'] = apenas quiz permitido em todos blocks do panel-2
    }
  }
}

/**
 * Mode configurations with descriptions
 */
export const PROJECT_MODES: Record<ProjectMode, Omit<ProjectModeConfig, 'mode'>> = {
  "free-page": {
    suggestedLayout: "single",
    restrictions: NODE_RESTRICTIONS["free-page"],
    description: "Free mode - no restrictions, choose single or multiple layout"
  },
  "code-page": {
    suggestedLayout: "multiple",
    restrictions: NODE_RESTRICTIONS["code-page"],
    description: "Code mode - optimized for code studio, works best with multiple layout"
  },
  "quiz-page": {
    suggestedLayout: "multiple",
    restrictions: NODE_RESTRICTIONS["quiz-page"],
    description: "Quiz mode - optimized for quiz nodes, works best with multiple layout"
  }
}

/**
 * Check if a node type is allowed in a specific block for a given mode
 * @param nodeType - The type of node to check
 * @param blockId - The block to check ("b1", "b2", etc.)
 * @param mode - The project mode
 * @param panelId - Optional panel ID to check panel-level restrictions
 * @param customRestrictions - Optional custom restrictions that override defaults
 */
export function isNodeAllowed(
  nodeType: string,
  blockId: string,
  mode: ProjectMode,
  panelId?: string,
  customRestrictions?: NodeRestrictions
): boolean {
  // Use custom restrictions if provided, otherwise use mode defaults
  const restrictions = customRestrictions || NODE_RESTRICTIONS[mode]
  
  if (!restrictions) {
    return true
  }

  // Priority: panel restrictions > block restrictions > defaults
  let blockRestrictions: [NodeList, NodeList] | undefined

  // 1. Check if there are panel-level restrictions
  if (panelId && restrictions.panels?.[panelId]) {
    blockRestrictions = restrictions.panels[panelId]
  }
  
  // 2. Check if there are block-specific restrictions (only if no panel restriction found)
  if (!blockRestrictions && restrictions.blocks?.[blockId]) {
    blockRestrictions = restrictions.blocks[blockId]
  }
  
  // 3. Try to use default block patterns
  if (!blockRestrictions && restrictions.blocks) {
    if (blockId === 'b1') {
      blockRestrictions = restrictions.blocks.b1
    } else {
      // Outros blocos herdam de b2, ou b1 se b2 não existir
      blockRestrictions = restrictions.blocks.b2 || restrictions.blocks.b1
    }
  }
  
  if (!blockRestrictions) {
    return true  // sem restrições
  }

  const [blocked, allowed] = blockRestrictions
  
  // Helper para verificar se nodeType está em uma lista
  const isInList = (list: NodeList, type: string): boolean => {
    if (list === null) return false
    if (list === '*') return true
    if (typeof list === 'string') return list === type
    return list.includes(type)
  }

  // Lógica de restrições:
  // 1. Se allowed='*', todos permitidos (ignora blocked)
  // 2. Se allowed é uma lista específica, APENAS esses são permitidos
  // 3. Se allowed=null, aplicar regras de blocked
  
  // Caso 1: Se allowed='*', todos permitidos
  if (allowed === '*') {
    return true
  }
  
  // Caso 2: Se há lista específica de allowed, APENAS esses são permitidos
  if (allowed !== null && allowed !== '*') {
    return isInList(allowed, nodeType)
  }
  
  // Caso 3: Se não há allowed específico, aplicar regras de blocked
  if (blocked !== null) {
    if (blocked === '*') {
      return false  // todos bloqueados quando não há allowed específico
    }
    return !isInList(blocked, nodeType)  // permitir se não está na lista de bloqueados
  }

  // Se não há nenhuma restrição, permitir
  return true
}

/**
 * Get suggested layout type for a mode (optional, não obrigatório)
 */
export function getSuggestedLayoutForMode(mode: ProjectMode): "single" | "multiple" {
  return PROJECT_MODES[mode].suggestedLayout || "single"
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
  const restrictions = currentRestrictions || { blocks: {}, panels: {} }
  return {
    ...restrictions,
    blocks: {
      ...restrictions.blocks,
      [blockId]: [blocked, allowed]
    }
  }
}

/**
 * Create custom restrictions for a specific panel
 */
export function setPanelRestriction(
  currentRestrictions: NodeRestrictions | undefined,
  panelId: string,
  blocked: NodeList,
  allowed: NodeList
): NodeRestrictions {
  const restrictions = currentRestrictions || { blocks: {}, panels: {} }
  return {
    ...restrictions,
    panels: {
      ...restrictions.panels,
      [panelId]: [blocked, allowed]
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
  if (!currentRestrictions?.blocks) return currentRestrictions || { blocks: {}, panels: {} }
  
  const { [blockId]: _, ...remainingBlocks } = currentRestrictions.blocks
  return {
    ...currentRestrictions,
    blocks: remainingBlocks
  }
}

/**
 * Remove restrictions for a specific panel
 */
export function removePanelRestriction(
  currentRestrictions: NodeRestrictions | undefined,
  panelId: string
): NodeRestrictions {
  if (!currentRestrictions?.panels) return currentRestrictions || { blocks: {}, panels: {} }
  
  const { [panelId]: _, ...remainingPanels } = currentRestrictions.panels
  return {
    ...currentRestrictions,
    panels: remainingPanels
  }
}

/**
 * Get restrictions for a specific block or panel
 */
export function getRestrictions(
  restrictions: NodeRestrictions | undefined,
  blockId?: string,
  panelId?: string
): [NodeList, NodeList] | undefined {
  if (!restrictions) return undefined
  
  // Panel restrictions have priority
  if (panelId && restrictions.panels?.[panelId]) {
    return restrictions.panels[panelId]
  }
  
  // Then block restrictions
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
