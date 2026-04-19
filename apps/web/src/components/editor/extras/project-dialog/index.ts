// Main components
export { ProjectList } from './project-list'
export { ProjectCard } from './project-card'
export { ProjectGridView } from './project-grid-view'
export { ProjectListView } from './project-list-view'

// Filter and pagination components
export { ProjectSearchFilters } from './project-search-filters'
export { ProjectPagination } from './project-pagination'
export { AdvancedFilters } from './advanced-filters'

// Types
export interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
  isLocallyAvailable?: boolean
}