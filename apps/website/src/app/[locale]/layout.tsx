import { routing } from '@/i18n/routing';
import { PropsWithLocaleParams } from '@/types';
import type { Metadata } from 'next';
import { hasLocale, NextIntlClientProvider } from 'next-intl';
import { setRequestLocale } from 'next-intl/server';
import { notFound } from 'next/navigation';
import React, { PropsWithChildren } from 'react';

export async function generateMetadata({ params }: PropsWithLocaleParams): Promise<Metadata> {
  const { locale } = await params;

  // const metadata = getWebsiteMetadata(locale);

  return {
    title: {
      template: ' %s | Matheus Martins',
      default: 'Matheus Martins',
    },
  };
}

export default async function Layout({ children, params }: PropsWithChildren<PropsWithLocaleParams>): Promise<React.JSX.Element> {
  const { locale } = await params;

  if (!hasLocale(routing.locales, locale)) notFound();

  // Enable static rendering.
  setRequestLocale(locale);

  return (
    <html lang={locale} suppressHydrationWarning>
      <body>
        <NextIntlClientProvider>
          {/* TODO: Add the Google Analytics and Google Tag Manager components here */}
          {/*<GoogleAnalytics gaId={environment.googleAnalyticsMeasurementId} />*/}
          {/*<GoogleTagManager gtmId={environment.googleTagManagerId} />*/}
          {children}
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
