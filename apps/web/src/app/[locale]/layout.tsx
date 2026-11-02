import { Providers } from '@/components/providers';
import { routing } from '@/i18n';
import { hasLocale, NextIntlClientProvider } from 'next-intl';
import { getMessages, setRequestLocale } from 'next-intl/server';
import { notFound } from 'next/navigation';
import React from 'react';

// No per-locale metadata here (issue #103 dedup): the root layout already
// declares the shared `title` template and `metadataBase`, and every page
// below defines its own `generateMetadata` when it needs overrides.

export async function generateStaticParams() {
  return routing.locales.map((locale) => ({ locale }));
}

export default async function Layout({ children, params }: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  const { locale } = await params;

  if (!hasLocale(routing.locales, locale)) notFound();

  // Enable static rendering (cache) based on the locale.
  setRequestLocale(locale);

  // Get messages for the client-side.
  const messages = await getMessages();

  return (
    <NextIntlClientProvider locale={locale} messages={messages}>
      <Providers>
        {children}
      </Providers>
    </NextIntlClientProvider>
  );
}
