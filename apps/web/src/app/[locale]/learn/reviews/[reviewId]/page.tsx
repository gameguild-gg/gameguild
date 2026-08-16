import { ReviewWorkspace } from './review-workspace';

export default async function ReviewWorkspacePage({ params }: { params: Promise<{ locale: string; reviewId: string }> }) {
  const { reviewId } = await params;
  return <ReviewWorkspace reviewId={reviewId} />;
}
