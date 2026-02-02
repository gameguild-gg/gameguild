import React from 'react';

/**
 * Learning Section Layout
 *
 * Shared layout for all learning routes. The API handles tenant/permissions
 * via auth context, so no explicit data fetching is needed here.
 *
 * Future: May add learning-specific navigation or context providers.
 */
export default async function Layout({ children }: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  return <>{children}</>;
}
