import { ContentFilterAction, ContentFilterState } from '@/lib/content/types';

export function contentFilterReducer(state: ContentFilterState, action: ContentFilterAction): ContentFilterState {
  switch (action.type) {
    case 'FETCH_INITIAL': {
      return {
        ...state,
      };
    }
    case 'FETCH_SUCCESS': {
      return {
        ...state,
        // TODO: Add the fetched data to the state.
      };
    }
    case 'FETCH_FAILURE': {
      return {
        ...state,
        error: action.payload.error,
      };
    }
    case 'SET_MATURE_CONTENT_VISIBILITY': {
      return {
        ...state,
        matureContentVisibility: action.payload.mode,
      };
    }
    default:
      throw new Error(`Unhandled action type: ${action}`);
  }
}
