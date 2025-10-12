<<<<<<< Updated upstream
/**
 * Content filter types for mature content handling
 */

/**
 * Available visibility options for mature content
 */
export type MatureContentVisibilityOptions = 'show' | 'blur' | 'hide';

/**
 * Content filter configuration options
 */
export interface ContentFilterOptions {
  /** Controls how mature content is displayed */
  matureContentVisibility: MatureContentVisibilityOptions;
}

/**
 * Content filter state which can either be valid options or an error state
 */
export type ContentFilterState = ContentFilterOptions | { error: string };

/**
 * Default content filter configuration
 */
export const defaultContentFilterState: ContentFilterState = {
  matureContentVisibility: 'blur',
};

/**
 * Content filter action types
 */
export const ContentFilterActionTypes = {
  SET_MATURE_CONTENT_VISIBILITY: 'SET_MATURE_CONTENT_VISIBILITY',
} as const;

export type ContentFilterActionType = (typeof ContentFilterActionTypes)[keyof typeof ContentFilterActionTypes];

/**
 * Content filter actions
 */
export interface ContentFilterAction {
  type: ContentFilterActionType;

  payload: {
    mode: MatureContentVisibilityOptions;
  };
}

/**
 * Function to set the mature content visibility mode
 */
export type SetMatureContentVisibility = (mode: MatureContentVisibilityOptions) => void;

/**
 * Content filter reducer function type
 */
export type ContentFilterReducer = (state: ContentFilterState, action: ContentFilterAction) => ContentFilterState;

/**
 * Content filter context type
 */
export interface ContentFilterContextType {
  state: ContentFilterState;

  setMatureContentVisibility: SetMatureContentVisibility;
}
=======
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
>>>>>>> Stashed changes
