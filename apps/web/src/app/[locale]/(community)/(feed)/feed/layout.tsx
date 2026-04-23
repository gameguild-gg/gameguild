import React from 'react';

// Parallel route slots: @modal (intercepted modals), @following / @discover / @trending (tabbed feeds).
// Slots render alongside `children`. UI tabs to switch which slot is visible will live in the (community) shell layout.
export default function Layout({
  children,
  modal,
  following,
  discover,
  trending,
}: LayoutProps<'/[locale]/feed'>): React.JSX.Element {
  return (
    <div className="container mx-auto px-4 py-6">
      {children}
      <div className="mt-6 grid gap-6 md:grid-cols-3">
        {following}
        {discover}
        {trending}
      </div>
      {modal}
    </div>
  );
}
