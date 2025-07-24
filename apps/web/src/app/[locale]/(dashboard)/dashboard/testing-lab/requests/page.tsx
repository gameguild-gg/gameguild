import { Suspense } from 'react';
import { Metadata } from 'next';
import { Loader2 } from 'lucide-react';
import { ErrorBoundary } from '@/components/legacy/custom/error-boundary';

export const metadata: Metadata = {
  title: 'Request Management | Game Guild Dashboard',
  description: 'Manage requests, review submissions, and handle approval workflows in the Game Guild platform.',
};

// interface RequestsPageProps {
//   searchParams: Promise<{
//     page?: string;
//     limit?: string;
//     search?: string;
//     status?: string;
//   }>;
// }

export default async function RequestsPage() {
  // Future implementation will use searchParams for: page, limit, search
  
  try {
    // const requestData = await getRequestsData(page, limit, search);

    return (
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Request Management</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Manage and review requests, submissions, and approval workflows across the platform.</p>
        </div>

        <ErrorBoundary fallback={<div className="text-red-500">Failed to load request management interface</div>}>
          <Suspense
            fallback={
              <div className="flex items-center justify-center p-4">
                <Loader2 className="h-4 w-4 animate-spin" />
              </div>
            }
          >
            <div className="space-y-6">
              <div className="flex items-center gap-2">
                <div className="h-6 w-6 bg-blue-600 rounded"></div>
                <div>
                  <h2 className="text-xl font-semibold">Requests (0)</h2>
                  <p className="text-sm text-gray-600">Manage and review platform requests</p>
                </div>
              </div>
              <div className="bg-white rounded-lg border p-8 text-center">
                <div className="h-12 w-12 bg-gray-100 rounded-full mx-auto mb-4"></div>
                <h3 className="text-lg font-medium mb-2">Request Management</h3>
                <p className="text-gray-500 mb-4">This page will show all requests with the same aesthetic as the users page.</p>
                <button className="bg-blue-600 text-white px-4 py-2 rounded-lg">New Request</button>
              </div>
            </div>
          </Suspense>
        </ErrorBoundary>
      </div>
    );
  } catch (error) {
    console.error('Error loading requests page:', error);

    return (
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Request Management</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Manage and review requests, submissions, and approval workflows across the platform.</p>
        </div>

        <div className="bg-red-50 border border-red-200 rounded-lg p-6">
          <h2 className="text-lg font-semibold text-red-800 mb-2">Unable to Load Requests</h2>
          <p className="text-red-600">There was an error loading the request data. Please check your connection and try again.</p>
        </div>
      </div>
    );
  }
}
