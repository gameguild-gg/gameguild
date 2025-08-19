"use client"

import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { SaveAll } from 'lucide-react'

interface SaveAsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  projectName: string
  onProjectNameChange: (name: string) => void
  onSave: () => void
  currentProjectSize: number
  getSizeIndicatorColor: () => string
  formatSize: (size: number) => string
  isDbInitialized: boolean
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
  isDbInitialized
}: SaveAsDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm" className="gap-2 bg-transparent" disabled={!isDbInitialized}>
          <SaveAll className="w-4 h-4" />
          Save As
        </Button>
      </DialogTrigger>
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
              onKeyDown={(e) => e.key === "Enter" && onSave()}
              className="mt-1"
            />
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
            <Button onClick={onSave}>Save Project</Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
