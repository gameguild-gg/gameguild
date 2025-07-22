// Example: Root layout with error boundary setup
// app/layout.tsx

import { Inter } from 'next/font/google';
import { ErrorBoundaryProvider } from '@/components/errors';
import './globals.css';

const inter = Inter({ subsets: ['latin'] });

export const metadata = {
  title: 'Game Guild',
  description: 'Gaming community platform',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body className={inter.className}>
        <ErrorBoundaryProvider
          config={{
            enableRetry: true,
            maxRetries: 3,
            retryDelay: 1000,
            level: 'page',
            reportToAnalytics: true,
            isolate: false,
          }}
        >
          {children}
        </ErrorBoundaryProvider>
      </body>
    </html>
  );
}

// Example: Page-level error boundary
// app/dashboard/page.tsx

import { SmartErrorBoundary } from '@/components/errors';
import { DashboardHeader, DashboardSidebar, DashboardContent } from '@/components/dashboard';

export default function DashboardPage() {
  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header with its own error boundary */}
      <SmartErrorBoundary level="component" isolate>
        <DashboardHeader />
      </SmartErrorBoundary>

      <div className="flex">
        {/* Sidebar with retry capability */}
        <SmartErrorBoundary 
          enableRetry 
          maxRetries={2} 
          level="component"
          isolate
        >
          <DashboardSidebar />
        </SmartErrorBoundary>

        {/* Main content area */}
        <main className="flex-1">
          <SmartErrorBoundary 
            level="page"
            onError={(error, errorInfo) => {
              // Custom logging for main content errors
              console.error('Dashboard main content error:', { error, errorInfo });
            }}
          >
            <DashboardContent />
          </SmartErrorBoundary>
        </main>
      </div>
    </div>
  );
}

// Example: Component with async error handling
// components/UserProfile.tsx

'use client';

import { useState, useEffect } from 'react';
import { useErrorBoundary, SmartErrorBoundary } from '@/components/errors';

interface User {
  id: string;
  name: string;
  email: string;
}

function UserProfileContent({ userId }: { userId: string }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const { captureError } = useErrorBoundary();

  useEffect(() => {
    async function fetchUser() {
      try {
        setLoading(true);
        const response = await fetch(`/api/users/${userId}`);
        
        if (!response.ok) {
          throw new Error(`Failed to fetch user: ${response.statusText}`);
        }
        
        const userData = await response.json();
        setUser(userData);
      } catch (error) {
        // This will trigger the error boundary
        captureError(error instanceof Error ? error : new Error('Unknown error'));
      } finally {
        setLoading(false);
      }
    }

    if (userId) {
      fetchUser();
    }
  }, [userId, captureError]);

  if (loading) {
    return (
      <div className="animate-pulse p-6">
        <div className="h-4 bg-gray-200 rounded w-3/4 mb-4"></div>
        <div className="h-4 bg-gray-200 rounded w-1/2"></div>
      </div>
    );
  }

  if (!user) {
    return <div className="p-6 text-gray-500">User not found</div>;
  }

  return (
    <div className="p-6 bg-white rounded-lg shadow">
      <h2 className="text-xl font-semibold mb-2">{user.name}</h2>
      <p className="text-gray-600">{user.email}</p>
    </div>
  );
}

export function UserProfile({ userId }: { userId: string }) {
  return (
    <SmartErrorBoundary
      enableRetry
      maxRetries={3}
      level="component"
      resetKeys={[userId]} // Reset error when userId changes
      onError={(error) => {
        console.error(`User profile error for user ${userId}:`, error);
      }}
    >
      <UserProfileContent userId={userId} />
    </SmartErrorBoundary>
  );
}

// Example: Custom error fallback for specific use cases
// components/GameListErrorFallback.tsx

import type { ErrorFallbackProps } from '@/components/errors';

export function GameListErrorFallback({ error, resetError }: ErrorFallbackProps) {
  const isNetworkError = error.message.includes('fetch') || error.message.includes('network');
  
  return (
    <div className="bg-gradient-to-br from-blue-50 to-indigo-100 dark:from-blue-950 dark:to-indigo-900 p-6 rounded-lg border border-blue-200 dark:border-blue-800">
      <div className="flex items-center mb-4">
        <div className="w-10 h-10 bg-blue-100 dark:bg-blue-900 rounded-full flex items-center justify-center mr-3">
          <svg className="w-5 h-5 text-blue-600 dark:text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>
        <h3 className="text-lg font-semibold text-blue-900 dark:text-blue-100">
          Unable to Load Games
        </h3>
      </div>
      
      <p className="text-blue-700 dark:text-blue-300 mb-4">
        {isNetworkError 
          ? "We're having trouble connecting to our servers. Please check your internet connection and try again."
          : "Something went wrong while loading the game list. Our team has been notified."
        }
      </p>
      
      <div className="flex gap-3">
        <button
          onClick={resetError}
          className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg font-medium transition-colors"
        >
          Try Again
        </button>
        <button
          onClick={() => window.location.reload()}
          className="bg-gray-200 hover:bg-gray-300 dark:bg-gray-700 dark:hover:bg-gray-600 text-gray-900 dark:text-white px-4 py-2 rounded-lg font-medium transition-colors"
        >
          Refresh Page
        </button>
      </div>
    </div>
  );
}

// Usage with custom fallback
// components/GameList.tsx

import { SmartErrorBoundary } from '@/components/errors';
import { GameListErrorFallback } from './GameListErrorFallback';

export function GameList() {
  return (
    <SmartErrorBoundary
      gracefulFallback={GameListErrorFallback}
      level="component"
      onError={(error) => {
        // Custom analytics for game list errors
        if (typeof window !== 'undefined' && window.gtag) {
          window.gtag('event', 'exception', {
            description: 'Game list error',
            fatal: false,
            custom_map: { error_component: 'GameList' },
          });
        }
      }}
    >
      <GameListContent />
    </SmartErrorBoundary>
  );
}
