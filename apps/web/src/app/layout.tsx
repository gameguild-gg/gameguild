import React from 'react';
import type { Metadata } from 'next';
import { getLocale } from 'next-intl/server';
import '@/styles/globals.css';

export const metadata: Metadata = {
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
      <body>{children}</body>
    </html>
  );
}
