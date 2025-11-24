import React, { PropsWithChildren } from 'react';
import type { Metadata } from 'next';
// TODO: next-intl package not installed - commenting out internationalization
// import { notFound } from 'next/navigation';
// import { hasLocale, NextIntlClientProvider } from 'next-intl';
// import { setRequestLocale } from 'next-intl/server';
// import { routing } from '@/i18n/routing';
// import { PropsWithLocaleParams } from '@/types';

interface PropsWithLocaleParams {
  params: Promise<{ locale: string }>;
}

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

  // TODO: Re-enable locale checking when next-intl is installed
  // if (!hasLocale(routing.locales, locale)) notFound();
  // setRequestLocale(locale);

  return (
    <html lang={locale} suppressHydrationWarning>
      <body>
        {/* TODO: Re-enable NextIntlClientProvider when next-intl is installed */}
        {/* TODO: Add the Google Analytics and Google Tag Manager components here */}
        {/*<GoogleAnalytics gaId={environment.googleAnalyticsMeasurementId} />*/}
        {/*<GoogleTagManager gtmId={environment.googleTagManagerId} />*/}
        {children}
      </body>
    </html>
  );
}
