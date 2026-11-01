import React from 'react';
import Script from 'next/script';
import type { Metadata } from 'next';
import { getLocale } from 'next-intl/server';
import { DevelopmentReactDiagnostics } from '@/components/app/development-react-diagnostics';
import { AnalyticsConsent } from '@/components/app/analytics-consent';
import '@/styles/globals.css';

/**
 * Canonical site origin (issue #103): resolves relative metadata URLs
 * (canonical links, Open Graph) onto the canonical host, mirroring
 * `app/sitemap.ts` and `app/robots.ts`. Same expression as the blog SEO
 * helper (`lib/blogs/seo.ts`).
 */
const SITE_BASE_URL = process.env.NEXT_PUBLIC_APP_URL ?? 'https://gameguild.gg';

export const metadata: Metadata = {
  metadataBase: new URL(SITE_BASE_URL),
  applicationName: 'GameGuild',
  title: {
    default: 'GameGuild',
    template: '%s | GameGuild',
  },
  description: 'Game development learning and community platform.',
  manifest: '/manifest.webmanifest',
  icons: {
    icon: [{ url: '/favicon.svg', type: 'image/svg+xml' }],
    shortcut: [{ url: '/favicon.svg', type: 'image/svg+xml' }],
  },
};

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const locale = await getLocale();

  return (
    <html lang={locale} suppressHydrationWarning>
      <head>
        {process.env.NODE_ENV === 'development' &&
          process.env.NEXT_PUBLIC_DISABLE_REACT_DEVTOOLS !== '1' && (
          <Script
            src="//unpkg.com/react-grab/dist/index.global.js"
            crossOrigin="anonymous"
            strategy="beforeInteractive"
          />
          )}
      </head>
      <body suppressHydrationWarning>
        <AnalyticsConsent />
        <DevelopmentReactDiagnostics />
        {children}
      </body>
    </html>
  );
}
