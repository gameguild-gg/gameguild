"use client"

import React from 'react'
import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { 
  Folder, 
  Image as ImageIcon, 
  LayoutGrid, 
  List, 
  Columns as ColumnsIcon,
  Plus,
  Package
} from 'lucide-react'
import { type ViewMode } from './types'

interface ManagerLayoutProps {
  children: React.ReactNode
  activeContext: 'projects' | 'assets' | 'collections'
  viewMode: ViewMode
  gridColumns: number
  listColumns: number
  onContextChange: (context: 'projects' | 'assets' | 'collections') => void
  onViewModeChange: (mode: ViewMode) => void
  onGridColumnsChange: (columns: number) => void
  onListColumnsChange: (columns: number) => void
  onCreateNew?: () => void
  filterSection?: React.ReactNode
  paginationSection?: React.ReactNode
}

export function ManagerLayout({
  children,
  activeContext,
  viewMode,
  gridColumns,
  listColumns,
  onContextChange,
  onViewModeChange,
  onGridColumnsChange,
  onListColumnsChange,
  onCreateNew,
  filterSection,
  paginationSection,
}: ManagerLayoutProps) {
  const isGrid = viewMode === 'grid'
  const currentColumns = isGrid ? gridColumns : listColumns
  const columnOptions = isGrid 
    ? [
        { value: '4', label: '4 Columns' },
        { value: '5', label: '5 Columns' },
        { value: '6', label: '6 Columns' },
        { value: '9', label: '9 Columns (Compact)' },
        { value: '12', label: '12 Columns (Compact)' },
        { value: '15', label: '15 Columns (Compact)' },
      ]
    : [
        { value: '1', label: '1 Column' },
        { value: '2', label: '2 Columns' },
      ]

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="flex h-screen">
        {/* Left Sidebar */}
        <div className="w-64 bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 flex flex-col">
          {/* Logo/Header */}
          <div className="p-6 border-b border-gray-200 dark:border-gray-700">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 bg-gradient-to-br from-blue-500 to-purple-600 rounded-lg flex items-center justify-center">
                <span className="text-white font-bold text-sm">GG</span>
              </div>
              <div>
                <h1 className="text-lg font-bold text-gray-900 dark:text-white">GameGuild</h1>
              </div>
            </div>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Content Platform</p>
          </div>

          {/* Navigation */}
          <div className="flex-1 p-4 space-y-4">
            {/* Projects */}
            <div>
              <Button
                variant={activeContext === 'projects' ? 'default' : 'ghost'}
                className="w-full justify-start"
                onClick={() => onContextChange('projects')}
              >
                <Folder className="mr-2 h-4 w-4" />
                Projects
              </Button>
            </div>

            {/* Resources Group */}
            <div>
              <p className="text-xs font-semibold text-gray-500 dark:text-gray-400 px-3 mb-2 uppercase tracking-wider">
                Resources
              </p>
              <div className="space-y-1">
                <Button
                  variant={activeContext === 'assets' ? 'default' : 'ghost'}
                  className="w-full justify-start"
                  onClick={() => onContextChange('assets')}
                >
                  <ImageIcon className="mr-2 h-4 w-4" />
                  Assets
                </Button>
                <Button
                  variant={activeContext === 'collections' ? 'default' : 'ghost'}
                  className="w-full justify-start"
                  onClick={() => onContextChange('collections')}
                >
                  <Package className="mr-2 h-4 w-4" />
                  Collections
                </Button>
              </div>
            </div>
          </div>

          {/* Footer */}
          <div className="p-4 border-t border-gray-200 dark:border-gray-700">
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {activeContext === 'projects' ? 'Manage Projects' : activeContext === 'assets' ? 'Manage Assets' : 'Manage Collections'}
            </p>
          </div>
        </div>

        {/* Main Content */}
        <div className="flex-1 flex flex-col overflow-hidden">
          {/* Header */}
          <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 p-6">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
                  {activeContext === 'projects' ? 'My Projects' : activeContext === 'assets' ? 'My Assets' : 'My Collections'}
                </h2>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  {activeContext === 'projects' 
                    ? 'Manage and organize your projects'
                    : activeContext === 'assets'
                    ? 'Manage and organize your media files'
                    : 'Manage and organize your asset collections'
                  }
                </p>
              </div>
              
              {onCreateNew && activeContext !== 'collections' && (
                <Button onClick={onCreateNew}>
                  <Plus className="mr-2 h-4 w-4" />
                  {activeContext === 'projects' ? 'New Project' : 'Upload Asset'}
                </Button>
              )}
            </div>

            {/* View Controls */}
            <div className="flex items-center gap-2">
              <div className="flex items-center border border-gray-200 dark:border-gray-700 rounded-md">
                <Button
                  variant={viewMode === 'list' ? 'secondary' : 'ghost'}
                  size="sm"
                  onClick={() => onViewModeChange('list')}
                  className="rounded-r-none"
                >
                  <List className="h-4 w-4" />
                </Button>
                <Button
                  variant={viewMode === 'grid' ? 'secondary' : 'ghost'}
                  size="sm"
                  onClick={() => onViewModeChange('grid')}
                  className="rounded-l-none"
                >
                  <LayoutGrid className="h-4 w-4" />
                </Button>
              </div>

              <Select
                value={currentColumns.toString()}
                onValueChange={(value) => {
                  const cols = parseInt(value)
                  if (isGrid) {
                    onGridColumnsChange(cols)
                  } else {
                    onListColumnsChange(cols)
                  }
                }}
              >
                <SelectTrigger className="w-[140px]">
                  <ColumnsIcon className="h-4 w-4 mr-2" />
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {columnOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Filters */}
          {filterSection && (
            <div className="bg-gray-50 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700">
              {filterSection}
            </div>
          )}

          {/* Content Area */}
          <div className="flex-1 overflow-auto p-6">
            {children}
          </div>

          {/* Pagination */}
          {paginationSection && (
            <div className="bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700 p-4">
              {paginationSection}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
