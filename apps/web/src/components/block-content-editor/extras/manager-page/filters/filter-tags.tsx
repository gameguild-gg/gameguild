"use client"

import React from 'react'
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
import { Tag as TagIcon } from 'lucide-react'

interface FilterTagsProps {
  selectedTags: string[]
  availableTags: Array<{ name: string }>
  tagFilterMode: 'all' | 'any'
  onTagsChange: (tags: string[]) => void
  onModeChange: (mode: 'all' | 'any') => void
}

export function FilterTags({ 
  selectedTags, 
  availableTags, 
  tagFilterMode, 
  onTagsChange, 
  onModeChange 
}: FilterTagsProps) {
  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button variant="outline" size="sm">
          <TagIcon className="mr-2 h-4 w-4" />
          Tags
          {selectedTags.length > 0 && (
            <Badge variant="secondary" className="ml-2">
              {selectedTags.length}
            </Badge>
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-80">
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h4 className="font-medium text-sm">Filter by Tags</h4>
            {selectedTags.length > 0 && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => onTagsChange([])}
              >
                Clear
              </Button>
            )}
          </div>

          <Select value={tagFilterMode} onValueChange={(value: 'all' | 'any') => onModeChange(value)}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All tags</SelectItem>
              <SelectItem value="any">Any tag</SelectItem>
            </SelectContent>
          </Select>

          <div className="flex flex-wrap gap-1 max-h-48 overflow-y-auto">
            {availableTags.map((tag) => {
              const isSelected = selectedTags.includes(tag.name)
              return (
                <Badge
                  key={tag.name}
                  variant={isSelected ? 'default' : 'outline'}
                  className="cursor-pointer"
                  onClick={() => {
                    const newTags = isSelected
                      ? selectedTags.filter((t) => t !== tag.name)
                      : [...selectedTags, tag.name]
                    onTagsChange(newTags)
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
  )
}
