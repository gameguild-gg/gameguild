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
  availableTags: Array<{ name: string; usageCount: number }>
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
              <div className="flex flex-wrap gap-2 rounded-md border p-2">
                {tags.map((tag) => (
                  <span
                    key={tag}
                    className="inline-flex items-center gap-1 rounded-full bg-blue-100 px-2 py-1 text-xs text-blue-800 dark:bg-blue-900 dark:text-blue-200"
                  >
                    {tag}
                    <button
                      type="button"
                      onClick={() => removeTag(tag)}
                      className="rounded-full hover:bg-blue-200 dark:hover:bg-blue-800"
                    >
                      <X className="h-3 w-3" />
                    </button>
                  </span>
                ))}
                <Input
                  id="tags"
                  placeholder="Add a tag..."
                  value={tagInput}
                  onChange={(e) => setTagInput(e.target.value)}
                  onKeyDown={handleTagInputKeyDown}
                  className="flex-1 border-none bg-transparent p-0 shadow-none focus-visible:ring-0"
                />
              </div>
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
