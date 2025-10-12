import React, { PropsWithChildren } from 'react';
import '@/styles/globals.css';
import { CookieConsent } from '@gameguild/common/cookies';
import { WebVitals } from '@gameguild/common/analytics';
import { ContentFilter } from '@/components/content/content-filter';
import { getInitialContentFilterState } from '@/lib/content/content-filter.actions';
import { ContentFilterProvider } from '@/lib/content/content-filter.context';
import { ContentFilterState } from '@/lib/content/types';

export async function generateMetadata(): Promise<Metadata> {
  return {
    title: {
      template: ' %s | Matheus Martins',
      default: 'Matheus Martins',
    },
  };
}

export default async function Layout({ children }: Readonly<PropsWithChildren>): Promise<React.JSX.Element> {
  const initialContentFilterState: ContentFilterState = await getInitialContentFilterState();

  return (
    <html lang="en">
      <body>
        <WebVitals />
        <ContentFilterProvider initialState={initialContentFilterState}>
          {children}
          <ContentFilter />
          <CookieConsent />
        </ContentFilterProvider>
      </body>
    </html>
  );
}
