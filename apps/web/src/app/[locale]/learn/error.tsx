'use client';

import { LearnerRouteError } from '@/components/learning/learner-route-state';

export default function LearningError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return <LearnerRouteError error={error} reset={reset} />;
}
