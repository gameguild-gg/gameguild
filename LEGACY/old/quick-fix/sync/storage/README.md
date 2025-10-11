# Enhanced Client-Side Storage System

## Overview

I've created a c```tsx
import { SyncProvider } from '@/lib/sync/sync-provider';

function App() {
  return (
    <SyncProvider
      primaryStorageAdapter="indexedDB"
      fallbackAdapters={['localStorage', 'sessionStorage', 'memory']}
      namespace="my-app"
      enableOptimisticUpdates={true}
    >
      <MyAppContent />
    </SyncProvider>
  );
}
```ent-side storage system that integrates multiple storage adapters and provides a unified interface for your sync and storage needs. This system now includes powerful hooks for context integration and real-time change monitoring.

## File Structure

```
storage/
├── storage.auth.types.ts          # Type definitions for all storage adapters
├── base-storage.ts           # Base class for all storage adapters
├── storage-adapters.ts       # Factory for creating storage adapters
├── storage-manager.ts        # Manager that orchestrates multiple adapters
├── local-storage.ts          # localStorage implementation
├── session-storage.ts        # sessionStorage implementation  
├── indexed-db.ts             # IndexedDB implementation
├── cache-api.ts              # Cache API implementation
├── memory.ts                 # In-memory implementation (fallback)
└── index.ts                  # Main exports
```

## New Hooks

```
hooks/
├── use-enhanced-storage.ts   # Enhanced storage hook with auto-sync
├── use-batch-storage.ts      # Batch operations hook
├── use-storage-observer.ts   # Real-time change monitoring
├── use-storage-adapter-status.ts # Adapter status and switching
├── use-context-integration.ts # React context integration
└── index.ts                  # Hook exports
```

## Features

### 🔄 Multiple Storage Adapters
- **localStorage** - Browser local storage (`local-storage.ts`)
- **sessionStorage** - Session-based storage (`session-storage.ts`)
- **IndexedDB** - High-performance database storage (`indexed-db.ts`)
- **Cache API** - Service worker compatible caching (`cache-api.ts`)
- **Memory** - In-memory fallback storage (`memory.ts`)

### 🎯 Smart Adapter Selection
- Automatic fallback to available adapters
- Configurable primary and fallback adapters
- Runtime adapter switching with data migration
- Support detection for each storage type

### 📊 Enhanced Features
- **TTL Support** - Time-to-live for stored items
- **Batch Operations** - Efficient bulk read/write operations
- **Event System** - Real-time change notifications
- **Quota Management** - Automatic cleanup when storage is full
- **Compression** - Optional data compression
- **Encryption** - Optional data encryption (planned)

### 🎣 Advanced Hooks
- **useEnhancedStorage** - Auto-saving, debounced storage with optimistic updates
- **useBatchStorage** - Efficient batch operations for multiple items
- **useStorageObserver** - Real-time monitoring of storage changes
- **useStorageAdapterStatus** - Monitor and switch between storage adapters
- **useContextIntegration** - Seamless integration with React contexts

## Usage Examples

### Basic Setup

```tsx
import { EnhancedSyncProvider } from '@/lib/sync/enhanced-sync.provider';

function App() {
  return (
    <EnhancedSyncProvider
      primaryStorageAdapter="indexedDB"
      fallbackAdapters={['localStorage', 'sessionStorage', 'memory']}
      namespace="my-app"
      enableOptimisticUpdates={true}
    >
      <MyAppContent />
    </EnhancedSyncProvider>
  );
}
```

### Using Enhanced Storage Hook

```tsx
import { useEnhancedStorage } from '@/lib/sync/hooks';

function UserProfile() {
  const { data: profile, setData, isLoading, error } = useEnhancedStorage<UserProfile>({
    key: 'user:profile',
    defaultValue: { name: '', email: '' },
    ttl: 24 * 60 * 60 * 1000, // 24 hours
    debounceMs: 500,
  });

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;

  return (
    <form onSubmit={(e) => {
      e.preventDefault();
      setData(profile); // Auto-saves with debouncing
    }}>
      <input 
        value={profile?.name || ''} 
        onChange={(e) => setData({ ...profile, name: e.target.value })}
      />
      <input 
        value={profile?.email || ''} 
        onChange={(e) => setData({ ...profile, email: e.target.value })}
      />
    </form>
  );
}
```

### Context Integration

```tsx
import { useContextIntegration } from '@/lib/sync/hooks';

function useAppState() {
  const contextIntegration = useContextIntegration<AppState>({
    namespace: 'app',
    syncKeys: ['preferences', 'gameState'],
    autoSync: true,
    syncInterval: 5000,
  });

  // Watch for changes from other tabs/windows
  useEffect(() => {
    const unsubscribe = contextIntegration.watchStateChanges('preferences', (newPrefs) => {
      setPreferences(newPrefs);
    });
    return unsubscribe;
  }, []);

  return {
    saveState: (key: string, state: AppState) => contextIntegration.syncStateToStorage(key, state),
    loadState: (key: string) => contextIntegration.loadStateFromStorage(key),
    onStateChange: contextIntegration.watchStateChanges,
  };
}
```

### Storage Observer

```tsx
import { useStorageObserver } from '@/lib/sync/hooks';

function StorageMonitor() {
  const { changes, lastChange } = useStorageObserver({
    keys: /^user:/,  // Watch all keys starting with "user:"
    includeValues: true,
    debounceMs: 200,
  });

  return (
    <div>
      <h3>Storage Activity</h3>
      {lastChange && (
        <div>
          Last change: {lastChange.type} on {lastChange.key} at {new Date(lastChange.timestamp).toLocaleTimeString()}
        </div>
      )}
      <ul>
        {changes.slice(-10).map((change, i) => (
          <li key={i}>
            {change.type} - {change.key} - {new Date(change.timestamp).toLocaleTimeString()}
          </li>
        ))}
      </ul>
    </div>
  );
}
```

### Batch Operations

```tsx
import { useBatchStorage } from '@/lib/sync/hooks';

function BulkDataImport() {
  const { executeBatch, isExecuting, error } = useBatchStorage();

  const importData = async (items: Array<{key: string, value: any}>) => {
    const operations = items.map(item => ({
      type: 'set' as const,
      key: item.key,
      value: item.value,
      ttl: 24 * 60 * 60 * 1000, // 24 hours
    }));

    try {
      const results = await executeBatch(operations);
      console.log('Import results:', results);
    } catch (err) {
      console.error('Import failed:', err);
    }
  };

  return (
    <div>
      <button onClick={() => importData(someData)} disabled={isExecuting}>
        {isExecuting ? 'Importing...' : 'Import Data'}
      </button>
      {error && <div>Error: {error.message}</div>}
    </div>
  );
}
```

### Adapter Management

```tsx
import { useStorageAdapterStatus } from '@/lib/sync/hooks';

function StorageSettings() {
  const { adapters, currentAdapter, switchAdapter, isLoading } = useStorageAdapterStatus();

  return (
    <div>
      <h3>Storage Adapters</h3>
      {adapters.map(adapter => (
        <div key={adapter.type}>
          <input
            type="radio"
            checked={adapter.isActive}
            onChange={() => switchAdapter(adapter.type)}
            disabled={!adapter.isAvailable || isLoading}
          />
          <span>
            {adapter.type} - {adapter.size} bytes ({adapter.itemCount} items)
            {adapter.usedPercentage && ` - ${adapter.usedPercentage.toFixed(1)}% used`}
          </span>
        </div>
      ))}
    </div>
  );
}
```

## Configuration Options

### SyncProvider Props

```tsx
interface SyncProviderProps {
  // Storage Configuration
  primaryStorageAdapter?: StorageAdapterType; // 'indexedDB' | 'localStorage' | 'sessionStorage' | 'cache' | 'memory'
  fallbackAdapters?: StorageAdapterType[];
  namespace?: string;

  // Sync Configuration  
  syncInterval?: number; // milliseconds
  maxRetries?: number;
  enableBackgroundSync?: boolean;
  enableOptimisticUpdates?: boolean;
  conflictResolution?: ConflictResolution;
  maxCacheSize?: number;
  debounceDelay?: number;
  defaultTTL?: number; // milliseconds
}
```

### Storage Adapter Types

- **localStorage**: Browser's localStorage API - persistent across sessions
- **sessionStorage**: Browser's sessionStorage API - cleared when tab closes  
- **indexedDB**: High-performance database storage - large capacity, asynchronous
- **cache**: Cache API - works with Service Workers, good for offline support
- **memory**: In-memory storage - fast but lost on page refresh

## Integration with Other Contexts

The system is designed to work seamlessly with other React contexts. The `useContextIntegration` hook provides:

- **Namespace Management**: Organize data by context/feature
- **Auto-sync**: Automatically save state changes
- **Change Watching**: React to changes from other tabs/windows
- **Conflict Resolution**: Handle concurrent modifications
- **Bulk Operations**: Efficient multi-key operations

See `examples/game-context-example.tsx` for a complete implementation example.

## Event System

The storage system emits various events for monitoring and debugging:

```tsx
const { onContentChange } = useSync();

useEffect(() => {
  const unsubscribe = onContentChange((event) => {
    console.log('Storage change:', event);
    // event: { type: 'set' | 'delete' | 'clear', key?: string, value?: unknown }
  });
  
  return unsubscribe;
}, []);
```

## Performance Considerations

- **Debouncing**: Automatic debouncing of rapid changes
- **Batch Operations**: Use `useBatchStorage` for multiple operations
- **TTL Management**: Automatic cleanup of expired items
- **Quota Handling**: Automatic cleanup when storage limits are reached
- **Fallback Strategy**: Seamless fallback to available storage types

## Best Practices

1. **Choose the Right Adapter**: 
   - Use IndexedDB for large data and complex queries
   - Use localStorage for simple persistent data
   - Use sessionStorage for temporary session data
   - Use cache API for offline-capable applications

2. **Namespace Your Data**: Always use meaningful namespaces to avoid conflicts

3. **Set Appropriate TTLs**: Use TTL to prevent storage bloat

4. **Monitor Storage Usage**: Use `useStorageAdapterStatus` to monitor capacity

5. **Handle Errors Gracefully**: Always provide fallbacks for storage failures

6. **Use Batch Operations**: For multiple related operations, use batch methods for better performance

## Migration from Previous Versions

If you're migrating from an older storage system:

1. Wrap your app with `SyncProvider`
2. Replace direct storage calls with the provided hooks
3. Update context integrations to use `useContextIntegration`
4. Test fallback scenarios with different storage availability

This enhanced storage system provides a robust, scalable foundation for client-side data management with excellent developer experience and powerful context integration capabilities.
import { EnhancedSyncProvider, useEnhancedSync } from '@/lib/sync/storage';

// Wrap your app with the provider
function App() {
  return (
    <EnhancedSyncProvider
      primaryStorageAdapter="indexedDB"
      fallbackAdapters={['localStorage', 'sessionStorage', 'memory']}
      namespace="my-app"
      defaultTTL={24 * 60 * 60 * 1000} // 24 hours
    >
      <MyComponent />
    </EnhancedSyncProvider>
  );
}
```

### Using the Storage Hook

```tsx
function MyComponent() {
  const {
    get,
    set,
    delete: deleteItem,
    clear,
    has,
    keys,
    getMany,
    setMany,
    deleteMany,
    storageAdapter,
    setStorageAdapter,
    availableAdapters,
    getStorageStats,
    onContentChange,
  } = useEnhancedSync();

  // Basic operations
  const handleSave = async () => {
    await set('user-data', { name: 'John', age: 30 });
  };

  const handleLoad = async () => {
    const userData = await get<{ name: string; age: number }>('user-data');
    console.log(userData);
  };

  // Batch operations
  const handleBatchSave = async () => {
    const items = new Map([
      ['item1', { data: 'value1' }],
      ['item2', { data: 'value2' }],
      ['item3', { data: 'value3' }],
    ]);
    
    const results = await setMany(items, { ttl: 60000 }); // 1 minute TTL
    console.log('Save results:', results);
  };

  // Change storage adapter
  const handleSwitchAdapter = async () => {
    try {
      await setStorageAdapter('localStorage');
      console.log('Switched to localStorage');
    } catch (error) {
      console.error('Failed to switch adapter:', error);
    }
  };

  // Listen for content changes
  useEffect(() => {
    const unsubscribe = onContentChange((event) => {
      console.log('Content changed:', event);
    });

    return unsubscribe;
  }, [onContentChange]);

  return (
    <div>
      <p>Current adapter: {storageAdapter}</p>
      <p>Available adapters: {availableAdapters.join(', ')}</p>
      
      <button onClick={handleSave}>Save Data</button>
      <button onClick={handleLoad}>Load Data</button>
      <button onClick={handleBatchSave}>Batch Save</button>
      <button onClick={handleSwitchAdapter}>Switch to localStorage</button>
    </div>
  );
}
```

### Advanced Usage with Context Integration

```tsx
// Use within another context
function useMyDataContext() {
  const { set, get, onContentChange } = useEnhancedSync();
  const [data, setData] = useState(null);

  // Listen for changes to specific keys
  useEffect(() => {
    const unsubscribe = onContentChange((event) => {
      if (event.type === 'set' && event.key === 'my-data') {
        setData(event.value);
      }
    });

    return unsubscribe;
  }, [onContentChange]);

  const updateData = async (newData) => {
    await set('my-data', newData);
    setData(newData); // Optimistic update
  };

  return { data, updateData };
}
```

### Storage Statistics

```tsx
function StorageStats() {
  const { getStorageStats } = useEnhancedSync();
  const [stats, setStats] = useState(null);

  useEffect(() => {
    const loadStats = async () => {
      const storageStats = await getStorageStats();
      setStats(storageStats);
    };

    loadStats();
  }, [getStorageStats]);

  if (!stats) return <div>Loading stats...</div>;

  return (
    <div>
      <h3>Storage Statistics</h3>
      <p>Primary: {stats.primary.type} ({stats.primary.size} bytes, {stats.primary.itemCount} items)</p>
      <p>Total Size: {stats.totalSize} bytes</p>
      <p>Total Items: {stats.totalItems}</p>
      
      <h4>Fallback Adapters:</h4>
      {stats.fallbacks.map((fallback, index) => (
        <p key={index}>
          {fallback.type}: {fallback.size} bytes, {fallback.itemCount} items
        </p>
      ))}
    </div>
  );
}
```

## Configuration Options

### Provider Props

```tsx
interface EnhancedSyncProviderProps {
  // Storage Configuration
  primaryStorageAdapter?: 'localStorage' | 'sessionStorage' | 'indexedDB' | 'cache' | 'memory';
  fallbackAdapters?: StorageAdapterType[];
  namespace?: string;

  // Sync Configuration
  syncInterval?: number; // milliseconds
  maxRetries?: number;
  enableBackgroundSync?: boolean;
  enableOptimisticUpdates?: boolean;
  conflictResolution?: 'local-first' | 'remote-first' | 'manual' | 'merge';
  maxCacheSize?: number;
  debounceDelay?: number;
  defaultTTL?: number; // milliseconds
}
```

## API Reference

### Storage Operations
- `get<T>(key: string): Promise<T | null>` - Get a single item
- `set<T>(key: string, value: T, options?: { ttl?: number }): Promise<boolean>` - Set a single item
- `delete(key: string): Promise<boolean>` - Delete a single item
- `clear(): Promise<boolean>` - Clear all items
- `has(key: string): Promise<boolean>` - Check if key exists
- `keys(): Promise<string[]>` - Get all keys

### Batch Operations
- `getMany<T>(keys: string[]): Promise<Map<string, T>>` - Get multiple items
- `setMany<T>(items: Map<string, T>, options?: { ttl?: number }): Promise<Map<string, boolean>>` - Set multiple items
- `deleteMany(keys: string[]): Promise<Map<string, boolean>>` - Delete multiple items

### Management
- `setStorageAdapter(adapter: StorageAdapterType): Promise<void>` - Switch storage adapter
- `getStorageStats(): Promise<StorageManagerStats>` - Get storage statistics
- `onContentChange(callback): () => void` - Listen for content changes

## Migration from Old System

The new system is backward compatible with your existing sync system. You can gradually migrate by:

1. Replace `SyncProvider` with `EnhancedSyncProvider`
2. Replace `useSync` with `useEnhancedSync`
3. Use the new storage operations instead of direct content manipulation
4. Configure your preferred storage adapters

## Benefits

✅ **Flexible Storage**: Choose the best storage adapter for your use case
✅ **Automatic Fallbacks**: Never lose data due to storage unavailability
✅ **Performance**: Optimized batch operations and caching
✅ **Developer Experience**: Type-safe APIs and comprehensive error handling
✅ **Future Proof**: Easy to extend with new storage adapters
✅ **Production Ready**: Comprehensive error handling and edge case management
