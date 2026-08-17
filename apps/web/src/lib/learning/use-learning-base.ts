'use client';

import { usePathname } from 'next/navigation';

/**
 * Base path for learning console routes on the current surface:
 * /console/learning under the management console, /workspace/learning otherwise.
 */
export function useLearningBase(): string {
  const pathname = usePathname?.() ?? '';
  return /\/console(\/|$)/.test(pathname) ? '/console/learning' : '/workspace/learning';
}
