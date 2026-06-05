"use client"

import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { SaveAll } from 'lucide-react'
import { useState, useEffect } from "react"
import { StorageOptionSelector, type StorageOption } from "./storage-option-selector"

interface SaveAsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  projectName: string
  onProjectNameChange: (name: string) => void
  onSave: (storageOption: StorageOption, tags: string[]) => void
  currentProjectSize: number
  getSizeIndicatorColor: () => string
  formatSize: (size: number) => string
  isDbInitialized: boolean
  availableTags?: Array<{ name: string }>
  initialTags?: string[]
}

export function SaveAsDialog({
  open,
  onOpenChange,
  projectName,
  onProjectNameChange,
  onSave,
  currentProjectSize,
  getSizeIndicatorColor,
  formatSize,
  isDbInitialized,
  availableTags = [],
  initialTags = [],
}: SaveAsDialogProps) {
  const [storageOption, setStorageOption] = useState<StorageOption>("local")
  const [projectTags, setProjectTags] = useState<string[]>(initialTags)
  const [tagInput, setTagInput] = useState("")
  const [showTagDropdown, setShowTagDropdown] = useState(false)

  // Sync initialTags when dialog opens
  useEffect(() => {
    if (open) setProjectTags(initialTags)
  }, [open, initialTags])

  // Close tag dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (showTagDropdown) {
        const target = event.target as Element
        if (!target.closest(".save-as-tag-dropdown")) {
          setShowTagDropdown(false)
        }
      }
    }
    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [showTagDropdown])

  const handleSave = () => {
    onSave(storageOption, projectTags)
    onOpenChange(false)
  }
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Save Project As</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div>
            <Label htmlFor="project-name">Project Name</Label>
            <Input
              id="project-name"
              value={projectName}
              onChange={(e) => onProjectNameChange(e.target.value)}
              placeholder="Enter project name..."
              onKeyDown={(e) => e.key === "Enter" && handleSave()}
              className="mt-1"
            />
          </div>
          {/* Storage Options Section */}
          <StorageOptionSelector
            selectedOption={storageOption}
            onSelectionChange={setStorageOption}
          />

          {/* Tags Section */}
          <div className="space-y-1.5">
            <Label className="text-sm font-semibold">Tags</Label>
            <div className="relative save-as-tag-dropdown">
              <div className="flex gap-2">
                <div className="relative flex-1">
                  <Input
                    placeholder="Search or create tags..."
                    value={tagInput}
                    onChange={(e) => {
                      setTagInput(e.target.value)
                      setShowTagDropdown(true)
                    }}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" && tagInput.trim()) {
                        e.preventDefault()
                        const newTag = tagInput.trim()
                        if (!projectTags.includes(newTag)) {
                          setProjectTags((prev) => [...prev, newTag])
                        }
                        setTagInput("")
                        setShowTagDropdown(false)
                      }
                      if (e.key === "Escape") {
                        setShowTagDropdown(false)
                      }
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
                <div className="absolute z-10 w-full mt-1 bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-md shadow-lg max-h-36 overflow-y-auto">
                  {(() => {
                    const filteredTags = tagInput.trim()
                      ? availableTags.filter(
                          (tag) =>
                            tag.name.toLowerCase().includes(tagInput.toLowerCase()) && !projectTags.includes(tag.name),
                        )
                      : availableTags.filter((tag) => !projectTags.includes(tag.name))

                    return (
                      <>
                        {filteredTags.length > 0 && filteredTags.slice(0, 8).map((tag) => (
                          <button
                            key={tag.name}
                            type="button"
                            onClick={() => {
                              setProjectTags((prev) => [...prev, tag.name])
                              setTagInput("")
                              setShowTagDropdown(false)
                            }}
                            className="w-full px-3 py-1.5 text-left hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-between"
                          >
                            <span className="text-sm">{tag.name}</span>
                          </button>
                        ))}

                        {tagInput.trim() &&
                          !availableTags.some((tag) => tag.name.toLowerCase() === tagInput.toLowerCase()) &&
                          !projectTags.includes(tagInput.trim()) && (
                            <button
                              type="button"
                              onClick={() => {
                                setProjectTags((prev) => [...prev, tagInput.trim()])
                                setTagInput("")
                                setShowTagDropdown(false)
                              }}
                              className="w-full px-3 py-1.5 text-left hover:bg-gray-100 dark:hover:bg-gray-700"
                            >
                              <div className="flex items-center gap-2">
                                <svg className="w-3.5 h-3.5 text-green-600 dark:text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                                </svg>
                                <span className="text-sm">
                                  Create &quot;<strong>{tagInput.trim()}</strong>&quot;
                                </span>
                              </div>
                            </button>
                          )}

                        {filteredTags.length === 0 && !tagInput.trim() && (
                          <div className="px-3 py-1.5 text-sm text-gray-500 dark:text-gray-400">
                            {availableTags.length === 0
                              ? "No tags yet. Type to create one."
                              : "Type to search or create tags..."}
                          </div>
                        )}
                      </>
                    )
                  })()}
                </div>
              )}
            </div>

            {/* Selected Tags */}
            {projectTags.length > 0 && (
              <div className="flex flex-wrap gap-1.5">
                {projectTags.map((tag, index) => (
                  <span
                    key={index}
                    className="inline-flex items-center gap-1 px-2.5 py-0.5 bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-xs rounded-full"
                  >
                    {tag}
                    <button
                      type="button"
                      onClick={() => setProjectTags((prev) => prev.filter((_, i) => i !== index))}
                      className="hover:text-blue-600 dark:hover:text-blue-300 ml-0.5"
                    >
                      <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </span>
                ))}
              </div>
            )}
          </div>
          
          <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
            <span className="text-sm text-gray-600 dark:text-gray-300">Project size:</span>
            <span className={`text-sm font-medium ${getSizeIndicatorColor()}`}>
              {formatSize(currentProjectSize)}
            </span>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button onClick={handleSave}>Save Project</Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
