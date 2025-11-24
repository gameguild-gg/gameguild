import React, { PropsWithChildren } from 'react';

export default async function Layout({ children }: Readonly<PropsWithChildren>): Promise<React.JSX.Element> {
  return <div>{children}</div>;
}
