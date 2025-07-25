import { Suspense } from 'react';
import { Metadata } from 'next';
import { Loader2 } from 'lucide-react';
import { ErrorBoundary } from '@/components/legacy/custom/error-boundary';
import { auth } from '@/auth';
import { EnhancedTestingSessionsList } from '@/components/testing-lab/enhanced-testing-sessions-list';
import { getTestingSessionsData } from '@/lib/testing-lab/testing-lab.actions';

export const metadata: Metadata = {
  title: 'Testing Sessions | Game Guild Dashboard',
  description: 'Manage testing sessions, review submissions, and coordinate game testing activities in the Game Guild platform.',
};

interface SessionsPageProps {
  searchParams: Promise<{
    page?: string;
    limit?: string;
    search?: string;
    status?: string;
  }>;
}

export default async function TestingSessionsPage({ searchParams }: SessionsPageProps) {
  const params = await searchParams;
  // Future implementation will use searchParams for: page, limit, search

  try {
    const session = await auth();

    if (!session?.accessToken) {
      return (
        <div className="container mx-auto px-4 py-8">
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Testing Sessions</h1>
            <p className="text-gray-600 dark:text-gray-400 mt-2">Manage testing sessions, coordinate activities, and track game testing progress.</p>
          </div>

          <div className="flex items-center justify-center min-h-64">
            <div className="text-center">
              <Loader2 className="h-8 w-8 animate-spin mx-auto mb-4" />
              <p className="text-gray-600">Loading authentication...</p>
            </div>
          </div>
        </div>
      );
    }

    const sessionsData = await getTestingSessionsData();
    const sessions = sessionsData?.testingSessions || [];

    // Convert API sessions to component format
    const convertedSessions = sessions.map((session: any) => ({
      ...session,
      id: session.id || `session-${Date.now()}-${Math.random()}`,
    }));

    return (
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Testing Sessions</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Manage testing sessions, coordinate activities, and track game testing progress.</p>
        </div>

        <ErrorBoundary fallback={<div className="text-red-500">Failed to load testing sessions interface</div>}>
          <Suspense
            fallback={
              <div className="flex items-center justify-center p-4">
                <Loader2 className="h-4 w-4 animate-spin" />
              </div>
            }
          >
            <EnhancedTestingSessionsList initialSessions={convertedSessions} />
          </Suspense>
        </ErrorBoundary>
      </div>
    );
  } catch (error) {
    console.error('Error loading testing sessions page:', error);

    return (
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Testing Sessions</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Manage testing sessions, coordinate activities, and track game testing progress.</p>
        </div>

        <div className="bg-red-50 border border-red-200 rounded-lg p-6">
          <h2 className="text-lg font-semibold text-red-800 mb-2">Unable to Load Testing Sessions</h2>
          <p className="text-red-600">There was an error loading the testing session data. Please check your connection and try again.</p>
        </div>
      </div>
    );
  }
}
