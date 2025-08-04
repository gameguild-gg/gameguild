// Client-side storage types and interfaces
export * from './storage-types';

// Base storage adapter
export * from './base-storage';

// Storage adapters
export * from './storage-adapters';
export * from './localStorage';
export * from './sessionStorage';
export * from './indexeddb';
export * from './cache-api';
export * from './memory';

// Storage manager
export * from './storage-manager';

// Legacy exports (for backward compatibility)
export * from './base-storage.adapter';
export * from './markdown-storage.adapter';
export * from './image-storage.adapter';
export * from './storage.factory';
