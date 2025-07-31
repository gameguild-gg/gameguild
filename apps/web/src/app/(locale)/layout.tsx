import { auth } from '@/auth';
import { WebVitals } from '@/components/analytics';
import { NewRelicProvider } from '@/components/analytics/newrelic-provider';
import { ErrorBoundaryProvider } from '@/components/common/error/error-boundary-provider';
import { TenantProvider } from '@/components/tenant';
import { ThemeProvider } from '@/components/theme';
import { Toaster } from '@/components/ui/sonner';
import { Web3Provider } from '@/components/web3/web3-context';
import { environment } from '@/configs/environment';
import { GoogleAnalytics, GoogleTagManager } from '@next/third-parties/google';
import { SessionProvider } from 'next-auth/react';
import React, { PropsWithChildren } from 'react';

// TODO: Uncomment this when you have the metadata fetching logic ready.
// export async function generateMetadata(): Promise<Metadata> {
//   // TODO: Use the locale to fetch metadata if needed.
//   // const metadata = getWebsiteMetadata();
//
//   return {
//     title: {
//       template: ' %s | Game Guild',
//       default: 'Game Guild',
//     },
//   };
// }

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  const session = await auth();

  return (
    <html lang="en" suppressHydrationWarning>
      <body>
        <WebVitals />
        <GoogleAnalytics gaId={environment.googleAnalyticsMeasurementId} />
        <GoogleTagManager gtmId={environment.googleTagManagerId} />
        <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
          <ErrorBoundaryProvider config={{ level: 'page', enableRetry: true, maxRetries: 3, reportToAnalytics: true, isolate: false }}>
            {/* TODO: If the session has a user and it has signed-in by a web3 address then try to connect to the wallet address. */}
            <Web3Provider>
              <SessionProvider session={session}>
                <TenantProvider initialState={{ currentTenant: session?.currentTenant, availableTenants: session?.availableTenants }}>
                  <NewRelicProvider>
                    {/*TODO: Move this to a better place*/}
                    {/*<ThemeToggle />*/}
                    {children}
                    {/*TODO: Move this to a better place*/}
                    {/*<FeedbackFloatingButton />*/}
                    <Toaster />
                  </NewRelicProvider>
                </TenantProvider>
              </SessionProvider>
            </Web3Provider>
          </ErrorBoundaryProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
