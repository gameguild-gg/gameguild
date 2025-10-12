import { useContentFilter } from './use-content-filter.hook';

export function useContentFilterState() {
  try {
    const context = useContentFilter();

    const { state } = context;

    return state;
  } catch (error) {
    throw new Error('useContentFilterState must be used within a ContentFilterProvider');
  }
}
