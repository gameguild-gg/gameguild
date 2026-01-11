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

// Group extensions by category for better display
const getExtensionCategory = (ext: string): { label: string; color: string } => {
  const imageExts = ['.jpg', '.jpeg', '.png', '.gif', '.svg', '.webp', '.bmp', '.ico', '.tiff', '.psd']
  const videoExts = ['.mp4', '.webm', '.ogg', '.avi', '.mov', '.wmv', '.flv', '.mkv', '.m4v', '.3gp']
  const audioExts = ['.mp3', '.wav', '.ogg', '.m4a', '.flac', '.aac', '.wma', '.opus', '.oga']
  const textExts = ['.txt', '.md', '.js', '.ts', '.jsx', '.tsx', '.json', '.xml', '.html', '.css', '.scss', '.sass', '.less', '.py', '.java', '.c', '.cpp', '.h', '.hpp', '.rs', '.go', '.rb', '.php', '.sh', '.bash', '.yml', '.yaml', '.sql', '.lua', '.r', '.swift', '.kt', '.cs', '.vb', '.pl', '.dart', '.scala']
  const appExts = ['.pdf', '.zip', '.rar', '.7z', '.tar', '.gz', '.exe', '.dmg', '.apk', '.deb', '.rpm', '.msi']
  const fontExts = ['.ttf', '.otf', '.woff', '.woff2', '.eot']

  if (imageExts.includes(ext)) return { label: 'Image', color: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200' }
  if (videoExts.includes(ext)) return { label: 'Video', color: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200' }
  if (audioExts.includes(ext)) return { label: 'Audio', color: 'bg-pink-100 text-pink-800 dark:bg-pink-900 dark:text-pink-200' }
  if (textExts.includes(ext)) return { label: 'Text', color: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' }
  if (appExts.includes(ext)) return { label: 'App', color: 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200' }
  if (fontExts.includes(ext)) return { label: 'Font', color: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900 dark:text-indigo-200' }
  
  return { label: 'File', color: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200' }
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
    (filters.mimeTypes && filters.mimeTypes.length > 0) ||
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

      {filters.mimeTypes?.map((ext) => {
        const category = getExtensionCategory(ext)
        return (
          <Badge key={ext} variant="secondary" className={`gap-1 ${category.color}`}>
            <span className="font-mono text-xs">{ext}</span>
            <X 
              className="h-3 w-3 cursor-pointer" 
              onClick={() => {
                const newTypes = filters.mimeTypes?.filter((t) => t !== ext) || []
                onFilterChange({ mimeTypes: newTypes })
              }}
            />
          </Badge>
        )
      })}

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
          mimeTypes: [],
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
