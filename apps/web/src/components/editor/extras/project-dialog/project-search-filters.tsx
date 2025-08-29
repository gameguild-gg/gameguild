"use client"

import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useState, useEffect } from "react"
import { ChevronDown, ChevronUp } from "lucide-react"

interface ProjectSearchFiltersProps {
  searchTerm: string
  onSearchChange: (value: string) => void
  selectedTags: string[]
  onTagsChange: (tags: string[]) => void
  availableTags: Array<{ name: string }>
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
  const [isCollapsed, setIsCollapsed] = useState(false)

  // Close tag dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (showTagDropdown) {
        const target = event.target as Element
        if (!target.closest(".tag-filter-container")) {
          setShowTagDropdown(false)
        }
      }
    }

    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [showTagDropdown])

  return (
    <div className="space-y-3 rounded-lg bg-gray-100/50 p-4 dark:bg-gray-900/50">
      <div className="flex cursor-pointer items-center justify-between" onClick={() => setIsCollapsed(!isCollapsed)}>
        <Label className="text-base font-medium">Filters</Label>
        <button
          className="rounded-full p-1.5 hover:bg-gray-200 dark:hover:bg-gray-800"
          aria-label={isCollapsed ? "Show filters" : "Hide filters"}
        >
          {isCollapsed ? <ChevronDown className="h-5 w-5" /> : <ChevronUp className="h-5 w-5" />}
        </button>
      </div>
      {!isCollapsed && (
        <div className="flex flex-col gap-4 pt-2 md:flex-row md:gap-6">
          {/* Project Search Section */}
          <div className="flex-1 space-y-3">
            <div className="flex items-center justify-between">
              <Label className="text-sm font-medium">Filter by Projects:</Label>
              <div className="flex items-center gap-2">
                <Label className="text-xs text-gray-700 dark:text-gray-400">Items per page:</Label>
                <select
                  value={itemsPerPage}
                  onChange={(e) => onItemsPerPageChange(Number(e.target.value))}
                  className="rounded border bg-background px-2 py-1 text-sm"
                >
                  <option value={8}>8</option>
                  <option value={16}>16</option>
                  <option value={32}>32</option>
                  <option value={64}>64</option>
                  <option value={128}>128</option>
                </select>
              </div>
            </div>
            <Input
              placeholder="Search projects by name..."
              value={searchTerm}
              onChange={(e) => onSearchChange(e.target.value)}
              className="w-full"
            />
          </div>

          <div className="hidden h-auto w-px bg-gray-200 dark:bg-gray-700 md:block" />

          {/* Tags Filter Section */}
          <div className="tag-filter-container flex-1 space-y-2">
            <div className="flex items-center justify-between">
              <Label className="text-sm font-medium">Filter by tags:</Label>
              <div className="flex items-center gap-2">
                <Label className="text-xs text-gray-700 dark:text-gray-400">Match tags:</Label>
                <select
                  value={tagFilterMode}
                  onChange={(e) => onTagFilterModeChange(e.target.value as "all" | "any")}
                  className="rounded border bg-background px-2 py-1 text-xs"
                >
                  <option value="any">Any tags</option>
                  <option value="all">All tags</option>
                </select>
              </div>
            </div>

            <div className="relative">
              <Input
                placeholder="Search or select tags..."
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
                  className={`h-4 w-4 transition-transform ${showTagDropdown ? "rotate-180" : ""}`}
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </button>

              {showTagDropdown && (
                <div className="absolute z-10 mt-1 max-h-48 w-full overflow-y-auto rounded-md border bg-white shadow-lg dark:border-gray-700 dark:bg-gray-800">
                  {(() => {
                    const filtered = availableTags.filter((tag) =>
                      tag.name.toLowerCase().includes(tagSearchInput.toLowerCase()),
                    )
                    if (filtered.length === 0) {
                      return (
                        <div className="px-3 py-2 text-sm text-gray-500 dark:text-gray-400">
                          {availableTags.length === 0 ? "No tags available" : `No tags matching "${tagSearchInput}"`}
                        </div>
                      )
                    }
                    return filtered.map((tag) => (
                      <button
                        key={tag.name}
                        type="button"
                        onClick={() =>
                          onTagsChange(
                            selectedTags.includes(tag.name)
                              ? selectedTags.filter((t) => t !== tag.name)
                              : [...selectedTags, tag.name],
                          )
                        }
                        className="flex w-full items-center justify-between px-3 py-2 text-left transition-colors hover:bg-gray-100 dark:hover:bg-gray-700"
                      >
                        <div className="flex items-center gap-2">
                          <div
                            className={`flex h-4 w-4 items-center justify-center rounded border ${
                              selectedTags.includes(tag.name)
                                ? "border-blue-500 bg-blue-500"
                                : "border-gray-300 dark:border-gray-600"
                            }`}
                          >
                            {selectedTags.includes(tag.name) && (
                              <svg className="h-3 w-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path
                                  strokeLinecap="round"
                                  strokeLinejoin="round"
                                  strokeWidth={2}
                                  d="M5 13l4 4L19 7"
                                />
                              </svg>
                            )}
                          </div>
                          <span className="text-sm">{tag.name}</span>
                        </div>
                      </button>
                    ))
                  })()}
                </div>
              )}
            </div>

            {selectedTags.length > 0 && (
              <div className="flex flex-wrap items-center justify-between gap-2 pt-2">
                <div className="flex flex-wrap gap-1">
                  {selectedTags.map((tagName) => (
                    <span
                      key={tagName}
                      className="inline-flex items-center gap-1 rounded-full bg-blue-100 px-2 py-1 text-xs text-blue-800 dark:bg-blue-900 dark:text-blue-200"
                    >
                      {tagName}
                      <button
                        type="button"
                        onClick={() => onTagsChange(selectedTags.filter((t) => t !== tagName))}
                        className="hover:text-blue-600 dark:hover:text-blue-300"
                      >
                        <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                      </button>
                    </span>
                  ))}
                </div>
                <button
                  type="button"
                  onClick={() => onTagsChange([])}
                  className="text-xs text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
                >
                  Clear all
                </button>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
