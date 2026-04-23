import React from 'react';

/**
 * Learning Section Layout (route group pass-through)
 *
 * The API handles tenant/permissions via auth context, so no explicit data
 * fetching is needed here. Kept as a pure pass-through; consider deleting the
 * (learning) route group folder if no future learning-specific layout is added.
 */
export default function Layout({ children }: { children: React.ReactNode }): React.JSX.Element {
  return <>{children}</>;
}
