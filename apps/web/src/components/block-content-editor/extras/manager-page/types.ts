/**
 * Shared types for the Manager Page system
 * Unifies project and asset management interfaces
 */

export type ViewMode = 'list' | 'grid'

export type CardType = 'project' | 'asset' | 'collection'

export interface BaseCard {
  id: string
  name: string
  createdAt: string
  updatedAt: string
}

export interface ProjectCard extends BaseCard {
  type: 'project'
  tags: string[]
  size: number
  data: string
  storageType?: 'local' | 'gameguild-cloud' | 'google-drive'
  isLocallyAvailable?: boolean
  /** High-level kind (document/quiz/general). Read from preferences. */
  projectType?: 'document' | 'quiz' | 'general'
}

export interface AssetCard extends BaseCard {
  type: 'asset'
  mimeType: string
  size: number
  projects?: string[]
  thumbnailUrl?: string
  assetType?: 'standard' | 'bundler'
}

export interface CollectionCard extends BaseCard {
  type: 'collection'
  description?: string
  tags?: string[]
  fileCount: number
  totalSize: number
}

export type ManagerCard = ProjectCard | AssetCard | CollectionCard

export interface CardAction {
  label: string
  icon?: React.ReactNode
  onClick: (card: ManagerCard) => void
  variant?: 'default' | 'destructive'
}

export interface FilterConfig {
  searchTerm: string
  tags?: string[]
  tagFilterMode?: 'all' | 'any'
  storageType?: 'all' | 'local' | 'gameguild-cloud' | 'google-drive'
  mimeTypes?: string[] // Multi-select MIME types filter
  assetType?: 'all' | 'standard' | 'bundler'
  projectFilter?: string
  usageFilter?: 'all' | 'used' | 'unused'
  dateFrom?: string
  dateTo?: string
  sortOrder?: Array<'newest' | 'oldest' | 'name' | 'name-desc' | 'size-largest' | 'size-smallest'>
}

export interface ManagerContext {
  type: 'projects' | 'assets' | 'collections'
  viewMode: ViewMode
  gridColumns: number
  listColumns: number
  currentPage: number
  itemsPerPage: number
  filters: FilterConfig
}

export interface ManagerPageProps {
  context: ManagerContext
  cards: ManagerCard[]
  totalCards: number
  primaryActions: CardAction[]
  secondaryActions: CardAction[]
  onContextChange: (context: 'projects' | 'assets' | 'collections') => void
  onViewModeChange: (mode: ViewMode) => void
  onGridColumnsChange: (columns: number) => void
  onListColumnsChange: (columns: number) => void
  onPageChange: (page: number) => void
  onItemsPerPageChange: (items: number) => void
  onFilterChange: (filters: Partial<FilterConfig>) => void
  availableTags?: Array<{ name: string }>
  availableProjects?: Array<{ id: string; name: string }>
}
