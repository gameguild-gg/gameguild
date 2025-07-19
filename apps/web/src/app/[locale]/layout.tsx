import React, { PropsWithChildren } from 'react';
import { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { GoogleAnalytics, GoogleTagManager } from '@next/third-parties/google';
import { SessionProvider } from 'next-auth/react';
import { hasLocale, NextIntlClientProvider } from 'next-intl';
import { setRequestLocale } from 'next-intl/server';
import { WebVitals } from '@/components/analytics';
import { ThemeProvider } from '@/components/theme';
import { Web3Provider } from '@/components/web3/web3-context';
import { TenantProvider } from '@/lib/tenants/tenant-provider';
import { environment } from '@/configs/environment';
import { auth } from '@/auth';
import { routing } from '@/i18n/routing';
import { PropsWithLocaleParams } from '@/types';

export async function generateMetadata({ params }: PropsWithLocaleParams): Promise<Metadata> {
  const { locale } = await params;
  // TODO: Use the locale to fetch metadata if needed.
  // const metadata = getWebsiteMetadata(locale);

  return {
    title: {
      template: ' %s | Game Guild',
      default: 'Game Guild',
    },
  };
}

export default async function Layout({ children, params }: PropsWithChildren<PropsWithLocaleParams>): Promise<React.JSX.Element> {
  const session = await auth();
  const { locale } = await params;

  if (!hasLocale(routing.locales, locale)) notFound();

  // Enable static rendering (cache) based on the locale.
  setRequestLocale(locale);

  return (
    <html lang={locale} suppressHydrationWarning>
      <body>
        <WebVitals />
        <NextIntlClientProvider>
          <GoogleAnalytics gaId={environment.googleAnalyticsMeasurementId} />
          <GoogleTagManager gtmId={environment.googleTagManagerId} />
          <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
            {/*TODO: If the session has a user and it has signed-in by a web3 address then try to connect to the wallet address.*/}
            <Web3Provider>
              <SessionProvider session={session}>
                <TenantProvider>
                  {/*TODO: Move this to a better place*/}
                  {/*<ThemeToggle />*/}

                  {children}
                  {/*TODO: Move this to a better place*/}
                  {/*<FeedbackFloatingButton />*/}
                </TenantProvider>
              </SessionProvider>
            </Web3Provider>
          </ThemeProvider>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
