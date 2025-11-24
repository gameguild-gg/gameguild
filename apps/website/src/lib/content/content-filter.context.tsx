'use client';

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
