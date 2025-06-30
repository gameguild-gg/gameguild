import { Identifiable } from "@/lib/types";

export const SyncOperationType = {
  CREATE: 'create',
  UPDATE: 'update',
  DELETE: 'delete',
};

export type SyncOperationType = (typeof SyncOperationType)[keyof typeof SyncOperationType];

export const ConflictResolutionMode = {
  LOCAL_FIRST: 'local-first',
  REMOTE_FIRST: 'remote-first',
  MANUAL: 'manual',
  MERGE: 'merge'
}

export const ConflictAutoResolutionMode = {
  LOCAL: 'local',
  REMOTE: 'remote',
}
export type ConflictAutoResolutionMode = (typeof ConflictAutoResolutionMode)[keyof typeof ConflictAutoResolutionMode];

export type ConflictResolutionMode = (typeof ConflictResolutionMode)[keyof typeof ConflictResolutionMode];

export const SyncOperationStatus = {
  PENDING: 'pending',
  SYNCING: 'syncing',
  COMPLETED: 'completed',
  FAILED: 'failed',
}

export type SyncOperationStatus = (typeof SyncOperationStatus)[keyof typeof SyncOperationStatus];

export interface SyncOperation<T = Identifiable> {
  id: string;
  type: SyncOperationType;
  itemId?: string;
  data: Partial<T>;
  timestamp: number;
  retryCount: number;
  status: SyncOperationStatus;
  error?: Error;
}

export interface SyncQueue {
  operations: SyncOperation[];
  maxRetries: number;
}

export interface CacheEntry<T = Identifiable> {
  data: T;
  timestamp: number;
  dirty: boolean;
}

export interface MemoryCache<T = Identifiable> {
  items: Map<string, T>;
  dirtyChanges: Map<string, Partial<T>>;
}

export interface StorageStats {
  memorySize: number;
  dirtyChanges: number;
  queueSize: number;
  localStorageCount: number;
  lastSyncTime: number;
}

export interface ConflictInfo<T = Identifiable> {
  local: T;
  remote: T;
  timestamp: number;
  autoResolutionMode?: ConflictAutoResolutionMode;
}