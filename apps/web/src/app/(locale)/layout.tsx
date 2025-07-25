import React, { PropsWithChildren } from 'react';
import { GoogleAnalytics, GoogleTagManager } from '@next/third-parties/google';
import { SessionProvider } from 'next-auth/react';
import { WebVitals } from '@/components/analytics';
import { ThemeProvider } from '@/components/theme';
import { Web3Provider } from '@/components/web3/web3-context';
import { TenantProvider } from '@/components/tenant';
import { ErrorBoundaryProvider } from '@/components/common/error/error-boundary-provider';
import { environment } from '@/configs/environment';
import { auth } from '@/auth';
import { Toaster } from '@/components/ui/sonner';

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
                  {/*TODO: Move this to a better place*/}
                  {/*<ThemeToggle />*/}
                  {children}
                  {/*TODO: Move this to a better place*/}
                  {/*<FeedbackFloatingButton />*/}
                  <Toaster />
                </TenantProvider>
              </SessionProvider>
            </Web3Provider>
          </ErrorBoundaryProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
