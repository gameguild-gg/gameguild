"use client"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { toast } from "sonner"
import type { SyncStats } from "@/components/block-content-editor/lib/sync/editor/sync-types"

interface SyncStatusDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  syncStats: SyncStats | null
  onRetryFailed: () => Promise<void>
}

export function SyncStatusDialog({ 
  open, 
  onOpenChange, 
  syncStats,
  onRetryFailed 
}: SyncStatusDialogProps) {
  const handleRetryFailed = async () => {
    try {
      await onRetryFailed()
      toast.success("Trying again", {
        description: "Failed items have been re-queued",
        duration: 3000,
        icon: "🔄",
      })
    } catch (error) {
      toast.error("Error trying again", {
        description: "Unable to reprocess items",
        duration: 4000,
        icon: "❌",
      })
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Synchronization Status</DialogTitle>
        </DialogHeader>
        {syncStats && (
          <div className="space-y-4">
            <div className="flex items-center justify-between rounded-lg bg-gray-50 p-3 dark:bg-gray-800">
              <span className="text-sm text-gray-600 dark:text-gray-300">Connection:</span>
              <div className="flex items-center gap-2">
                <div className={`h-2 w-2 rounded-full ${syncStats.isOnline ? "bg-green-500" : "bg-red-500"}`} />
                <span className="text-sm font-medium">{syncStats.isOnline ? "Online" : "Offline"}</span>
              </div>
            </div>

            <div className="space-y-2">
              <h4 className="text-sm font-medium">Sync Queue</h4>
              <div className="grid grid-cols-2 gap-2 text-xs">
                <div className="rounded bg-blue-50 p-2 dark:bg-blue-900">
                  <div className="font-medium text-blue-800 dark:text-blue-200">Pending</div>
                  <div className="text-blue-600 dark:text-blue-300">{syncStats.queue.pending}</div>
                </div>
                <div className="rounded bg-yellow-50 p-2 dark:bg-yellow-900">
                  <div className="font-medium text-yellow-800 dark:text-yellow-200">Processing</div>
                  <div className="text-yellow-600 dark:text-yellow-300">{syncStats.queue.processing}</div>
                </div>
                <div className="rounded bg-green-50 p-2 dark:bg-green-900">
                  <div className="font-medium text-green-800 dark:text-green-200">Completed</div>
                  <div className="text-green-600 dark:text-green-300">{syncStats.queue.completed}</div>
                </div>
                <div className="rounded bg-red-50 p-2 dark:bg-red-900">
                  <div className="font-medium text-red-800 dark:text-red-200">Failed</div>
                  <div className="text-red-600 dark:text-red-300">{syncStats.queue.failed}</div>
                </div>
              </div>
            </div>

            {syncStats.lastSync && (
              <div className="rounded-lg bg-gray-50 p-3 dark:bg-gray-800">
                <span className="text-sm text-gray-600 dark:text-gray-300">Last sync:</span>
                <div className="text-sm font-medium">{new Date(syncStats.lastSync).toLocaleString()}</div>
              </div>
            )}

            {syncStats.queue.failed > 0 && (
              <Button
                onClick={handleRetryFailed}
                className="w-full"
                variant="outline"
              >
                Retry Failed Items
              </Button>
            )}

            <div className="flex justify-end">
              <Button variant="outline" onClick={() => onOpenChange(false)}>
                Close
              </Button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
