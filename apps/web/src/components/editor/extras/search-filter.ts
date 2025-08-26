interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
}

interface SearchFilters {
  searchTerm: string
  selectedTags: string[]
  tagFilterMode: "all" | "any"
}

interface StorageAdapter {
  list: () => Promise<ProjectData[]>
  searchProjects: (searchTerm: string, tags: string[], filterMode: "all" | "any") => Promise<ProjectData[]>
}

/**
 * Utility function to format file size in human-readable format
 */
export function formatFileSize(bytes: number): string {
  if (bytes === 0) return "0 B"
  const k = 1024
  const sizes = ["B", "KB", "MB", "GB"]
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i]
}

/**
 * Filters projects based on search term and selected tags
 */
export async function filterProjects(
  storageAdapter: StorageAdapter,
  allProjects: ProjectData[],
  filters: SearchFilters
): Promise<ProjectData[]> {
  const { searchTerm, selectedTags, tagFilterMode } = filters
  
  // If no filters are applied, return all projects
  if (!searchTerm && selectedTags.length === 0) {
    return allProjects
  }
  
  try {
    // Use storage adapter's search function if available
    return await storageAdapter.searchProjects(searchTerm, selectedTags, tagFilterMode)
  } catch (error) {
    console.error("Failed to search projects:", error)
    return []
  }
}

/**
 * Filters available tags based on search input
 */
export function filterTagsForDropdown(
  availableTags: Array<{ name: string; usageCount: number }>,
  searchInput: string
): Array<{ name: string; usageCount: number }> {
  if (!searchInput.trim()) {
    return availableTags
  }
  
  return availableTags.filter((tag) =>
    tag.name.toLowerCase().includes(searchInput.toLowerCase())
  )
}

/**
 * Handles tag selection toggle
 */
export function toggleTag(
  selectedTags: string[],
  tagName: string
): string[] {
  return selectedTags.includes(tagName)
    ? selectedTags.filter((t) => t !== tagName)
    : [...selectedTags, tagName]
}

/**
 * Clears all filters
 */
export function clearAllFilters(): SearchFilters {
  return {
    searchTerm: "",
    selectedTags: [],
    tagFilterMode: "any"
  }
}

/**
 * Loads all projects from storage
 */
export async function loadAllProjects(
  storageAdapter: StorageAdapter
): Promise<ProjectData[]> {
  try {
    return await storageAdapter.list()
  } catch (error) {
    console.error("Failed to load projects:", error)
    return []
  }
}

/**
 * Hook for managing search and filter state
 */
export function createSearchFilterState() {
  return {
    searchTerm: "",
    selectedTags: [] as string[],
    tagFilterMode: "any" as "all" | "any",
    showFilters: false,
    tagSearchInput: "",
    showTagDropdown: false,
    loading: false
  }
}

/**
 * Validates if filters are active
 */
export function hasActiveFilters(filters: SearchFilters): boolean {
  return filters.searchTerm.length > 0 || filters.selectedTags.length > 0
}

/**
 * Gets filter summary text
 */
export function getFilterSummary(
  filteredCount: number,
  totalCount: number,
  selectedTagsCount: number
): { main: string; subtitle?: string } {
  const main = `${filteredCount} of ${totalCount} documents`
  const subtitle = selectedTagsCount > 0 
    ? `Filtered by ${selectedTagsCount} tag${selectedTagsCount > 1 ? 's' : ''}`
    : undefined
    
  return { main, subtitle }
}

// Export types for use in other components
export type { ProjectData, SearchFilters, StorageAdapter }
