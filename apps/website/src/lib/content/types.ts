export type ContentFilterAction =
  | { type: 'FETCH_INITIAL' }
  | {
      type: 'FETCH_SUCCESS';
      payload: {
        // TODO: Add the fetched data type here.
      };
    }
  | { type: 'FETCH_FAILURE'; payload: { error: string } }
  | { type: 'SET_MATURE_CONTENT_VISIBILITY'; payload: { mode: MatureContentVisibilityOptions } };

export type ContentFilterDispatch = (action: ContentFilterAction) => void;

export type ContentFilterOptions = { matureContentVisibility: MatureContentVisibilityOptions };

export type ContentFilterState = ContentFilterOptions | { error: string };

export type MatureContentVisibilityOptions = 'show' | 'blur' | 'hide';

export type SetMatureContentVisibility = (mode: MatureContentVisibilityOptions) => void;

export const defaultContentFilterState: ContentFilterState = {
  matureContentVisibility: 'blur',
};
