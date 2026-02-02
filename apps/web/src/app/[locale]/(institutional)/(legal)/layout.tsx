import React from 'react';

export default async function Layout({ children }: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  return <>{children}</>;
}
