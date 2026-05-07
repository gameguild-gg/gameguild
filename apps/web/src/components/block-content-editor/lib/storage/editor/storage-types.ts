/**
 * Storage Types System
 * Centralized storage type definitions
 */

export const STORAGE_TYPES = {
  LOCAL: 'local',
  GAMEGUILD_CLOUD: 'gameguild-cloud',
  GOOGLE_DRIVE: 'google-drive',
} as const

export type StorageType = typeof STORAGE_TYPES[keyof typeof STORAGE_TYPES]

/**
 * Sync Status System
 */

export const SYNC_STATUS = {
  SYNCED: 'synced',
  PENDING: 'pending',
  CONFLICT: 'conflict',
  LOCAL_ONLY: 'local-only',
} as const

export type SyncStatus = typeof SYNC_STATUS[keyof typeof SYNC_STATUS]
