import { Content } from "@/types";

export type SyncStatus = 'idle' | 'syncing' | 'success' | 'error' | 'partial';

export type ConflictResolution = 'local-first' | 'remote-first' | 'manual' | 'merge';

export type OperationType = 'create' | 'update' | 'delete';

export interface SyncOperation {
  id: string;
  type: OperationType;
  contentId?: string;
  data: Partial<Content>;
  timestamp: number;
  retryCount: number;
  status: 'pending' | 'syncing' | 'completed' | 'failed';
  error?: Error;
}

export interface SyncQueue {
  operations: SyncOperation[];
  maxRetries: number;
}

export interface CacheEntry<T = Content> {
  data: T;
  timestamp: number;
  dirty: boolean;
}

export interface MemoryCache {
  idToContent: Map<string, Content>;
  slugToContent: Map<string, Content>;
  dirtyChanges: Map<string, Partial<Content>>;
}

export interface StorageStats {
  memorySize: number;
  dirtyChanges: number;
  queueSize: number;
  localStorageCount: number;
  lastSyncTime: number;
}

export interface ConflictInfo {
  local: Content;
  remote: Content;
  timestamp: number;
  autoResolution?: 'local' | 'remote';
}