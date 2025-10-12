'use client';

import { useCallback, useState } from 'react';

/**
 * Hook for triggering error boundaries from functional components
 * Compatible with React 19 and Next.js 15+
 */
export function useErrorBoundary() {
  const [, setError] = useState<Error | null>(null);

  const captureError = useCallback((error: Error | string) => {
    const errorObj = error instanceof Error ? error : new Error(error);
    setError(() => {
      throw errorObj;
    });
  }, []);

  return { captureError };
}

/**
 * Hook for handling async errors in components
 */
export function useAsyncError() {
  const { captureError } = useErrorBoundary();

  const executeAsync = useCallback(
    async <T>(asyncFn: () => Promise<T>): Promise<T | void> => {
      try {
        return await asyncFn();
      } catch (error) {
        captureError(error instanceof Error ? error : new Error(String(error)));
      }
    },
    [captureError],
  );

  return { executeAsync };
}

export default useErrorBoundary;
