"use client"

/**
 * useProjectSync
 *
 * Polls / subscribes to the {@link SyncManager} via the storage adapter,
 * exposing the current {@link SyncStats}. Also owns the user-toggleable
 * `autoSaveEnabled` flag (the auto-save effect itself lives in
 * `useProjectStorage` since it depends on cross-cutting state).
 */

import { useEffect, useState, type Dispatch, type SetStateAction } from "react"
import { toast } from "sonner"
import type { EnhancedStorageAdapter } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import type { SyncStats } from "@/components/block-content-editor/lib/sync/editor/sync-types"

export interface UseProjectSyncReturn {
  syncStats: SyncStats | null
  autoSaveEnabled: boolean
  setAutoSaveEnabled: Dispatch<SetStateAction<boolean>>
}

export function useProjectSync(
  db: EnhancedStorageAdapter,
  isDbInitialized: boolean,
): UseProjectSyncReturn {
  const [syncStats, setSyncStats] = useState<SyncStats | null>(null)
  const [autoSaveEnabled, setAutoSaveEnabled] = useState(false)

  useEffect(() => {
    if (!isDbInitialized) return

    const updateSyncStats = async () => {
      try { setSyncStats(await db.getSyncStats()) }
      catch (error) { console.error("Failed to get sync stats:", error) }
    }

    const interval = setInterval(updateSyncStats, 5000)
    updateSyncStats()

    db.onSyncStart(() => { updateSyncStats() })
    db.onSyncComplete((stats: SyncStats) => {
      updateSyncStats()
      if (stats.queue.processing > 0 || stats.queue.completed > 0) {
        toast.success("Synchronization completed", {
          description: `${stats.queue.completed} synchronized projects`,
          duration: 3000, icon: "🔄",
        })
      }
    })
    db.onSyncError(() => {
      updateSyncStats()
      toast.error("Synchronization error", {
        description: "Some projects may not be synchronized",
        duration: 4000, icon: "⚠️",
      })
    })

    return () => { clearInterval(interval) }
  }, [db, isDbInitialized])

  return { syncStats, autoSaveEnabled, setAutoSaveEnabled }
}
