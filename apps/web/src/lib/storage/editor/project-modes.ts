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
  blocks: Record<string, [NodeList, NodeList]>  // {b1: [bloqueados, liberados], b2: [...], ...}
  // [bloqueados, liberados] - primeiro define bloqueio, segundo define permissão
  // '*' bloqueia/libera todos, string específico, ou null para nenhum
  // ex: {b1: ['*', 'code-studio']} = bloqueia todos exceto code-studio no b1
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
    }
  },
  "quiz-page": {
    blocks: {
      b1: ['quiz', null],        // bloqueia quiz no b1 (painel esquerdo/principal)
      b2: ['*', 'quiz'],         // bloqueia todos exceto quiz no b2
      // Outros blocos herdam regra do b2
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
 */
export function isNodeAllowed(
  nodeType: string,
  blockId: string,
  mode: ProjectMode
): boolean {
  const restrictions = NODE_RESTRICTIONS[mode]
  
  if (!restrictions || !restrictions.blocks) {
    return true
  }

  // Buscar restrições específicas do bloco, ou usar padrão
  let blockRestrictions = restrictions.blocks[blockId]
  
  // Se não tem restrição específica, tentar usar b1 como padrão para single
  // ou b2 como padrão para outros blocos
  if (!blockRestrictions) {
    if (blockId === 'b1') {
      blockRestrictions = restrictions.blocks.b1 || [null, null]
    } else {
      // Outros blocos herdam de b2, ou b1 se b2 não existir
      blockRestrictions = restrictions.blocks.b2 || restrictions.blocks.b1 || [null, null]
    }
  }
  
  if (!blockRestrictions) {
    return true  // sem restrições neste bloco
  }

  const [blocked, allowed] = blockRestrictions
  
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
export function getSuggestedLayoutForMode(mode: ProjectMode): "single" | "multiple" {
  return PROJECT_MODES[mode].suggestedLayout || "single"
}
