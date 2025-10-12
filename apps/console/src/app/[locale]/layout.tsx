import React from 'react';
import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { hasLocale, NextIntlClientProvider } from 'next-intl';
import { getMessages, setRequestLocale } from 'next-intl/server';
import { routing } from '@/i18n';

export async function generateStaticParams() {
  const locales = routing.locales;

  return locales.map((locale) => ({ locale: locale }));
}

export async function generateMetadata({ params }: LayoutProps<'/[locale]'>): Promise<Metadata> {
  const { locale } = await params;

  console.log('Generating metadata for locale:', locale);

  // TODO: Uncomment this when you have the metadata fetching logic ready.
  // const metadata = getWebsiteMetadata(locale);

  // TODO: Fetch chapter data based on the chapter slug.
  return {
    title: {
      template: ' %s | Console | GameGuild',
      default: 'Console | GameGuild',
    },
  };
}

export default async function Layout({ children, params }: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  const { locale } = await params;

  if (!hasLocale(routing.locales, locale)) notFound();

  // Enable static rendering (cache) based on the locale.
  setRequestLocale(locale);

  // Get messages for the client-side.
  const messages = await getMessages();

  return (
    <html lang={locale} suppressHydrationWarning>
      <body>
        <NextIntlClientProvider locale={locale} messages={messages}>
          {children}
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
