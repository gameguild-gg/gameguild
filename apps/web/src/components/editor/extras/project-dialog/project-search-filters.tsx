"use client"

import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useState, useEffect } from "react"

interface ProjectSearchFiltersProps {
  searchTerm: string
  onSearchChange: (value: string) => void
  selectedTags: string[]
  onTagsChange: (tags: string[]) => void
  availableTags: Array<{ name: string; usageCount: number }>
  tagFilterMode: "all" | "any"
  onTagFilterModeChange: (mode: "all" | "any") => void
  itemsPerPage: number
  onItemsPerPageChange: (value: number) => void
}

export function ProjectSearchFilters({
  searchTerm,
  onSearchChange,
  selectedTags,
  onTagsChange,
  availableTags,
  tagFilterMode,
  onTagFilterModeChange,
  itemsPerPage,
  onItemsPerPageChange,
}: ProjectSearchFiltersProps) {
  const [tagSearchInput, setTagSearchInput] = useState("")
  const [showTagDropdown, setShowTagDropdown] = useState(false)

  // Close tag dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (showTagDropdown) {
        const target = event.target as Element
        if (!target.closest(".relative")) {
          setShowTagDropdown(false)
        }
      }
    }

    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [showTagDropdown])

  return (
    <div className="space-y-3 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
      {/* Search and Filter Section */}
      <div className="flex items-center justify-between">
        <Label className="text-sm font-medium">Filter by Projects:</Label>
        <div className="flex items-center gap-2">
          <Label className="text-xs text-gray-700 dark:text-gray-400">Items per page:</Label>
          <select
            value={itemsPerPage}
            onChange={(e) => onItemsPerPageChange(Number(e.target.value))}
            className="px-2 py-1 border rounded text-sm bg-background"
          >
            <option value={3}>3</option>
            <option value={6}>6</option>
            <option value={12}>12</option>
            <option value={24}>24</option>
          </select>
        </div>
      </div>
      <div className="flex-1">
        <Input
          placeholder="Search projects by name..."
          value={searchTerm}
          onChange={(e) => onSearchChange(e.target.value)}
          className="w-full"
        />
      </div>

      {/* Tags Filter */}
      <div className="space-y-2 min-h-[15vh] border-b">
        <div className="flex items-center justify-between">
          <Label className="text-sm font-medium">Filter by tags:</Label>
          <div className="flex items-center gap-2">
            <Label className="text-xs text-gray-700 dark:text-gray-400">Match tags:</Label>
            <select
              value={tagFilterMode}
              onChange={(e) => onTagFilterModeChange(e.target.value as "all" | "any")}
              className="px-2 py-1 border rounded text-xs bg-background"
            >
              <option value="any">Any tags</option>
              <option value="all">All tags</option>
            </select>
          </div>
        </div>

        <div className="relative">
          <div className="flex gap-2">
            <div className="relative flex-1">
              <Input
                placeholder="Search tags or leave empty to see all..."
                value={tagSearchInput}
                onChange={(e) => {
                  setTagSearchInput(e.target.value)
                  setShowTagDropdown(true)
                }}
                onFocus={() => setShowTagDropdown(true)}
                className="pr-10"
              />
              <button
                type="button"
                onClick={() => setShowTagDropdown(!showTagDropdown)}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
              >
                <svg
                  className={`w-4 h-4 transition-transform ${showTagDropdown ? "rotate-180" : ""}`}
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </button>
            </div>
          </div>

          {showTagDropdown && (
            <div className="absolute z-10 w-full mt-1 bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-md shadow-lg max-h-48 overflow-y-auto">
              {availableTags.length === 0 ? (
                <div className="px-3 py-2 text-sm text-gray-500 dark:text-gray-400">No tags available</div>
              ) : (
                (() => {
                  const filteredTags = tagSearchInput.trim()
                    ? availableTags.filter((tag) => tag.name.toLowerCase().includes(tagSearchInput.toLowerCase()))
                    : availableTags

                  return filteredTags.length === 0 ? (
                    <div className="px-3 py-2 text-sm text-gray-500 dark:text-gray-400">
                      No tags found matching "{tagSearchInput}"
                    </div>
                  ) : (
                    filteredTags.map((tag) => (
                      <button
                        key={tag.name}
                        type="button"
                        onClick={() => {
                          onTagsChange(
                            selectedTags.includes(tag.name)
                              ? selectedTags.filter((t) => t !== tag.name)
                              : [...selectedTags, tag.name],
                          )
                        }}
                        className="w-full px-3 py-2 text-left hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-between transition-colors"
                      >
                        <div className="flex items-center gap-2">
                          <div
                            className={`w-4 h-4 border rounded flex items-center justify-center ${
                              selectedTags.includes(tag.name)
                                ? "bg-blue-500 border-blue-500"
                                : "border-gray-300 dark:border-gray-600"
                            }`}
                          >
                            {selectedTags.includes(tag.name) && (
                              <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                              </svg>
                            )}
                          </div>
                          <span className="text-sm">{tag.name}</span>
                        </div>
                        <span className="text-xs text-gray-500 dark:text-gray-400">({tag.usageCount})</span>
                      </button>
                    ))
                  )
                })()
              )}
            </div>
          )}
        </div>

        {/* Selected tags display */}
        {selectedTags.length > 0 && (
          <div className="space-y-2">
            <div className="flex flex-wrap gap-2">
              {selectedTags.map((tagName) => (
                <span
                  key={tagName}
                  className="inline-flex items-center gap-1 px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-xs rounded-full"
                >
                  {tagName}
                  <button
                    type="button"
                    onClick={() => onTagsChange(selectedTags.filter((t) => t !== tagName))}
                    className="hover:text-blue-600 dark:hover:text-blue-300"
                  >
                    <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </span>
              ))}
            </div>
            <div className="flex items-center justify-between">
              <button
                type="button"
                onClick={() => {
                  onTagsChange([])
                  setTagSearchInput("")
                }}
                className="text-xs text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
              >
                Clear all filters
              </button>
              <span className="text-xs text-gray-500 dark:text-gray-400">
                Showing projects with {tagFilterMode === "all" ? "all" : "any"} of these tags
              </span>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
