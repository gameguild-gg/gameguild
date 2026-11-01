import React from 'react';
import { fetchPeerReviewWorkspace } from '@/lib/learning/actions-peer-review';
import { ReviewWorkspaceForm } from './review-workspace';

/**
 * Peer-review page (issue #106 phase 1): the anonymous review payload is
 * fetched on the server with the viewer's session token, so the submission
 * and rubric render in the initial response — no client fetch waterfall.
 */
export default async function ReviewWorkspacePage({
  params,
}: {
  params: Promise<{ locale: string; reviewId: string }>;
}): Promise<React.JSX.Element> {
  const { reviewId } = await params;
  const result = await fetchPeerReviewWorkspace(reviewId);

  if (!result.ok) {
    return (
      <div role="alert" className="p-4 text-sm text-destructive">
        {result.error}
      </div>
    );
  }

  return <ReviewWorkspaceForm review={result.review} />;
}
