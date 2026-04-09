/**
 * Project Types System
 * 
 * Centraliza configuração de todos os tipos de projetos.
 * Extensível - adicionar novos tipos é simples, basta adicionar em PROJECT_TYPES
 */

// ============================================================================
// Engine Types
// ============================================================================

/**
 * Engine types available for projects.
 * - "lexical": Rich-text Lexical editor with decorator nodes embedded in text.
 * - "blocks": Simple array of decorator blocks (no text between blocks).
 */
export const ENGINE_TYPES = {
  LEXICAL: 'lexical',
  BLOCKS: 'blocks',
} as const

export type EngineType = typeof ENGINE_TYPES[keyof typeof ENGINE_TYPES]

// ============================================================================
// Project Types
// ============================================================================

/**
 * Tipos de projeto disponíveis
 * Usar este objeto para referenciar tipos em vez de strings hardcoded
 */
export const PROJECT_TYPES = {
  TYPE1: 'type1',
  TYPE2: 'type2',
  TYPE3: 'type3',
} as const

/**
 * Type derivado automaticamente dos valores de PROJECT_TYPES
 * Automaticamente inclui novos tipos quando adicionados
 */
export type ProjectType = typeof PROJECT_TYPES[keyof typeof PROJECT_TYPES]

/**
 * Layout interno usado pelos componentes
 * Derivado automaticamente do tipo de projeto
 */
export type InternalLayout = "single" | "multiple" | "slideshow"

/**
 * Configuração de cada tipo de projeto
 */
export interface ProjectTypeConfig {
  label: string
  description: string
  layout: InternalLayout
  minBlocks: number
  maxBlocks: number
  allowsDynamicBlocks: boolean
}

/**
 * Configurações de todos os tipos de projeto
 * Para adicionar um novo tipo: adicione em PROJECT_TYPES e aqui
 */
export const PROJECT_TYPE_CONFIG: Record<ProjectType, ProjectTypeConfig> = {
  [PROJECT_TYPES.TYPE1]: {
    label: 'Simple',
    description: 'One vertical editor for simple documents',
    layout: 'single',
    minBlocks: 1,
    maxBlocks: 1,
    allowsDynamicBlocks: false,
  },
  [PROJECT_TYPES.TYPE2]: {
    label: 'Multi-Panel',
    description: 'Multiple panels (1 or more)',
    layout: 'multiple',
    minBlocks: 1,
    maxBlocks: Infinity,
    allowsDynamicBlocks: true,
  },
  [PROJECT_TYPES.TYPE3]: {
    label: 'Slideshow',
    description: 'Multiple slides in sequence for presentations',
    layout: 'slideshow',
    minBlocks: 1,
    maxBlocks: Infinity,
    allowsDynamicBlocks: true,
  },
} as const

/**
 * Obtém configuração de um tipo de projeto
 */
export function getProjectTypeConfig(type: ProjectType): ProjectTypeConfig {
  return PROJECT_TYPE_CONFIG[type]
}

/**
 * Obtém o layout interno baseado no tipo de projeto
 * Esta é a única função necessária para converter type → layout
 */
export function getLayoutFromType(type: ProjectType): InternalLayout {
  return PROJECT_TYPE_CONFIG[type].layout
}

/**
 * Verifica se um tipo de projeto permite blocos dinâmicos
 */
export function allowsDynamicBlocks(type: ProjectType): boolean {
  return PROJECT_TYPE_CONFIG[type].allowsDynamicBlocks
}

/**
 * Valida se um número de blocos é válido para o tipo
 */
export function isValidBlockCount(type: ProjectType, blockCount: number): boolean {
  const config = PROJECT_TYPE_CONFIG[type]
  return blockCount >= config.minBlocks && blockCount <= config.maxBlocks
}

/**
 * Lista todos os tipos de projeto disponíveis
 */
export function getAllProjectTypes(): ProjectType[] {
  return Object.values(PROJECT_TYPES)
}
