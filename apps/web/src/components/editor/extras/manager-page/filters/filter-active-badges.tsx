"use client"

import React from 'react'
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { X } from 'lucide-react'
import { type FilterConfig } from '../types'

const SORT_LABELS: Record<string, string> = {
  'newest': 'Newest',
  'oldest': 'Oldest',
  'name': 'A-Z',
  'name-desc': 'Z-A',
  'size-largest': 'Largest',
  'size-smallest': 'Smallest',
}

interface FilterActiveBadgesProps {
  filters: FilterConfig
  availableProjects: Array<{ id: string; name: string }>
  onFilterChange: (filters: Partial<FilterConfig>) => void
}

export function FilterActiveBadges({ 
  filters, 
  availableProjects, 
  onFilterChange 
}: FilterActiveBadgesProps) {
  const hasActiveFilters = 
    (filters.tags && filters.tags.length > 0) || 
    (filters.storageType && filters.storageType !== 'all') ||
    (filters.mimeType && filters.mimeType !== 'all') ||
    (filters.assetType && filters.assetType !== 'all') ||
    (filters.projectFilter && filters.projectFilter !== 'all') ||
    (filters.usageFilter && filters.usageFilter !== 'all') ||
    (filters.sortOrder && filters.sortOrder.length > 0)

  if (!hasActiveFilters) return null

  return (
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

      {filters.sortOrder?.map((sort) => (
        <Badge key={sort} variant="secondary" className="gap-1">
          {SORT_LABELS[sort] || sort}
          <X 
            className="h-3 w-3 cursor-pointer" 
            onClick={() => {
              const newSort = filters.sortOrder?.filter((s) => s !== sort) || []
              onFilterChange({ sortOrder: newSort })
            }}
          />
        </Badge>
      ))}

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
  )
}
