"use client"

import { useState, useEffect } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group"
import { Label } from "@/components/ui/label"
import { Import, GitBranch, Tag } from "lucide-react"

interface ProjectListItem {
  id: string
  name: string
  type?: string
  updatedAt: string
}

interface ProjectImportDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  storageAdapter: {
    list: () => Promise<Array<ProjectListItem>>
    listSnapshots?: (id: string) => Promise<Array<{ tag: string; sha: string; date: string }>>
  }
  /** Called when the user confirms the import */
  onConfirm: (projectId: string, loadMode: 'snapshot' | 'head', snapshotTag?: string) => void
  /** Current project ID to exclude from the list */
  currentProjectId?: string
}

export function ProjectImportDialog({
  open,
  onOpenChange,
  storageAdapter,
  onConfirm,
  currentProjectId,
}: ProjectImportDialogProps) {
  const [projects, setProjects] = useState<ProjectListItem[]>([])
  const [selectedProjectId, setSelectedProjectId] = useState<string | null>(null)
  const [loadMode, setLoadMode] = useState<'head' | 'snapshot'>('head')
  const [snapshots, setSnapshots] = useState<Array<{ tag: string; sha: string; date: string }>>([])
  const [selectedSnapshot, setSelectedSnapshot] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  // Load type2 projects
  useEffect(() => {
    if (!open) return
    setLoading(true)
    storageAdapter.list().then((all) => {
      // Filter to type2 projects only, excluding the current type3 project
      const type2Projects = all.filter(
        (p) => p.type === 'type2' && p.id !== currentProjectId
      )
      setProjects(type2Projects)
      setLoading(false)
    }).catch(() => setLoading(false))
  }, [open, storageAdapter, currentProjectId])

  // Load snapshots when a project is selected and mode is snapshot
  useEffect(() => {
    if (!selectedProjectId || loadMode !== 'snapshot' || !storageAdapter.listSnapshots) {
      setSnapshots([])
      return
    }
    storageAdapter.listSnapshots(selectedProjectId).then(setSnapshots).catch(() => setSnapshots([]))
  }, [selectedProjectId, loadMode, storageAdapter])

  const handleConfirm = () => {
    if (!selectedProjectId) return
    onConfirm(
      selectedProjectId,
      loadMode,
      loadMode === 'snapshot' ? selectedSnapshot || undefined : undefined
    )
    // Reset state
    setSelectedProjectId(null)
    setLoadMode('head')
    setSelectedSnapshot(null)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Import className="h-5 w-5" />
            Import Type2 Project
          </DialogTitle>
        </DialogHeader>

        {loading ? (
          <div className="py-8 text-center text-gray-500">Loading projects...</div>
        ) : projects.length === 0 ? (
          <div className="py-8 text-center text-gray-500">
            No type2 projects found. Create a type2 project first.
          </div>
        ) : (
          <div className="space-y-4">
            {/* Project list */}
            <div className="space-y-2">
              <Label className="text-sm font-medium">Select a project</Label>
              <div className="max-h-48 overflow-y-auto border rounded-md divide-y">
                {projects.map((project) => (
                  <button
                    key={project.id}
                    type="button"
                    className={`w-full text-left px-3 py-2 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors ${
                      selectedProjectId === project.id
                        ? 'bg-blue-50 dark:bg-blue-950 border-l-2 border-l-blue-500'
                        : ''
                    }`}
                    onClick={() => {
                      setSelectedProjectId(project.id)
                      setSelectedSnapshot(null)
                    }}
                  >
                    <div className="flex items-center justify-between">
                      <span className="font-medium text-sm">{project.name}</span>
                      <Badge variant="outline" className="text-xs">type2</Badge>
                    </div>
                    <div className="text-xs text-gray-500 mt-0.5">
                      {project.id.substring(0, 8)}... &middot; {new Date(project.updatedAt).toLocaleDateString()}
                    </div>
                  </button>
                ))}
              </div>
            </div>

            {/* Load mode selection */}
            {selectedProjectId && (
              <div className="space-y-3">
                <Label className="text-sm font-medium">Load mode</Label>
                <RadioGroup
                  value={loadMode}
                  onValueChange={(val) => setLoadMode(val as 'head' | 'snapshot')}
                  className="space-y-2"
                >
                  <div className="flex items-center gap-2">
                    <RadioGroupItem value="head" id="load-head" />
                    <Label htmlFor="load-head" className="flex items-center gap-1.5 text-sm cursor-pointer">
                      <GitBranch className="h-3.5 w-3.5" />
                      Head (always show latest version)
                    </Label>
                  </div>
                  <div className="flex items-center gap-2">
                    <RadioGroupItem value="snapshot" id="load-snapshot" />
                    <Label htmlFor="load-snapshot" className="flex items-center gap-1.5 text-sm cursor-pointer">
                      <Tag className="h-3.5 w-3.5" />
                      Snapshot (pin to a specific version)
                    </Label>
                  </div>
                </RadioGroup>

                {/* Snapshot selection */}
                {loadMode === 'snapshot' && (
                  <div className="ml-6 space-y-2">
                    {snapshots.length === 0 ? (
                      <div className="text-xs text-gray-500 py-2">
                        No snapshots available for this project.
                      </div>
                    ) : (
                      <div className="max-h-32 overflow-y-auto border rounded-md divide-y">
                        {snapshots.map((snap) => (
                          <button
                            key={snap.tag}
                            type="button"
                            className={`w-full text-left px-3 py-1.5 text-sm hover:bg-gray-50 dark:hover:bg-gray-800 ${
                              selectedSnapshot === snap.tag
                                ? 'bg-blue-50 dark:bg-blue-950'
                                : ''
                            }`}
                            onClick={() => setSelectedSnapshot(snap.tag)}
                          >
                            <div className="flex items-center justify-between">
                              <span className="flex items-center gap-1">
                                <Tag className="h-3 w-3" />
                                {snap.tag}
                              </span>
                              <span className="text-xs text-gray-500">
                                {new Date(snap.date).toLocaleDateString()}
                              </span>
                            </div>
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </div>
            )}

            {/* Actions */}
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={() => onOpenChange(false)}>
                Cancel
              </Button>
              <Button
                onClick={handleConfirm}
                disabled={
                  !selectedProjectId ||
                  (loadMode === 'snapshot' && !selectedSnapshot)
                }
              >
                <Import className="h-4 w-4 mr-1" />
                Import
              </Button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
