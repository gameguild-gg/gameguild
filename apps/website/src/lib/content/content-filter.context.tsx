'use client';

<<<<<<< Updated upstream
import React, { createContext, PropsWithChildren, useCallback, useContext, useReducer } from 'react';
import {
  ContentFilterAction,
  ContentFilterActionTypes,
  ContentFilterContextType,
  ContentFilterReducer,
  ContentFilterState,
  defaultContentFilterState,
  MatureContentVisibilityOptions,
  SetMatureContentVisibility,
} from './types';

/**
 * Content filter reducer function to handle state updates
 */
const contentFilterReducer: ContentFilterReducer = (state: ContentFilterState, action: ContentFilterAction): ContentFilterState => {
  switch (action.type) {
    case ContentFilterActionTypes.SET_MATURE_CONTENT_VISIBILITY: {
      return {
        ...state,
        matureContentVisibility: action.payload.mode,
      };
    }
    default: {
      console.error(`Unhandled action type: ${action.type}`);
      return state;
    }
  }
};

/**
 * Default context to handle errors when context is used outside the provider
 */
const defaultContextValue: ContentFilterContextType = {
  state: defaultContentFilterState,
  setMatureContentVisibility: () => console.error('ContentFilterContext used outside of provider'),
};

/**
 * Content Filter Context for providing mature content filter state and actions
 */
const ContentFilterContext = createContext<ContentFilterContextType>(defaultContextValue);

/**
 * Props for ContentFilterProvider
 */
interface ContentFilterProviderProps {
  initialState?: ContentFilterState;
}

/**
 * Initialize content filter state with cookie values if available
 */
const createInitialContentFilterState = (initialState: ContentFilterState): ContentFilterState => {
  return {
    ...initialState,
  };
};

/**
 * ContentFilterProvider component
 * Manages state for content filtering preferences
 */
export const ContentFilterProvider = ({
  children,
  initialState = defaultContentFilterState,
}: PropsWithChildren<ContentFilterProviderProps>): React.JSX.Element => {
  const [state, dispatch] = useReducer(contentFilterReducer, initialState, () => createInitialContentFilterState(initialState));

  /**
   * Update mature content visibility preference
   */
  const setMatureContentVisibility: SetMatureContentVisibility = useCallback(
    (mode: MatureContentVisibilityOptions) => {
      dispatch({
        type: ContentFilterActionTypes.SET_MATURE_CONTENT_VISIBILITY,
        payload: { mode },
      });
    },
    [state, dispatch],
  );

  const value = { state, setMatureContentVisibility };

  return <ContentFilterContext.Provider value={value}>{children}</ContentFilterContext.Provider>;
};

/**
 * Hook to access content filter context
 * @throws Error if used outside a ContentFilterProvider
 */
export const useContentFilter = (): ContentFilterContextType => {
  const context = useContext(ContentFilterContext);

  if (!context) throw new Error('useContentFilter must be used within a ContentFilterProvider');

  return context;
};

/**
 * Hook to access the setMatureContentVisibility function
 * @throws Error if used outside a ContentFilterProvider
 */
export const useSetMatureContentVisibility = (): SetMatureContentVisibility => {
  const context = useContext(ContentFilterContext);

  if (!context) throw new Error('useSetMatureContentVisibility must be used within a ContentFilterProvider');

  return context.setMatureContentVisibility;
};
=======
import React, { PropsWithChildren } from 'react';
import { ContentFilterDispatch, ContentFilterState, defaultContentFilterState } from '@/lib/content/types';
import { useContentFilterReducer } from '@/lib/content/hooks/use-content-filter-reducer.hook';

type ContentFilterContextType = { state: ContentFilterState; dispatch: ContentFilterDispatch } | undefined;

type ContentFilterProviderProps = { initialState?: ContentFilterState };

export const ContentFilterContext = React.createContext<ContentFilterContextType>(undefined);

export function ContentFilterProvider({
  children,
  initialState = defaultContentFilterState,
}: PropsWithChildren<ContentFilterProviderProps>): React.JSX.Element {
  const [state, dispatch] = useContentFilterReducer(initialState);

  // React.useEffect(() => {}, []);

  const value = { state, dispatch };

  return <ContentFilterContext.Provider value={value}>{children}</ContentFilterContext.Provider>;
}

// export function useFetchContentFilterOptions() {
//   const context = React.useContext(ContentFilterContext);
//
//   if (context === undefined) {
//     throw new Error('useFetchContentFilter must be used within a ContentFilterProvider');
//   }
//
//   const { dispatch } = context;
//
//   return React.useCallback(async () => {
//     dispatch({ type: 'FETCH_INITIAL' });
//
//     // TODO: Fetch the content filter options using server actions.
//     // try {
//     //   const response = await fetch('/api/portfolio');
//     //   const data = await response.json();
//     //
//     //   dispatch({ type: 'FETCH_SUCCESS', payload: data });
//     // } catch (error) {
//     //   dispatch({ type: 'FETCH_FAILURE', payload: { error: (error as Error).message } });
//     // }
//   }, [dispatch]);
// }

// export async function setContentFilterState(): Promise<void> {
//   const cookieStore = await cookies();
//
//   cookieStore.set(CONTENT_FILTER_COOKIE_KEY, JSON.stringify(''));
// }
>>>>>>> Stashed changes
