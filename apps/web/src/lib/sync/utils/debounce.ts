export function debounce<T extends (...args: unknown[]) => unknown>(func: T, delay: number): (...args: Parameters<T>) => void {
  let timeoutId: NodeJS.Timeout | null = null;

  return function debounced(...args: Parameters<T>) {
    if (timeoutId) {
      clearTimeout(timeoutId);
    }

    timeoutId = setTimeout(() => {
      func(...args);
      timeoutId = null;
    }, delay);
  };
}

export function throttle<T extends (...args: unknown[]) => unknown>(func: T, limit: number): (...args: Parameters<T>) => void {
  let inThrottle = false;

  return function throttled(...args: Parameters<T>) {
    if (!inThrottle) {
      func(...args);
      inThrottle = true;
      setTimeout(() => {
        inThrottle = false;
      }, limit);
    }
  };
}

export function batchOperations<T>(operations: T[], batchSize: number, delay: number = 0): Promise<T[][]> {
  return new Promise((resolve) => {
    const batches: T[][] = [];

    for (let i = 0; i < operations.length; i += batchSize) {
      batches.push(operations.slice(i, i + batchSize));
    }

    if (delay > 0) {
      setTimeout(() => resolve(batches), delay);
    } else {
      resolve(batches);
    }
  });
}
