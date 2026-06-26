/**
 * Manager Page - Unified content management system
 * 
 * This module provides a standardized approach to displaying and managing
 * both projects and assets with consistent card styling, responsive layouts,
 * and unified filtering capabilities.
 */

// Core types
export * from './types'

// Components
export { ManagerCardComponent } from './card/card'
export { GridView } from './grid-view'
export { ListView } from './list-view'
export { ManagerLayout } from './manager-layout'
export { ManagerFilters } from './filters/filters'

// Utilities
export { applySorting } from './sorting'

// Re-export common types for convenience
export type {
  ViewMode,
  CardType,
  BaseCard,
  ProjectCard,
  AssetCard,
  ManagerCard,
  CardAction,
  FilterConfig,
  ManagerContext,
  ManagerPageProps,
} from './types'
