/**
 * Project Types System
 * 
 * Only engine types remain. Layout is always single-pane.
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
