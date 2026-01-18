/**
 * Storage Types System
 * Centralized storage type definitions and configuration
 */

export const STORAGE_TYPES = {
  LOCAL: 'local',
  GAMEGUILD_CLOUD: 'gameguild-cloud',
  GOOGLE_DRIVE: 'google-drive',
} as const

export type StorageType = typeof STORAGE_TYPES[keyof typeof STORAGE_TYPES]

/**
 * Storage type configuration
 */
export const STORAGE_TYPE_CONFIG = {
  [STORAGE_TYPES.LOCAL]: {
    name: 'Local Storage',
    description: 'Stored locally in browser',
    requiresAuth: false,
    supportsSync: false,
  },
  [STORAGE_TYPES.GAMEGUILD_CLOUD]: {
    name: 'GameGuild Cloud',
    description: 'Synced to GameGuild cloud',
    requiresAuth: true,
    supportsSync: true,
  },
  [STORAGE_TYPES.GOOGLE_DRIVE]: {
    name: 'Google Drive',
    description: 'Synced to Google Drive',
    requiresAuth: true,
    supportsSync: true,
  },
} as const

/**
 * Get storage type configuration
 */
export function getStorageTypeConfig(type: StorageType) {
  return STORAGE_TYPE_CONFIG[type]
}

/**
 * Check if storage type requires authentication
 */
export function requiresAuthentication(type: StorageType): boolean {
  return STORAGE_TYPE_CONFIG[type].requiresAuth
}

/**
 * Check if storage type supports sync
 */
export function supportsSync(type: StorageType): boolean {
  return STORAGE_TYPE_CONFIG[type].supportsSync
}

/**
 * Sync Status System
 * Centralized sync status definitions
 */

export const SYNC_STATUS = {
  SYNCED: 'synced',
  PENDING: 'pending',
  CONFLICT: 'conflict',
  LOCAL_ONLY: 'local-only',
} as const

export type SyncStatus = typeof SYNC_STATUS[keyof typeof SYNC_STATUS]

/**
 * Sync status configuration
 */
export const SYNC_STATUS_CONFIG = {
  [SYNC_STATUS.SYNCED]: {
    label: 'Synced',
    description: 'Project is synced with remote',
    color: 'green',
  },
  [SYNC_STATUS.PENDING]: {
    label: 'Pending',
    description: 'Project has pending changes',
    color: 'yellow',
  },
  [SYNC_STATUS.CONFLICT]: {
    label: 'Conflict',
    description: 'Project has sync conflicts',
    color: 'red',
  },
  [SYNC_STATUS.LOCAL_ONLY]: {
    label: 'Local Only',
    description: 'Project is local only',
    color: 'gray',
  },
} as const

/**
 * Get sync status configuration
 */
export function getSyncStatusConfig(status: SyncStatus) {
  return SYNC_STATUS_CONFIG[status]
}
