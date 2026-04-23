import React from 'react';

export default async function Layout({ children }: LayoutProps<'/[locale]/members'>): Promise<React.JSX.Element> {
  return <>{children}</>;
}
