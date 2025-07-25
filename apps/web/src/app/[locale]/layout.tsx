import React, { PropsWithChildren } from 'react';
import { notFound } from 'next/navigation';
import { GoogleAnalytics, GoogleTagManager } from '@next/third-parties/google';
import { SessionProvider } from 'next-auth/react';
import { hasLocale, NextIntlClientProvider } from 'next-intl';
import { setRequestLocale } from 'next-intl/server';
import { WebVitals } from '@/components/analytics';
import { ThemeProvider } from '@/components/theme';
import { Web3Provider } from '@/components/web3/context/web3-context';
import { TenantProvider } from '@/components/tenant';
import { ErrorBoundaryProvider } from '@/components/common/errors/error-boundary-provider';
import { environment } from '@/configs/environment';
import { auth } from '@/auth';
import { routing } from '@/i18n/routing';
import { PropsWithLocaleParams } from '@/types';
import { Toaster } from '@/components/ui/sonner';

// TODO: Uncomment this when you have the metadata fetching logic ready.
// export async function generateMetadata({ params }: PropsWithLocaleParams): Promise<Metadata> {
//   const { locale } = await params;
//   // TODO: Use the locale to fetch metadata if needed.
//   // const metadata = getWebsiteMetadata(locale);
//
//   return {
//     title: {
//       template: ' %s | Game Guild',
//       default: 'Game Guild',
//     },
//   };
// }

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
            <ErrorBoundaryProvider config={{ level: 'page', enableRetry: true, maxRetries: 3, reportToAnalytics: true, isolate: false }}>
              {/* TODO: If the session has a user and it has signed-in by a web3 address then try to connect to the wallet address. */}
              <Web3Provider>
                <SessionProvider session={session}>
                  <TenantProvider initialState={{ currentTenant: session?.currentTenant, availableTenants: session?.availableTenants }}>
                    {/*TODO: Move this to a better place*/}
                    {/*<ThemeToggle />*/}
                    {children}
                    {/*TODO: Move this to a better place*/}
                    {/*<FeedbackFloatingButton />*/}ø
                    <Toaster />
                  </TenantProvider>
                </SessionProvider>
              </Web3Provider>
            </ErrorBoundaryProvider>
          </ThemeProvider>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
