import { Suspense } from 'react';
import { ErrorBoundary } from '@/components/legacy/custom/error-boundary';

import { FeedbackManager } from '@/components/testing-lab/feedback-manager';

function LoadingFallback() {
  return (
    <div className="flex items-center justify-center min-h-[400px]">
      <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
    </div>
  );
}

export default function FeedbackPage() {
  return (
    <div className="container mx-auto px-6 py-8">
      <ErrorBoundary fallback={<div className="text-red-500">Failed to load feedback management interface</div>}>
        <Suspense fallback={<LoadingFallback />}>
          <FeedbackManager />
        </Suspense>
      </ErrorBoundary>
    </div>
  );
}
