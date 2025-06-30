import { useCallback, useState } from 'react';
import { ConflictAutoResolutionMode, ConflictInfo, ConflictResolutionMode } from "@/lib/sync/sync.types";


export const useConflictResolver = () => {
  const [ pendingConflicts, setPendingConflicts ] = useState<ConflictInfo[]>([]);

  const addConflict = useCallback((conflict: ConflictInfo) => {
    setPendingConflicts(conflicts => [
      ...conflicts,
      {
        ...conflict,
        timestamp: Date.now(),
      }
    ]);
  }, []);

  const resolveConflict = useCallback((contentId: string) => {
    setPendingConflicts(conflicts => conflicts.filter(conflict => conflict.local.id !== contentId));
  }, []);

  const autoResolveConflicts = useCallback((strategy: ConflictResolutionMode): ConflictInfo[] => {
    const resolvedConflicts: ConflictInfo[] = [];

    setPendingConflicts(conflicts => {
      const remainingConflicts: ConflictInfo[] = [];

      conflicts.forEach(conflict => {
        let shouldResolve = false;
        let autoResolutionMode: ConflictAutoResolutionMode | null = null;

        switch (strategy) {
          case ConflictResolutionMode.LOCAL_FIRST:
            shouldResolve = true;
            autoResolutionMode = ConflictAutoResolutionMode.LOCAL;
            break;
          case ConflictResolutionMode.REMOTE_FIRST:
            shouldResolve = true;
            autoResolutionMode = ConflictAutoResolutionMode.REMOTE;
            break;
          case ConflictResolutionMode.MANUAL:
            // Do not auto-resolve, keep for manual resolution
            shouldResolve = false;
            break;
          case ConflictResolutionMode.MERGE:
            // TODO: Implement merge logic
            // For merge, we need more sophisticated logic
            // For now, we'll keep it in pending
            shouldResolve = false;
            break;
        }

        if (shouldResolve && autoResolutionMode) {
          resolvedConflicts.push({
            ...conflict,
            autoResolutionMode: autoResolutionMode,
          });
        } else {
          remainingConflicts.push(conflict);
        }
      });

      return remainingConflicts;
    });

    return resolvedConflicts;
  }, []);

  const getConflictByItemId = useCallback((id: string): ConflictInfo | null => {
    return pendingConflicts.find(conflict => conflict.local.id === id) || null;
  }, [ pendingConflicts ]);

  const hasConflicts = useCallback((): boolean => {
    return pendingConflicts.length > 0;
  }, [ pendingConflicts ]);

  const clearConflicts = useCallback(() => {
    setPendingConflicts([]);
  }, []);

  // const mergeResource = useCallback((local: Identifiable, remote: Identifiable): Identifiable => {
  //   // Implement your merge logic here
  //   // For now, we'll just return the local resource
  //   const merged: T
  //
  //   const localTime = new Date(local.updatedAt).getTime();
  //   const remoteTime = new Date(remote.updatedAt).getTime();
  //
  //   if(remoteTime > localTime) {
  //     Object.keys(remote).forEach(key => {
  //       const field = key as keyof T;
  //
  //       if (remote[field] !== undefined && remote[field] !== null) {
  //         merged[field] = remote[field];
  //       }
  //       if(Array.isArray(remote[field]) && Array.isArray(local[field])) {
  //         merged[field] = [ ...new Set([ ...(remote[field] as unknown[]), ...(local[field] as unknown[]) ]) ];
  //       }
  //     });
  //   }
  //
  //
  //   return local;
  // }, []);

  return {
    pendingConflicts,
    addConflict,
    resolveConflict,
    autoResolveConflicts,
    getConflictByItemId,
    hasConflicts,
    clearConflicts
  }
}