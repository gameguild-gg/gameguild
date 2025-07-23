import { GracefullyDegradingErrorBoundary } from '@/components/errors';
import { Inter } from 'next/font/google';
import './globals.css';

const inter = Inter({ subsets: ['latin'] });

export const metadata = {
  title: 'Game Guild',
  description: 'Modern gaming community platform',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" className={inter.className}>
      <body className="min-h-screen bg-background font-sans antialiased">
        <GracefullyDegradingErrorBoundary
          onError={(error, errorInfo) => {
            // Global error reporting
            console.error('Application Error:', error, errorInfo);
            
            // Send to error monitoring service
            if (typeof window !== 'undefined' && window.Sentry) {
              window.Sentry.captureException(error, {
                contexts: {
                  react: {
                    componentStack: errorInfo.componentStack,
                  },
                },
                tags: {
                  errorBoundary: 'root',
                },
              });
            }
          }}
          resetKeys={[]} // Add user ID or route changes here if needed
        >
          <div className="relative flex min-h-screen flex-col">
            <main className="flex-1">{children}</main>
          </div>
        </GracefullyDegradingErrorBoundary>
      </body>
    </html>
  );
}
