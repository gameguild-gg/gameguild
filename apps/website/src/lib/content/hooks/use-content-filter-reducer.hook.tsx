import { useReducer } from 'react';
import { contentFilterReducer, ContentFilterState, defaultContentFilterState } from '@/lib/content';

function createInitialContentFilterState({}): ContentFilterState {
  // TODO: Implement the logic to fetch the initial content filter state from cookies.
  return defaultContentFilterState;
}

export function useContentFilterReducer(initialState: ContentFilterState = defaultContentFilterState) {
  return useReducer(contentFilterReducer, { ...initialState }, createInitialContentFilterState);
}
