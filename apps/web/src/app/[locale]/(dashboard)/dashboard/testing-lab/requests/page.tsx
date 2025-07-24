import { Suspense } from 'react';
import { Metadata } from 'next';
import { getTestingRequestsData } from '@/lib/testing-lab/testing-requests.actions';
import { TestingRequestProvider } from '@/lib/testing-lab/testing-requests.context';
import { TestingRequestManagementContent } from '@/components/testing-lab/testing-request-management-content';
import { Loader2 } from 'lucide-react';
import { ErrorBoundary } from '@/components/legacy/custom/error-boundary';

export const metadata: Metadata = {
  title: 'Testing Requests | Game Guild Dashboard',
  description: 'Manage testing requests, approve or reject submissions from developers.',
};

interface TestingRequestsPageProps {
  searchParams: Promise<{
    page?: string;
    limit?: string;
    search?: string;
    status?: string;
  }>;
}

export default async function TestingRequestsPage({ searchParams }: TestingRequestsPageProps) {
  const params = await searchParams;
  const page = parseInt(params.page || '1');
  const limit = parseInt(params.limit || '20');
  const search = params.search || '';
  const status = params.status || '';

  try {
    const requestsData = await getTestingRequestsData(page, limit, search, status);

    return (
      <ErrorBoundary fallback={<div className="text-red-500">Failed to load testing request management interface</div>}>
        <TestingRequestProvider initialRequests={requestsData.requests} initialPagination={requestsData.pagination}>
          <Suspense
            fallback={
              <div className="flex items-center justify-center p-4">
                <Loader2 className="h-4 w-4 animate-spin" />
              </div>
            }
          >
            <TestingRequestManagementContent />
          </Suspense>
        </TestingRequestProvider>
      </ErrorBoundary>
    );
  } catch (error) {
    console.error('Error loading testing requests page:', error);

    return (
      <ErrorBoundary fallback={<div className="text-red-500">Failed to load testing request management interface</div>}>
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Testing Requests</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Review and manage game testing requests from developers.</p>
        </div>
      </ErrorBoundary>
    );
  }
}
