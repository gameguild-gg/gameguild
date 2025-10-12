import React, { PropsWithChildren } from 'react';
import '@/styles/globals.css';
import { CookieConsent } from '@gameguild/common/cookies';
import { WebVitals } from '@gameguild/common/analytics';

export default async function Layout({ children }: Readonly<PropsWithChildren>): Promise<React.JSX.Element> {
  return (
    <html lang="en">
      <body>
        <WebVitals />
        {children}
        <CookieConsent />
      </body>
    </html>
  );
}
