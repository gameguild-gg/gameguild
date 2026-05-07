"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Badge } from "@/components/ui/badge"
import { 
  History, 
  Tag, 
  Eye, 
  RotateCcw, 
  Camera, 
  Clock,
  GitCommit,
  Loader2
} from "lucide-react"
import { toast } from "sonner"
import type { CommitInfo, SnapshotInfo } from "@/components/block-content-editor/lib/storage/git"

interface ProjectHistoryDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  projectId: string
  projectName: string
  isViewingHistory: boolean
  currentViewingSha: string | null
  onLoadCommit: (sha: string) => Promise<void>
  onLoadSnapshot: (tag: string) => Promise<void>
  onReturnToHead: () => Promise<void>
  onCreateSnapshot: (name?: string) => Promise<void>
  listHistory: (projectId: string) => Promise<CommitInfo[]>
  listSnapshots: (projectId: string) => Promise<SnapshotInfo[]>
}

export function ProjectHistoryDialog({
  open,
  onOpenChange,
  projectId,
  projectName,
  isViewingHistory,
  currentViewingSha,
  onLoadCommit,
  onLoadSnapshot,
  onReturnToHead,
  onCreateSnapshot,
  listHistory,
  listSnapshots,
}: ProjectHistoryDialogProps) {
  const [commits, setCommits] = useState<CommitInfo[]>([])
  const [snapshots, setSnapshots] = useState<SnapshotInfo[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [isCreatingSnapshot, setIsCreatingSnapshot] = useState(false)
  const [snapshotName, setSnapshotName] = useState("")
  const [showCreateSnapshot, setShowCreateSnapshot] = useState(false)
  const [loadingItem, setLoadingItem] = useState<string | null>(null)

  // Load history and snapshots when dialog opens
  useEffect(() => {
    if (open && projectId) {
      loadData()
    }
  }, [open, projectId])

  const loadData = async () => {
    setIsLoading(true)
    try {
      const [historyData, snapshotsData] = await Promise.all([
        listHistory(projectId),
        listSnapshots(projectId)
      ])
      setCommits(historyData)
      setSnapshots(snapshotsData)
    } catch (error) {
      console.error("Failed to load history:", error)
      toast.error("Failed to load history")
    } finally {
      setIsLoading(false)
    }
  }

  const handleLoadCommit = async (sha: string) => {
    setLoadingItem(sha)
    try {
      await onLoadCommit(sha)
      onOpenChange(false)
    } catch (error) {
      console.error("Failed to load commit:", error)
      toast.error("Failed to load commit")
    } finally {
      setLoadingItem(null)
    }
  }

  const handleLoadSnapshot = async (tag: string) => {
    setLoadingItem(tag)
    try {
      await onLoadSnapshot(tag)
      onOpenChange(false)
    } catch (error) {
      console.error("Failed to load snapshot:", error)
      toast.error("Failed to load snapshot")
    } finally {
      setLoadingItem(null)
    }
  }

  const handleReturnToHead = async () => {
    setLoadingItem("head")
    try {
      await onReturnToHead()
      onOpenChange(false)
    } catch (error) {
      console.error("Failed to return to HEAD:", error)
      toast.error("Failed to return to latest version")
    } finally {
      setLoadingItem(null)
    }
  }

  const handleCreateSnapshot = async () => {
    setIsCreatingSnapshot(true)
    try {
      await onCreateSnapshot(snapshotName.trim() || undefined)
      setSnapshotName("")
      setShowCreateSnapshot(false)
      await loadData() // Refresh the list
      toast.success("Snapshot created successfully")
    } catch (error) {
      console.error("Failed to create snapshot:", error)
      toast.error(error instanceof Error ? error.message : "Failed to create snapshot")
    } finally {
      setIsCreatingSnapshot(false)
    }
  }

  const formatDate = (dateString: string) => {
    const date = new Date(dateString)
    return date.toLocaleString()
  }

  const formatRelativeTime = (dateString: string) => {
    const date = new Date(dateString)
    const now = new Date()
    const diffMs = now.getTime() - date.getTime()
    const diffMins = Math.floor(diffMs / 60000)
    const diffHours = Math.floor(diffMs / 3600000)
    const diffDays = Math.floor(diffMs / 86400000)

    if (diffMins < 1) return "just now"
    if (diffMins < 60) return `${diffMins}m ago`
    if (diffHours < 24) return `${diffHours}h ago`
    if (diffDays < 7) return `${diffDays}d ago`
    return date.toLocaleDateString()
  }

  // Find if a commit has a snapshot tag
  const getSnapshotForCommit = (sha: string): SnapshotInfo | undefined => {
    return snapshots.find(s => s.sha === sha)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[600px] max-h-[80vh]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <History className="h-5 w-5" />
            Project History
          </DialogTitle>
          <DialogDescription>
            View commit history and snapshots for "{projectName}"
          </DialogDescription>
        </DialogHeader>

        {/* Current State Banner */}
        {isViewingHistory && (
          <div className="flex items-center justify-between p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg">
            <div className="flex items-center gap-2 text-amber-800 dark:text-amber-200">
              <Eye className="h-4 w-4" />
              <span className="text-sm font-medium">
                Viewing historical version (read-only)
              </span>
            </div>
            <Button
              size="sm"
              variant="outline"
              onClick={handleReturnToHead}
              disabled={loadingItem === "head"}
              className="gap-1"
            >
              {loadingItem === "head" ? (
                <Loader2 className="h-3 w-3 animate-spin" />
              ) : (
                <RotateCcw className="h-3 w-3" />
              )}
              Return to Latest
            </Button>
          </div>
        )}

        <Tabs defaultValue="history" className="w-full">
          <TabsList className="grid w-full grid-cols-2">
            <TabsTrigger value="history" className="gap-2">
              <GitCommit className="h-4 w-4" />
              History ({commits.length})
            </TabsTrigger>
            <TabsTrigger value="snapshots" className="gap-2">
              <Tag className="h-4 w-4" />
              Snapshots ({snapshots.length})
            </TabsTrigger>
          </TabsList>

          <TabsContent value="history" className="mt-4">
            {isLoading ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              </div>
            ) : commits.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                <GitCommit className="h-8 w-8 mx-auto mb-2 opacity-50" />
                <p>No commit history yet</p>
                <p className="text-sm">Save the project to create history</p>
              </div>
            ) : (
              <ScrollArea className="h-[300px] pr-4">
                <div className="space-y-2">
                  {commits.map((commit, index) => {
                    const snapshot = getSnapshotForCommit(commit.sha)
                    const isHead = index === 0
                    const isCurrentlyViewing = currentViewingSha === commit.sha

                    return (
                      <div
                        key={commit.sha}
                        className={`p-3 border rounded-lg transition-colors ${
                          isCurrentlyViewing
                            ? "border-blue-500 bg-blue-50 dark:bg-blue-900/20"
                            : "border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800"
                        }`}
                      >
                        <div className="flex items-start justify-between gap-2">
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2 mb-1">
                              <code className="text-xs text-muted-foreground font-mono">
                                {commit.sha.substring(0, 7)}
                              </code>
                              {isHead && (
                                <Badge variant="default" className="text-xs">
                                  HEAD
                                </Badge>
                              )}
                              {snapshot && (
                                <Badge variant="secondary" className="text-xs gap-1">
                                  <Tag className="h-3 w-3" />
                                  {snapshot.tag}
                                </Badge>
                              )}
                              {isCurrentlyViewing && (
                                <Badge variant="outline" className="text-xs">
                                  Viewing
                                </Badge>
                              )}
                            </div>
                            <p className="text-sm text-gray-700 dark:text-gray-300 truncate">
                              {commit.message}
                            </p>
                            <div className="flex items-center gap-1 mt-1 text-xs text-muted-foreground">
                              <Clock className="h-3 w-3" />
                              <span title={formatDate(commit.date)}>
                                {formatRelativeTime(commit.date)}
                              </span>
                            </div>
                          </div>
                          <Button
                            size="sm"
                            variant={isCurrentlyViewing ? "secondary" : "outline"}
                            onClick={() => handleLoadCommit(commit.sha)}
                            disabled={loadingItem !== null || isCurrentlyViewing}
                            className="shrink-0"
                          >
                            {loadingItem === commit.sha ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <Eye className="h-4 w-4" />
                            )}
                          </Button>
                        </div>
                      </div>
                    )
                  })}
                </div>
              </ScrollArea>
            )}
          </TabsContent>

          <TabsContent value="snapshots" className="mt-4">
            {/* Create Snapshot Section */}
            {!isViewingHistory && (
              <div className="mb-4">
                {showCreateSnapshot ? (
                  <div className="p-3 border border-dashed border-gray-300 dark:border-gray-600 rounded-lg space-y-3">
                    <div className="space-y-2">
                      <Label htmlFor="snapshot-name">Snapshot Name (optional)</Label>
                      <Input
                        id="snapshot-name"
                        placeholder={`${projectName}-v1`}
                        value={snapshotName}
                        onChange={(e) => setSnapshotName(e.target.value)}
                        disabled={isCreatingSnapshot}
                      />
                      <p className="text-xs text-muted-foreground">
                        Leave empty to auto-generate a name
                      </p>
                    </div>
                    <div className="flex gap-2">
                      <Button
                        size="sm"
                        onClick={handleCreateSnapshot}
                        disabled={isCreatingSnapshot}
                        className="gap-1"
                      >
                        {isCreatingSnapshot ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Camera className="h-4 w-4" />
                        )}
                        Create
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => {
                          setShowCreateSnapshot(false)
                          setSnapshotName("")
                        }}
                        disabled={isCreatingSnapshot}
                      >
                        Cancel
                      </Button>
                    </div>
                  </div>
                ) : (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setShowCreateSnapshot(true)}
                    className="w-full gap-2 border-dashed"
                  >
                    <Camera className="h-4 w-4" />
                    Create Snapshot from Current State
                  </Button>
                )}
              </div>
            )}

            {isLoading ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              </div>
            ) : snapshots.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                <Tag className="h-8 w-8 mx-auto mb-2 opacity-50" />
                <p>No snapshots yet</p>
                <p className="text-sm">Create a snapshot to save a version</p>
              </div>
            ) : (
              <ScrollArea className="h-[250px] pr-4">
                <div className="space-y-2">
                  {snapshots.map((snapshot) => {
                    const isCurrentlyViewing = currentViewingSha === snapshot.sha

                    return (
                      <div
                        key={snapshot.tag}
                        className={`p-3 border rounded-lg transition-colors ${
                          isCurrentlyViewing
                            ? "border-blue-500 bg-blue-50 dark:bg-blue-900/20"
                            : "border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800"
                        }`}
                      >
                        <div className="flex items-start justify-between gap-2">
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2 mb-1">
                              <Badge variant="secondary" className="gap-1">
                                <Tag className="h-3 w-3" />
                                {snapshot.tag}
                              </Badge>
                              {isCurrentlyViewing && (
                                <Badge variant="outline" className="text-xs">
                                  Viewing
                                </Badge>
                              )}
                            </div>
                            <p className="text-sm text-gray-700 dark:text-gray-300 truncate">
                              {snapshot.message}
                            </p>
                            <div className="flex items-center gap-2 mt-1 text-xs text-muted-foreground">
                              <code className="font-mono">{snapshot.sha.substring(0, 7)}</code>
                              <span>•</span>
                              <span title={formatDate(snapshot.date)}>
                                {formatRelativeTime(snapshot.date)}
                              </span>
                            </div>
                          </div>
                          <Button
                            size="sm"
                            variant={isCurrentlyViewing ? "secondary" : "outline"}
                            onClick={() => handleLoadSnapshot(snapshot.tag)}
                            disabled={loadingItem !== null || isCurrentlyViewing}
                            className="shrink-0"
                          >
                            {loadingItem === snapshot.tag ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <Eye className="h-4 w-4" />
                            )}
                          </Button>
                        </div>
                      </div>
                    )
                  })}
                </div>
              </ScrollArea>
            )}
          </TabsContent>
        </Tabs>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Close
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
