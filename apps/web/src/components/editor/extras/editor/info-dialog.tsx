"use client"

import type React from "react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useState, useEffect } from "react"
import { toast } from "sonner"
import { X } from "lucide-react"

interface ProjectData {
  id: string
  name: string
  tags: string[]
}

interface StorageAdapter {
  list: () => Promise<Array<{ id: string; name: string }>>
}

interface InfoDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  project: ProjectData | null
  onSave: (projectId: string, newName: string, newTags: string[]) => Promise<void>
  availableTags: Array<{ name: string }>
  storageAdapter: StorageAdapter
}

export function InfoDialog({
  open,
  onOpenChange,
  project,
  onSave,
  availableTags,
  storageAdapter,
}: InfoDialogProps) {
  const [name, setName] = useState("")
  const [tags, setTags] = useState<string[]>([])
  const [tagInput, setTagInput] = useState("")
  const [showTagDropdown, setShowTagDropdown] = useState(false)

  useEffect(() => {
    if (project) {
      setName(project.name)
      setTags(project.tags || [])
    } else {
      // Reset state when dialog is closed or project is null
      setName("")
      setTags([])
      setTagInput("")
    }
  }, [project])

  // Close tag dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (showTagDropdown) {
        const target = event.target as Element
        // Updated to check against a more specific class
        if (!target.closest(".tag-input-container")) {
          setShowTagDropdown(false)
        }
      }
    }

    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [showTagDropdown])

  const handleSave = async () => {
    if (!project) return
    if (!name.trim()) {
      toast.error("Project name cannot be empty.")
      return
    }

    // Check if another project with the same name already exists
    const existingProjects = await storageAdapter.list()
    const trimmedName = name.trim()
    const conflictingProject = existingProjects.find(
      (p) => p.name === trimmedName && p.id !== project.id,
    )

    if (conflictingProject) {
      toast.error("Project name already exists", {
        description: `Another project is already named "${trimmedName}". Please choose a different name.`,
        duration: 5000,
        icon: "🚫",
      })
      return
    }

    try {
      await onSave(project.id, trimmedName, tags)
      onOpenChange(false)
    } catch (error) {
      // Error is handled in the onSave implementation, but we can add a fallback
      console.error("Failed to save project info:", error)
    }
  }

  const handleTagInputKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter" && tagInput.trim()) {
      e.preventDefault()
      const newTag = tagInput.trim()
      if (newTag && !tags.includes(newTag)) {
        setTags([...tags, newTag])
      }
      setTagInput("")
      setShowTagDropdown(false)
    }
  }

  const removeTag = (tagToRemove: string) => {
    setTags(tags.filter((tag) => tag !== tagToRemove))
  }

  if (!project) return null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Edit Project Information</DialogTitle>
          <DialogDescription>Update the name and tags for your project.</DialogDescription>
        </DialogHeader>
        <div className="grid gap-4 py-4">
          <div className="grid grid-cols-4 items-center gap-4">
            <Label htmlFor="name" className="text-right">
              Name
            </Label>
            <Input id="name" value={name} onChange={(e) => setName(e.target.value)} className="col-span-3" />
          </div>
          <div className="grid grid-cols-4 items-center gap-4">
            <Label htmlFor="tags" className="text-right">
              Tags
            </Label>
            <div className="col-span-3">
              {/* Tag Input with Dropdown */}
              <div className="relative tag-input-container">
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
                          handleTagInputKeyDown(e)
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
                        className={`h-4 w-4 transition-transform ${showTagDropdown ? "rotate-180" : ""}`}
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                      </svg>
                    </button>
                  </div>
                </div>

                {/* Dropdown with existing tags */}
                {showTagDropdown && (
                  <div className="absolute z-10 mt-1 w-full rounded-md border bg-white shadow-lg dark:border-gray-700 dark:bg-gray-800 max-h-48 overflow-y-auto">
                    {(() => {
                      const filteredTags = tagInput.trim()
                        ? availableTags.filter(
                            (tag) =>
                              tag.name.toLowerCase().includes(tagInput.toLowerCase()) && !tags.includes(tag.name),
                          )
                        : availableTags.filter((tag) => !tags.includes(tag.name))

                      return (
                        <>
                          {/* Show filtered existing tags */}
                          {filteredTags.length > 0 && (
                            <>
                              {filteredTags.slice(0, 10).map((tag) => (
                                <button
                                  key={tag.name}
                                  type="button"
                                  onClick={() => {
                                    setTags((prev) => [...prev, tag.name])
                                    setTagInput("")
                                    setShowTagDropdown(false)
                                  }}
                                  className="flex w-full items-center justify-between px-3 py-2 text-left hover:bg-gray-100 dark:hover:bg-gray-700"
                                >
                                  <span className="text-sm">{tag.name}</span>
                                </button>
                              ))}
                              {tagInput.trim() &&
                                !availableTags.some((tag) => tag.name.toLowerCase() === tagInput.toLowerCase()) &&
                                !tags.includes(tagInput.trim()) && (
                                  <div className="my-1 border-t dark:border-gray-700"></div>
                                )}
                            </>
                          )}

                          {/* Create new tag option */}
                          {tagInput.trim() &&
                            !availableTags.some((tag) => tag.name.toLowerCase() === tagInput.toLowerCase()) &&
                            !tags.includes(tagInput.trim()) && (
                              <button
                                type="button"
                                onClick={() => {
                                  const newTag = tagInput.trim()
                                  setTags((prev) => [...prev, newTag])
                                  setTagInput("")
                                  setShowTagDropdown(false)
                                }}
                                className="w-full px-3 py-2 text-left hover:bg-gray-100 dark:hover:bg-gray-700"
                              >
                                <div className="flex items-center gap-2">
                                  <svg
                                    className="h-4 w-4 text-green-600 dark:text-green-400"
                                    fill="none"
                                    stroke="currentColor"
                                    viewBox="0 0 24 24"
                                  >
                                    <path
                                      strokeLinecap="round"
                                      strokeLinejoin="round"
                                      strokeWidth={2}
                                      d="M12 4v16m8-8H4"
                                    />
                                  </svg>
                                  <span className="text-sm">
                                    Create "<strong>{tagInput.trim()}</strong>"
                                  </span>
                                </div>
                              </button>
                            )}
                        </>
                      )
                    })()}
                  </div>
                )}
              </div>

              {/* Selected Tags */}
              {tags.length > 0 && (
                <div className="mt-2 flex flex-wrap gap-2">
                  {tags.map((tag) => (
                    <span
                      key={tag}
                      className="inline-flex items-center gap-1 rounded-full bg-blue-100 px-2 py-1 text-xs text-blue-800 dark:bg-blue-900 dark:text-blue-200"
                    >
                      {tag}
                      <button
                        type="button"
                        onClick={() => removeTag(tag)}
                        className="ml-1 rounded-full hover:bg-blue-200 dark:hover:bg-blue-800"
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </span>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSave}>Save Changes</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
