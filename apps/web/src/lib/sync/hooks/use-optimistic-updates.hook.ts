import { useCallback, useRef } from 'react';
import { Identifiable } from "@/lib/types";

interface OptimisticUpdate<T extends Identifiable> {
  itemId: string;
  originalData: Omit<T, 'id'>;
  optimisticData: Partial<Omit<T, 'id'>>;
  timestamp: number;
}

export const useOptimisticUpdates = <T extends Identifiable>(currentItems: T[]) => {
  const optimisticUpdatesRef = useRef<Map<string, OptimisticUpdate<T>>>(new Map());

  const applyOptimisticUpdate = useCallback((itemId: string, updatedData: Partial<Omit<T, 'id'>>) => {
    const items = currentItems.find(item => item.id === itemId);

    if (!items) return null;

    // Store the original data before applying the optimistic update.
    if (!optimisticUpdatesRef.current.has(itemId)) {
      optimisticUpdatesRef.current.set(itemId, {
        itemId,
        originalData: { ...items },
        optimisticData: updatedData,
        timestamp: Date.now(),
      });
    } else {
      const existingItem = optimisticUpdatesRef.current.get(itemId)!;

      optimisticUpdatesRef.current.set(itemId, {
        ...existingItem,
        optimisticData: {
          ...existingItem.optimisticData,
          ...updatedData,
        },
        timestamp: Date.now(),
      });
    }

    return { ...items, ...updatedData };
  }, [ currentItems ]);

  const rollbackOptimisticUpdate = useCallback((itemId: string) => {
    const updatedItem = optimisticUpdatesRef.current.get(itemId);

    if (!updatedItem) return null;

    // Restore the original data.
    const originalData = updatedItem.originalData;

    // Remove the optimistic update from the map.
    optimisticUpdatesRef.current.delete(itemId);

    return { id: itemId, ...originalData };
  }, []);

  const getOptimisticItem = useCallback((itemId: string): T | null => {
    const item = currentItems.find(item => item.id === itemId);

    if (!item) return null;

    const optimisticUpdate = optimisticUpdatesRef.current.get(itemId);

    // No optimistic update, return the original item.
    if (!optimisticUpdate) return item;

    return { ...item, ...optimisticUpdate.optimisticData };
  }, [ currentItems ]);

  const getAllOptimisticItems = useCallback((): T[] => {
    return currentItems.map(item => {
      const itemId = item.id;
      const optimisticUpdate = optimisticUpdatesRef.current.get(itemId);

      // No optimistic update, return the original item.
      if (!optimisticUpdate) return item;

      return { ...item, ...optimisticUpdate.optimisticData };
    });
  }, [ currentItems ]);

  const hasOptimisticUpdate = useCallback((itemId: string): boolean => {
    return optimisticUpdatesRef.current.has(itemId);
  }, []);

  const clearOptimisticUpdates = useCallback(() => {
    optimisticUpdatesRef.current.clear();
  }, []);

  const getOptimisticUpdatesCount = useCallback((): number => {
    return optimisticUpdatesRef.current.size;
  }, [ optimisticUpdatesRef ]);

  return {
    applyOptimisticUpdate,
    rollbackOptimisticUpdate,
    getOptimisticItem,
    getAllOptimisticItems,
    hasOptimisticUpdate,
    clearOptimisticUpdates,
    getOptimisticUpdatesCount,
  };
};