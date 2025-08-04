import React, { PropsWithChildren } from 'react';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  return (
    <div className="flex flex-col flex-1">
      {/* This is the main content area where children components will be rendered */}
      {children}
    </div>
  );
}
