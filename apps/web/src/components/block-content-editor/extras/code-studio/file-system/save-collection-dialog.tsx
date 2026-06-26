"use client"

import { useState, useEffect } from "react"
import { Package, Tag, AlertCircle } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Badge } from "@/components/ui/badge"
import { Alert, AlertDescription } from "@/components/ui/alert"

interface SaveCollectionDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSave: (params: { name: string; description?: string; tags: string[] }) => Promise<{ success: boolean; error?: string }>
  folderName?: string
}

export function SaveCollectionDialog({ open, onOpenChange, onSave, folderName }: SaveCollectionDialogProps) {
  const [name, setName] = useState(folderName || "")
  const [description, setDescription] = useState("")
  const [tagInput, setTagInput] = useState("")
  const [tags, setTags] = useState<string[]>([])
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Update name when folderName changes
  useEffect(() => {
    if (folderName) {
      setName(folderName)
    }
  }, [folderName])

  const handleAddTag = () => {
    const tag = tagInput.trim()
    if (tag && !tags.includes(tag)) {
      setTags([...tags, tag])
      setTagInput("")
    }
  }

  const handleRemoveTag = (tagToRemove: string) => {
    setTags(tags.filter((t) => t !== tagToRemove))
  }

  const handleSave = async () => {
    if (!name.trim()) {
      setError("Collection name is required")
      return
    }

    setIsSaving(true)
    setError(null)

    try {
      const result = await onSave({
        name: name.trim(),
        description: description.trim() || undefined,
        tags,
      })

      if (result.success) {
        // Reset form
        setName("")
        setDescription("")
        setTags([])
        setTagInput("")
        onOpenChange(false)
      } else {
        setError(result.error || "Failed to save collection")
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unknown error")
    } finally {
      setIsSaving(false)
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") {
      e.preventDefault()
      handleAddTag()
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Package className="h-5 w-5" />
            Save as Collection
          </DialogTitle>
          <DialogDescription>
            Create a reusable asset collection from your current project. Only files with asset references will be
            included.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <Label htmlFor="name">Collection Name *</Label>
            <Input
              id="name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="My Project Assets"
              disabled={isSaving}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Describe what this collection contains..."
              rows={3}
              disabled={isSaving}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="tags" className="flex items-center gap-2">
              <Tag className="h-4 w-4" />
              Tags
            </Label>
            <div className="flex gap-2">
              <Input
                id="tags"
                value={tagInput}
                onChange={(e) => setTagInput(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="Add tag (press Enter)"
                disabled={isSaving}
              />
              <Button type="button" variant="outline" onClick={handleAddTag} disabled={isSaving}>
                Add
              </Button>
            </div>
            {tags.length > 0 && (
              <div className="flex flex-wrap gap-2 mt-2">
                {tags.map((tag) => (
                  <Badge key={tag} variant="secondary" className="cursor-pointer" onClick={() => handleRemoveTag(tag)}>
                    {tag} ×
                  </Badge>
                ))}
              </div>
            )}
          </div>

          {error && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isSaving}>
            Cancel
          </Button>
          <Button onClick={handleSave} disabled={isSaving || !name.trim()}>
            {isSaving ? "Saving..." : "Save Collection"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
