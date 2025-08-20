import React, { PropsWithChildren } from 'react';
import { ThemeProvider } from '@/components/theme/theme-provider';
import '@/styles/globals.css';
import { themeScript } from '@/lib/theme-script';

// Since we have a `not-found.tsx` page on the root, a layout file
// is required, even if it's just passing children through.
export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: themeScript }} />
      </head>
      <body>
          <ThemeProvider attribute="class" defaultTheme="system" enableSystem>
            {children}
          </ThemeProvider>
        </body>
    </html>
  );
}
