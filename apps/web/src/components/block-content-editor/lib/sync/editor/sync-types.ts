/**
 * Canonical Sync Types.
 *
 * Single source of truth for sync-related shapes consumed across the editor
 * (status indicator UI, project storage hook, sync manager).
 */

export interface SyncQueueStats {
  pending: number
  processing: number
  completed: number
  failed: number
  total: number
}

export interface SyncStats {
  isOnline: boolean
  isSyncing: boolean
  lastSync: string | null
  queue: SyncQueueStats
}
