"use client"

import React from 'react'
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import { Search, Filter, X, Tag as TagIcon } from 'lucide-react'
import { type FilterConfig } from './types'

interface ManagerFiltersProps {
  filters: FilterConfig
  onFilterChange: (filters: Partial<FilterConfig>) => void
  availableTags?: Array<{ name: string }>
  availableProjects?: Array<{ id: string; name: string }>
  contextType: 'projects' | 'assets' | 'collections'
  itemsPerPage: number
  onItemsPerPageChange: (items: number) => void
}

export function ManagerFilters({
  filters,
  onFilterChange,
  availableTags = [],
  availableProjects = [],
  contextType,
  itemsPerPage,
  onItemsPerPageChange,
}: ManagerFiltersProps) {
  const isProjectContext = contextType === 'projects'
  const isAssetContext = contextType === 'assets'
  const isCollectionContext = contextType === 'collections'

  return (
    <div className="p-4 space-y-4">
      {/* Search and Tags Row */}
      <div className="flex items-center gap-2 flex-wrap">
        {/* Search */}
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
          <Input
            placeholder={isProjectContext ? "Search projects..." : isAssetContext ? "Search assets..." : "Search collections..."}
            value={filters.searchTerm}
            onChange={(e) => onFilterChange({ searchTerm: e.target.value })}
            className="pl-9"
          />
        </div>

        {/* Tags (Projects and Collections) */}
        {(isProjectContext || isCollectionContext) && availableTags.length > 0 && (
          <Popover>
            <PopoverTrigger asChild>
              <Button variant="outline" size="sm">
                <TagIcon className="mr-2 h-4 w-4" />
                Tags
                {filters.tags && filters.tags.length > 0 && (
                  <Badge variant="secondary" className="ml-2">
                    {filters.tags.length}
                  </Badge>
                )}
              </Button>
            </PopoverTrigger>
            <PopoverContent className="w-80">
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <h4 className="font-medium text-sm">Filter by Tags</h4>
                  {filters.tags && filters.tags.length > 0 && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => onFilterChange({ tags: [] })}
                    >
                      Clear
                    </Button>
                  )}
                </div>

                {/* Tag Filter Mode */}
                <Select
                  value={filters.tagFilterMode || 'all'}
                  onValueChange={(value: 'all' | 'any') => onFilterChange({ tagFilterMode: value })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All tags</SelectItem>
                    <SelectItem value="any">Any tag</SelectItem>
                  </SelectContent>
                </Select>

                {/* Tag List */}
                <div className="flex flex-wrap gap-1 max-h-48 overflow-y-auto">
                  {availableTags.map((tag) => {
                    const isSelected = filters.tags?.includes(tag.name)
                    return (
                      <Badge
                        key={tag.name}
                        variant={isSelected ? 'default' : 'outline'}
                        className="cursor-pointer"
                        onClick={() => {
                          const currentTags = filters.tags || []
                          const newTags = isSelected
                            ? currentTags.filter((t) => t !== tag.name)
                            : [...currentTags, tag.name]
                          onFilterChange({ tags: newTags })
                        }}
                      >
                        {tag.name}
                      </Badge>
                    )
                  })}
                </div>
              </div>
            </PopoverContent>
          </Popover>
        )}

        {/* Items per page */}
        <Select
          value={itemsPerPage.toString()}
          onValueChange={(value) => onItemsPerPageChange(parseInt(value))}
        >
          <SelectTrigger className="w-[140px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="12">12 per page</SelectItem>
            <SelectItem value="24">24 per page</SelectItem>
            <SelectItem value="48">48 per page</SelectItem>
            <SelectItem value="96">96 per page</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Additional Filters Row */}
      <div className="flex items-center gap-2 flex-wrap">
        {/* Storage Type (Projects only) */}
        {isProjectContext && (
          <Select
            value={filters.storageType || 'all'}
            onValueChange={(value) => onFilterChange({ storageType: value as any })}
          >
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Storage type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All types</SelectItem>
              <SelectItem value="local">Local</SelectItem>
              <SelectItem value="gameguild-cloud">GameGuild Cloud</SelectItem>
              <SelectItem value="google-drive">Google Drive</SelectItem>
            </SelectContent>
          </Select>
        )}

        {/* MIME Type (Assets only) */}
        {isAssetContext && (
          <Select
            value={filters.mimeType || 'all'}
            onValueChange={(value) => onFilterChange({ mimeType: value })}
          >
            <SelectTrigger className="w-[150px]">
              <SelectValue placeholder="File type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All types</SelectItem>
              <SelectItem value="image">Images</SelectItem>
              <SelectItem value="video">Videos</SelectItem>
              <SelectItem value="audio">Audio</SelectItem>
              <SelectItem value="text">Text</SelectItem>
            </SelectContent>
          </Select>
        )}

        {/* Asset Type (Assets only) */}
        {isAssetContext && (
          <Select
            value={filters.assetType || 'all'}
            onValueChange={(value) => onFilterChange({ assetType: value as any })}
          >
            <SelectTrigger className="w-[150px]">
              <SelectValue placeholder="Asset type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              <SelectItem value="standard">Standard</SelectItem>
              <SelectItem value="bundler">Bundler</SelectItem>
            </SelectContent>
          </Select>
        )}

        {/* Project Filter (Assets only) */}
        {isAssetContext && availableProjects.length > 0 && (
          <Select
            value={filters.projectFilter || 'all'}
            onValueChange={(value) => onFilterChange({ projectFilter: value })}
          >
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Filter by project" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All projects</SelectItem>
              {availableProjects.map((project) => (
                <SelectItem key={project.id} value={project.id}>
                  {project.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}

        {/* Usage Filter (Assets only) */}
        {isAssetContext && (
          <Select
            value={filters.usageFilter || 'all'}
            onValueChange={(value) => onFilterChange({ usageFilter: value as any })}
          >
            <SelectTrigger className="w-[150px]">
              <SelectValue placeholder="Usage" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              <SelectItem value="used">Used</SelectItem>
              <SelectItem value="unused">Unused</SelectItem>
            </SelectContent>
          </Select>
        )}

        {/* Sort Order - Multi-select */}
        <Popover>
          <PopoverTrigger asChild>
            <Button variant="outline" size="sm">
              <Filter className="mr-2 h-4 w-4" />
              Sort
              {filters.sortOrder && filters.sortOrder.length > 0 && (
                <Badge variant="secondary" className="ml-2">
                  {filters.sortOrder.length}
                </Badge>
              )}
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-64">
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <h4 className="font-medium text-sm">Sort By</h4>
                {filters.sortOrder && filters.sortOrder.length > 0 && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => onFilterChange({ sortOrder: [] })}
                  >
                    Clear
                  </Button>
                )}
              </div>

              {/* Sort options with checkboxes */}
              <div className="space-y-2">
                {[
                  { value: 'newest', label: 'Date: Newest First', group: 'date' },
                  { value: 'oldest', label: 'Date: Oldest First', group: 'date' },
                  { value: 'name', label: 'Name: A-Z', group: 'name' },
                  { value: 'name-desc', label: 'Name: Z-A', group: 'name' },
                  { value: 'size-largest', label: 'Size: Largest First', group: 'size' },
                  { value: 'size-smallest', label: 'Size: Smallest First', group: 'size' },
                ].map((option) => {
                  const isSelected = filters.sortOrder?.includes(option.value as any)
                  
                  // Check if the opposite option in the same group is selected
                  const currentSort = filters.sortOrder || []
                  const groupOptions: Record<string, string[]> = {
                    'date': ['newest', 'oldest'],
                    'name': ['name', 'name-desc'],
                    'size': ['size-largest', 'size-smallest']
                  }
                  const oppositeSelected = groupOptions[option.group]?.some(
                    opt => opt !== option.value && currentSort.includes(opt as any)
                  )
                  
                  return (
                    <label
                      key={option.value}
                      className={`flex items-center gap-2 p-2 rounded ${
                        oppositeSelected && !isSelected
                          ? 'cursor-not-allowed opacity-50'
                          : 'cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800'
                      }`}
                    >
                      <input
                        type="checkbox"
                        checked={isSelected}
                        disabled={oppositeSelected && !isSelected}
                        onChange={() => {
                          const currentSort = filters.sortOrder || []
                          const newSort = isSelected
                            ? currentSort.filter((s) => s !== option.value)
                            : [...currentSort, option.value as any]
                          onFilterChange({ sortOrder: newSort })
                        }}
                        className="h-4 w-4"
                      />
                      <span className="text-sm">{option.label}</span>
                    </label>
                  )
                })}
              </div>
            </div>
          </PopoverContent>
        </Popover>
      </div>      {/* Active Filters */}
      {(filters.tags && filters.tags.length > 0) || 
       (filters.storageType && filters.storageType !== 'all') ||
       (filters.mimeType && filters.mimeType !== 'all') ||
       (filters.assetType && filters.assetType !== 'all') ||
       (filters.projectFilter && filters.projectFilter !== 'all') ||
       (filters.usageFilter && filters.usageFilter !== 'all') ||
       (filters.sortOrder && filters.sortOrder.length > 0) ? (
        <div className="flex items-center gap-2 flex-wrap pt-2 border-t border-gray-200 dark:border-gray-700">
          <span className="text-sm text-gray-600 dark:text-gray-400">Active filters:</span>
          
          {filters.tags?.map((tag) => (
            <Badge key={tag} variant="secondary" className="gap-1">
              {tag}
              <X 
                className="h-3 w-3 cursor-pointer" 
                onClick={() => {
                  const newTags = filters.tags?.filter((t) => t !== tag) || []
                  onFilterChange({ tags: newTags })
                }}
              />
            </Badge>
          ))}

          {filters.storageType && filters.storageType !== 'all' && (
            <Badge variant="secondary" className="gap-1">
              {filters.storageType}
              <X 
                className="h-3 w-3 cursor-pointer" 
                onClick={() => onFilterChange({ storageType: 'all' })}
              />
            </Badge>
          )}

          {filters.mimeType && filters.mimeType !== 'all' && (
            <Badge variant="secondary" className="gap-1">
              {filters.mimeType}
              <X 
                className="h-3 w-3 cursor-pointer" 
                onClick={() => onFilterChange({ mimeType: 'all' })}
              />
            </Badge>
          )}

          {filters.assetType && filters.assetType !== 'all' && (
            <Badge variant="secondary" className="gap-1">
              {filters.assetType === 'standard' ? 'Standard' : 'Bundler'}
              <X 
                className="h-3 w-3 cursor-pointer" 
                onClick={() => onFilterChange({ assetType: 'all' })}
              />
            </Badge>
          )}

          {filters.projectFilter && filters.projectFilter !== 'all' && (
            <Badge variant="secondary" className="gap-1">
              {availableProjects.find((p) => p.id === filters.projectFilter)?.name || filters.projectFilter}
              <X 
                className="h-3 w-3 cursor-pointer" 
                onClick={() => onFilterChange({ projectFilter: 'all' })}
              />
            </Badge>
          )}

          {filters.usageFilter && filters.usageFilter !== 'all' && (
            <Badge variant="secondary" className="gap-1">
              {filters.usageFilter === 'used' ? 'Used' : 'Unused'}
              <X 
                className="h-3 w-3 cursor-pointer" 
                onClick={() => onFilterChange({ usageFilter: 'all' })}
              />
            </Badge>
          )}

          {filters.sortOrder?.map((sort) => {
            const sortLabels: Record<string, string> = {
              'newest': 'Newest',
              'oldest': 'Oldest',
              'name': 'A-Z',
              'name-desc': 'Z-A',
              'size-largest': 'Largest',
              'size-smallest': 'Smallest',
            }
            return (
              <Badge key={sort} variant="secondary" className="gap-1">
                {sortLabels[sort] || sort}
                <X 
                  className="h-3 w-3 cursor-pointer" 
                  onClick={() => {
                    const newSort = filters.sortOrder?.filter((s) => s !== sort) || []
                    onFilterChange({ sortOrder: newSort })
                  }}
                />
              </Badge>
            )
          })}

          <Button
            variant="ghost"
            size="sm"
            onClick={() => onFilterChange({
              tags: [],
              storageType: 'all',
              mimeType: 'all',
              assetType: 'all',
              projectFilter: 'all',
              usageFilter: 'all',
              sortOrder: [],
            })}
          >
            Clear all
          </Button>
        </div>
      ) : null}
    </div>
  )
}
