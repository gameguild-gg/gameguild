import React, { PropsWithChildren } from 'react';
import '@/styles/globals.css';

export default async function Layout({ children }: Readonly<PropsWithChildren>): Promise<React.JSX.Element> {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
