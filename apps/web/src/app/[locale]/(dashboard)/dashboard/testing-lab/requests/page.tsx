import { Suspense } from 'react';
import { ErrorBoundary } from '@/components/legacy/custom/error-boundary';

import { TestingRequestsList } from '@/components/testing-lab/testing-requests-list';

function LoadingFallback() {
  return (
    <div className="flex items-center justify-center min-h-[400px]">
      <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
    </div>
  );
}

export default function RequestsPage() {
  return (
    <div className="container mx-auto px-6 py-8">
      <ErrorBoundary fallback={<div className="text-red-500">Failed to load testing requests interface</div>}>
        <Suspense fallback={<LoadingFallback />}>
          <TestingRequestsList />
        </Suspense>
      </ErrorBoundary>
    </div>
  );
}
