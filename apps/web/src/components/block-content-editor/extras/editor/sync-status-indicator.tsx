"use client"

import type { SyncStats } from "@/components/block-content-editor/lib/sync/editor/sync-types"

interface SyncStatusIndicatorProps {
  syncStats: Pick<SyncStats, "isOnline" | "isSyncing" | "queue">
  isSyncEnabled: boolean
  onClick: () => void
}

export function SyncStatusIndicator({ 
  syncStats, 
  isSyncEnabled, 
  onClick 
}: SyncStatusIndicatorProps) {
  return (
    <button
      onClick={onClick}
      className="flex items-center gap-2 bg-gray-50 px-3 py-1.5 transition-colors hover:bg-gray-100 dark:bg-gray-800 dark:hover:bg-gray-700"
    >
      <div
        className={`h-2 w-2 rounded-full ${
          syncStats.isOnline
            ? syncStats.isSyncing
              ? "bg-blue-500 animate-pulse"
              : "bg-green-500"
            : isSyncEnabled
              ? "bg-red-500"
              : "bg-gray-400"
        }`}
      />
      <span className="text-xs font-medium text-gray-600 dark:text-gray-300">
        {!isSyncEnabled
          ? "Sync Off"
          : syncStats.isOnline
            ? syncStats.isSyncing
              ? "Syncing..."
              : "Synced"
            : "Offline"}
      </span>
      {syncStats.queue.pending > 0 && (
        <span className="bg-blue-100 px-1.5 py-0.5 text-xs font-medium text-blue-800 dark:bg-blue-900 dark:text-blue-200">
          {syncStats.queue.pending}
        </span>
      )}
    </button>
  )
}
