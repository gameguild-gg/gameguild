import { Readable, Writable } from 'stream';

export type BaseIdentifier = number | string;

export type Identifiable<T, TIdentifier extends BaseIdentifier = string> =
  | (T & { id: TIdentifier })
  | (T & { slug: string })
  | (T & { id: TIdentifier; slug: string });

// Base storage types
export interface StorageMetadata {
  id: string;
  type: string;
  size?: number;
  mimeType?: string;
  createdAt: string;
  updatedAt: string;
  etag?: string;
  checksum?: string;
}

export interface StorageOptions {
  bucket?: string;
  path?: string;
  publicUrl?: string;
  streaming?: boolean;
  compression?: boolean;
}

export interface StorageResult<T = unknown> {
  success: boolean;
  data?: T;
  metadata?: StorageMetadata;
  error?: Error;
}

export interface StreamOptions {
  start?: number;
  end?: number;
  highWaterMark?: number;
}

// Abstract storage adapter interface
export interface StorageAdapter<T = unknown> {
  // O(1) operations
  exists(id: string): Promise<boolean>;

  getMetadata(id: string): Promise<StorageMetadata | null>;

  // O(1) read/write operations
  read(id: string): Promise<T | null>;

  write(id: string, data: T, metadata?: Partial<StorageMetadata>): Promise<StorageResult<T>>;

  delete(id: string): Promise<boolean>;

  // O(n) operations
  list(prefix?: string, limit?: number, offset?: number): Promise<StorageMetadata[]>;

  // Streaming operations
  createReadStream?(id: string, options?: StreamOptions): Readable;

  createWriteStream?(id: string, metadata?: Partial<StorageMetadata>): Writable;

  // Batch operations - O(n) but optimized
  batchRead?(ids: string[]): Promise<Map<string, T>>;

  batchWrite?(items: Map<string, T>): Promise<Map<string, StorageResult<T>>>;

  batchDelete?(ids: string[]): Promise<Map<string, boolean>>;
}

// Content-specific types
export interface ContentStorage extends StorageMetadata {
  slug: string;
  title: string;
  type: string;
  content?: string;
  frontMatter?: Record<string, unknown>;
  connections?: string[];
  position?: { x: number; y: number };
}

// Image-specific types
export interface ImageMetadata extends StorageMetadata {
  width: number;
  height: number;
  format: string;
  thumbnails?: {
    small?: string;
    medium?: string;
    large?: string;
  };
}

export interface ImageStorage extends ImageMetadata {
  url: string;
  alt?: string;
  caption?: string;
}

// Index types for O(1) lookups
export interface StorageIndex<T = unknown> {
  byId: Map<string, T>;
  bySlug?: Map<string, T>;
  byType?: Map<string, Set<string>>;
  byDate?: Map<string, Set<string>>; // YYYY-MM-DD -> Set<id>
  metadata: Map<string, StorageMetadata>;
}

// Event types for observer pattern
export type StorageEventType = 'create' | 'update' | 'delete' | 'sync';

export interface StorageEvent<T = unknown> {
  type: StorageEventType;
  id: string;
  data?: T;
  metadata?: StorageMetadata;
  timestamp: number;
}

export type StorageEventHandler<T = unknown> = (event: StorageEvent<T>) => void | Promise<void>;

// Factory types
export type StorageAdapterFactory = (options: StorageOptions) => StorageAdapter;

// Repository pattern types
export interface Repository<T> {
  // O(1) operations
  findById(id: string): Promise<T | null>;

  findBySlug?(slug: string): Promise<T | null>;

  // O(n) operations - optimized with indices
  findAll(options?: { type?: string; limit?: number; offset?: number }): Promise<T[]>;

  findByType(type: string): Promise<T[]>;

  findByDateRange(start: Date, end: Date): Promise<T[]>;

  // Write operations
  create(data: Omit<T, 'id' | 'createdAt' | 'updatedAt'>): Promise<T>;

  update(id: string, data: Partial<T>): Promise<T | null>;

  delete(id: string): Promise<boolean>;

  // Batch operations
  createMany(items: Array<Omit<T, 'id' | 'createdAt' | 'updatedAt'>>): Promise<T[]>;

  updateMany(updates: Map<string, Partial<T>>): Promise<Map<string, T>>;

  deleteMany(ids: string[]): Promise<Map<string, boolean>>;
}
