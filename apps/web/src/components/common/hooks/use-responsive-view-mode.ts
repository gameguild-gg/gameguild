/**
 * Stub hook for responsive view mode.
 * Used for switching between list/grid views on responsive layouts.
 */

import { useEffect, useState } from 'react';

export type ViewMode = 'list' | 'grid' | 'compact';

export interface UseResponsiveViewModeOptions {
  defaultMode?: ViewMode;
  breakpoint?: number;
}

export function useResponsiveViewMode(options?: UseResponsiveViewModeOptions) {
  const [viewMode, setViewMode] = useState<ViewMode>(options?.defaultMode || 'grid');
  const [isSmallScreen, setIsSmallScreen] = useState(false);

  useEffect(() => {
    const breakpoint = options?.breakpoint ?? 768;
    const checkSize = () => setIsSmallScreen(window.innerWidth < breakpoint);
    checkSize();
    window.addEventListener('resize', checkSize);
    return () => window.removeEventListener('resize', checkSize);
  }, [options?.breakpoint]);

  return {
    viewMode,
    setViewMode,
    isListView: viewMode === 'list',
    isGridView: viewMode === 'grid',
    isCompactView: viewMode === 'compact',
    isSmallScreen,
  };
}

export default useResponsiveViewMode;
