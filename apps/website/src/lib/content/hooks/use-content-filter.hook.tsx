import { useContext } from 'react';
import { ContentFilterContext } from '@/lib/content';

export function useContentFilter() {
  const context = useContext(ContentFilterContext);

  if (context === undefined) {
    throw new Error('useContentFilter must be used within a ContentFilterProvider');
  }

  return context;
}
