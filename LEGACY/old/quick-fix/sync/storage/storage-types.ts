/**
 * Client-Side Storage Adapter Types
 * Defines interfaces for different client-side storage adapters
 */

export type StorageAdapterType = 'localStorage' | 'sessionStorage' | 'indexedDB' | 'cache' | 'memory';

export interface StorageAdapterConfig {
  type: StorageAdapterType;
  name?: string;
  version?: number;
  maxSize?: number;
  ttl?: number; // Time to live in milliseconds
  compression?: boolean;
  encryption?: boolean;
}

export interface StorageAdapterResult<T = unknown> {
  success: boolean;
  data?: T;
  error?: Error;
  fromCache?: boolean;
}

export interface StorageAdapterStats {
  type: StorageAdapterType;
  size: number;
  itemCount: number;
  available: boolean;
  maxSize?: number;
  usedPercentage?: number;
}

/**
 * Base interface for all client-side storage adapters
 */
export interface ClientStorageAdapter {
  readonly type: StorageAdapterType;
  readonly config: StorageAdapterConfig;

  // Core operations
  get<T = unknown>(key: string): Promise<StorageAdapterResult<T>>;
  set<T = unknown>(key: string, value: T, ttl?: number): Promise<StorageAdapterResult<void>>;
  delete(key: string): Promise<StorageAdapterResult<boolean>>;
  clear(): Promise<StorageAdapterResult<void>>;
  has(key: string): Promise<boolean>;

  // Batch operations
  getMany<T = unknown>(keys: string[]): Promise<Map<string, T>>;
  setMany<T = unknown>(items: Map<string, T>, ttl?: number): Promise<Map<string, boolean>>;
  deleteMany(keys: string[]): Promise<Map<string, boolean>>;

  // Metadata operations
  keys(): Promise<string[]>;
  size(): Promise<number>;
  stats(): Promise<StorageAdapterStats>;

  // Lifecycle
  init(): Promise<void>;
  destroy(): Promise<void>;
  isAvailable(): Promise<boolean>;
}

/**
 * Enhanced storage item with metadata
 */
export interface StorageItem<T = unknown> {
  value: T;
  timestamp: number;
  ttl?: number;
  version?: number;
  compressed?: boolean;
  encrypted?: boolean;
  checksum?: string;
}

/**
 * Storage event types for observers
 */
export type StorageAdapterEventType = 'set' | 'delete' | 'clear' | 'error' | 'quota-exceeded';

export interface StorageAdapterEvent<T = unknown> {
  type: StorageAdapterEventType;
  key?: string;
  value?: T;
  adapter: StorageAdapterType;
  timestamp: number;
  error?: Error;
}

export type StorageAdapterEventHandler<T = unknown> = (event: StorageAdapterEvent<T>) => void | Promise<void>;

/**
 * Factory function type for creating storage adapters
 */
export type StorageAdapterFactory = (config: StorageAdapterConfig) => ClientStorageAdapter;

/**
 * Batch operation result
 */
export interface BatchStorageResult {
  success: boolean;
  results: Map<string, boolean>;
  errors: Map<string, Error>;
}
