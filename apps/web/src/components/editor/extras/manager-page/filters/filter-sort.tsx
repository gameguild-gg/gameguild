"use client"

import React from 'react'
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import { Filter } from 'lucide-react'

type SortOrder = 'newest' | 'oldest' | 'name' | 'name-desc' | 'size-largest' | 'size-smallest'

interface SortOption {
  value: SortOrder
  label: string
  group: string
}

const SORT_OPTIONS: SortOption[] = [
  { value: 'newest', label: 'Date: Newest First', group: 'date' },
  { value: 'oldest', label: 'Date: Oldest First', group: 'date' },
  { value: 'name', label: 'Name: A-Z', group: 'name' },
  { value: 'name-desc', label: 'Name: Z-A', group: 'name' },
  { value: 'size-largest', label: 'Size: Largest First', group: 'size' },
  { value: 'size-smallest', label: 'Size: Smallest First', group: 'size' },
]

const GROUP_OPTIONS: Record<string, SortOrder[]> = {
  'date': ['newest', 'oldest'],
  'name': ['name', 'name-desc'],
  'size': ['size-largest', 'size-smallest']
}

interface FilterSortProps {
  sortOrder: SortOrder[]
  onSortChange: (sortOrder: SortOrder[]) => void
}

export function FilterSort({ sortOrder, onSortChange }: FilterSortProps) {
  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button variant="outline" size="sm">
          <Filter className="mr-2 h-4 w-4" />
          Sort
          {sortOrder.length > 0 && (
            <Badge variant="secondary" className="ml-2">
              {sortOrder.length}
            </Badge>
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-64">
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h4 className="font-medium text-sm">Sort By</h4>
            {sortOrder.length > 0 && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => onSortChange([])}
              >
                Clear
              </Button>
            )}
          </div>

          <div className="space-y-2">
            {SORT_OPTIONS.map((option) => {
              const isSelected = sortOrder.includes(option.value)
              const oppositeSelected = GROUP_OPTIONS[option.group]?.some(
                opt => opt !== option.value && sortOrder.includes(opt)
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
                      const newSort = isSelected
                        ? sortOrder.filter((s) => s !== option.value)
                        : [...sortOrder, option.value]
                      onSortChange(newSort)
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
  )
}
