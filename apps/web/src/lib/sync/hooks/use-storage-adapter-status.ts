import { useCallback, useEffect, useState } from 'react';
import { useSync } from '../sync-provider';
import { StorageAdapterType } from '../storage/storage-types';

export interface StorageAdapterStatus {
  type: StorageAdapterType;
  isAvailable: boolean;
  isActive: boolean;
  size: number;
  itemCount: number;
  maxSize?: number;
  usedPercentage?: number;
  lastError?: Error;
}

export interface UseStorageAdapterStatusResult {
  adapters: StorageAdapterStatus[];
  currentAdapter: StorageAdapterType;
  switchAdapter: (adapter: StorageAdapterType) => Promise<void>;
  refreshStatus: () => Promise<void>;
  isLoading: boolean;
  error: Error | null;
}

/**
 * Hook for monitoring storage adapter status and switching between adapters
 */
export function useStorageAdapterStatus(): UseStorageAdapterStatusResult {
  const sync = useSync();
  const [adapters, setAdapters] = useState<StorageAdapterStatus[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  // Refresh adapter status
  const refreshStatus = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const stats = await sync.getStorageStats();
      const availableAdapters = sync.availableAdapters;
      const currentAdapter = sync.storageAdapter;

      const adapterStatuses: StorageAdapterStatus[] = availableAdapters.map((type) => {
        const adapterStats = stats?.adapters?.find((s: { type: StorageAdapterType }) => s.type === type);

        return {
          type,
          isAvailable: true, // If it's in availableAdapters, it's available
          isActive: type === currentAdapter,
          size: adapterStats?.size || 0,
          itemCount: adapterStats?.itemCount || 0,
          maxSize: adapterStats?.maxSize,
          usedPercentage: adapterStats?.usedPercentage,
        };
      });

      setAdapters(adapterStatuses);
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Failed to refresh adapter status');
      setError(error);
    } finally {
      setIsLoading(false);
    }
  }, [sync]);

  // Switch adapter
  const switchAdapter = useCallback(
    async (adapter: StorageAdapterType) => {
      setError(null);

      try {
        await sync.setStorageAdapter(adapter);
        await refreshStatus(); // Refresh to update active status
      } catch (err) {
        const error = err instanceof Error ? err : new Error(`Failed to switch to ${adapter}`);
        setError(error);
        throw error;
      }
    },
    [sync, refreshStatus],
  );

  // Initial load and periodic refresh
  useEffect(() => {
    refreshStatus();

    // Refresh every 30 seconds
    const interval = setInterval(refreshStatus, 30000);

    return () => {
      clearInterval(interval);
    };
  }, [refreshStatus]);

  return {
    adapters,
    currentAdapter: sync.storageAdapter,
    switchAdapter,
    refreshStatus,
    isLoading,
    error,
  };
}
