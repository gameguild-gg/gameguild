import { auth }                              from '@/auth';
import { WebVitals }                         from '@/components/analytics';
import { ConditionalAnalytics, InitializeGoogleConsent } from '@/components/analytics/conditional-analytics';
import { ErrorBoundaryProvider }             from '@/components/common/errors/error-boundary-provider';
import { ApolloClientProvider }              from '@/components/providers/apollo-provider';
import { GitHubIssueProvider }               from '@/components/providers/github-issue-provider';
import { TenantProvider }                    from '@/components/tenant';
import { ThemeProvider }                     from '@/components/theme';
import { Toaster }                           from '@/components/ui/sonner';
import { Web3Provider }                      from '@/components/web3';
import { environment }                       from '@/configs/environment';
import { routing }                           from '@/i18n';
import { PropsWithLocaleParams }             from '@/types';
import { GoogleAnalytics, GoogleTagManager } from '@next/third-parties/google';
import { Metadata }                          from 'next';
import { SessionProvider }                   from 'next-auth/react';
import { hasLocale, NextIntlClientProvider } from 'next-intl';
import { getMessages, setRequestLocale }     from 'next-intl/server';
import { Inter }                             from "next/font/google";
import { notFound }                          from 'next/navigation';
import React, { PropsWithChildren }          from 'react';


const inter = Inter({ subsets: [ "latin" ] });

export async function generateMetadata({ params }: PropsWithLocaleParams): Promise<Metadata> {
  const { locale } = await params;

  console.debug('Generating metadata for locale:', locale);
  // TODO: Uncomment this when you have the metadata fetching logic ready.
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
  
  // Get messages for the client-side
  const messages = await getMessages();

  return (
    <html lang={ locale } suppressHydrationWarning>
    <body className={ inter.className }>
    <WebVitals/>

    {/* Initialize Google Consent Mode early */ }
    <InitializeGoogleConsent/>

    <NextIntlClientProvider locale={ locale } messages={ messages }>
      {/* Conditional Analytics - only loads when the user consents */ }
      <ConditionalAnalytics/>
      <GoogleAnalytics gaId={ environment.googleAnalyticsMeasurementId }/>
      <GoogleTagManager gtmId={ environment.googleTagManagerId }/>
      <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
        <ErrorBoundaryProvider config={ { level: 'page', enableRetry: true, maxRetries: 3, reportToAnalytics: true, isolate: false } }>
          {/* TODO: If the session has a user and it has signed-in by a web3 address then try to connect to the wallet address. */ }
          <ApolloClientProvider>
            <Web3Provider>
              <SessionProvider session={ session }>
                <TenantProvider initialState={ { currentTenant: session?.currentTenant, availableTenants: session?.availableTenants } }>
                  {/*TODO: Move this to a better place*/ }
                  {/*<ThemeToggle />*/ }
                  <GitHubIssueProvider/>
                  { children }
                  {/*TODO: Move this to a better place*/ }
                  {/*<FeedbackFloatingButton />*/ }
                  <Toaster/>
                </TenantProvider>
              </SessionProvider>
            </Web3Provider>
          </ApolloClientProvider>
        </ErrorBoundaryProvider>
      </ThemeProvider>
    </NextIntlClientProvider>
    </body>
    </html>
  );
}
