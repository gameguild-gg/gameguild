import { useCallback, useRef, useState } from 'react';
import { SyncOperation, SyncOperationType } from "@/lib/sync/sync.types";

export const useSyncQueue = (maxRetries: number = 3) => {
  const [ queue, setQueue ] = useState<SyncOperation[]>([]);

  const operationIdRef = useRef(0);

  const generateOperationId = useCallback((): string => {
    operationIdRef.current++;
    return `op-${ Date.now() }-${ operationIdRef.current }`;
  }, []);

  const addToQueue = useCallback((operation: Partial<SyncOperation>) => {
    const newOperation: SyncOperation = {
      id: generateOperationId(),
      type: operation.type || SyncOperationType.UPDATE,
      itemId: operation.itemId || '',
      data: operation.data || {},
      timestamp: Date.now(),
      retryCount: 0,
      status: 'pending',
      ...operation,
    }

    setQueue(prevQueue => [ ...prevQueue, newOperation ]);

    return newOperation.id;
  }, [ generateOperationId ]);

  const removeFromQueue = useCallback((operationId: string) => {
    setQueue(prevQueue => prevQueue.filter(operation => operation.id !== operationId));
  }, []);

  const addCreateOperation = useCallback(
    (data: Omit<T, 'id' | 'createdAt' | 'updatedAt' | 'slug'>) => {
      return addToQueue({
        type: 'create',
        data: data,
      });
    }, [ addToQueue ]
  );

  const addUpdateOperation = useCallback(
    (itemId: string, data: Partial<Omit<T, 'id' | 'createdAt' | 'updatedAt' | 'slug'>>) => {
      return addToQueue({
        type: 'update',
        itemId: itemId,
        data: data,
      });
    }, [ addToQueue ]
  );

  const addDeleteOperation = useCallback(
    (itemId: string) => {
      return addToQueue({
        type: 'delete',
        itemId: itemId,
        data: {},
      });
    }, [ addToQueue ]
  );

  const updateOperationStatus = useCallback((operationId: string, status: SyncOperation['status'], error?: Error) => {
    setQueue(prev => prev.map(op => {
      if (op.id === operationId) {
        return { ...op, status, error };
      }
      return op;
    }));
  }, []);

  const markAsFailed = useCallback((operationId: string, error?: Error) => {
    setQueue(prev => prev.map(op => {
      if (op.id === operationId) {
        const newRetryCount = op.retryCount + 1;
        return {
          ...op,
          status: newRetryCount >= maxRetries ? 'failed' : 'pending',
          retryCount: newRetryCount,
          error,
        };
      }
      return op;
    }));
  }, [ maxRetries ]);

  const markAsCompleted = useCallback((operationId: string) => {
    setQueue(prev => prev.filter(op => op.id !== operationId));
  }, []);

  const getFailedOperations = useCallback(() => {
    return queue.filter(op => op.status === 'failed');
  }, [ queue ]);

  const getPendingOperations = useCallback(() => {
    return queue.filter(op => op.status === 'pending');
  }, [ queue ]);

  const clearQueue = useCallback(() => {
    setQueue([]);
  }, []);

  const clearFailedOperations = useCallback(() => {
    setQueue(prev => prev.filter(op => op.status !== 'failed'));
  }, []);

  const getQueueStats = useCallback(() => {
    return {
      total: queue.length,
      pending: queue.filter(op => op.status === 'pending').length,
      syncing: queue.filter(op => op.status === 'syncing').length,
      failed: queue.filter(op => op.status === 'failed').length,
      byType: {
        create: queue.filter(op => op.type === 'create').length,
        update: queue.filter(op => op.type === 'update').length,
        delete: queue.filter(op => op.type === 'delete').length,
      },
    };
  }, [ queue ]);

  return {
    queue,
    addToQueue,
    addCreateOperation,
    addUpdateOperation,
    addDeleteOperation,
    removeFromQueue,
    updateOperationStatus,
    markAsFailed,
    markAsCompleted,
    getFailedOperations,
    getPendingOperations,
    clearQueue,
    clearFailedOperations,
    getQueueStats,
  };
};