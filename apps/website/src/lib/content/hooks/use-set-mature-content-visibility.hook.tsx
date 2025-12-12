import { useCallback } from 'react';
import { useContentFilter } from './use-content-filter.hook';
import { MatureContentVisibilityOptions, SetMatureContentVisibility } from '@/lib/content';

export function useSetMatureContentVisibility(): SetMatureContentVisibility {
  try {
    const context = useContentFilter();

    return useCallback(
      (mode: MatureContentVisibilityOptions) =>
        context.dispatch({
          type: 'SET_MATURE_CONTENT_VISIBILITY',
          payload: { mode },
        }),
      [context.dispatch],
    );
  } catch (error) {
    throw new Error('useSetMatureContentVisibility must be used within a ContentFilterProvider');
  }
}
