'use client';

import { completeCourseContent, beginCourseContent } from '@/lib/learner/progress-actions';
import { Button } from '@game-guild/ui/components/button';
import { CheckCircle2, PlayCircle } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';

interface LessonProgressControlsProps {
  contentId: string;
  courseId: string;
  status: 'locked' | 'available' | 'in-progress' | 'completed';
}

export function LessonProgressControls({
  contentId,
  courseId,
  status,
}: LessonProgressControlsProps) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [refreshing, startRefreshTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const busy = pending || refreshing;

  async function mutate(action: typeof beginCourseContent) {
    setPending(true);
    setError(null);
    try {
      const result = await action(courseId, contentId);
      if (!result.success) {
        setError(result.error);
        return;
      }
      startRefreshTransition(() => router.refresh());
    } catch (mutationError) {
      setError(
        mutationError instanceof Error
          ? mutationError.message
          : 'Unable to update lesson progress.',
      );
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-2">
        {status === 'available' ? (
          <Button disabled={busy} onClick={() => void mutate(beginCourseContent)}>
            <PlayCircle className="size-4" />
            {busy ? 'Starting...' : 'Start lesson'}
          </Button>
        ) : null}
        {status === 'available' || status === 'in-progress' ? (
          <Button
            variant="outline"
            disabled={busy}
            onClick={() => void mutate(completeCourseContent)}
          >
            <CheckCircle2 className="size-4" />
            {busy ? 'Saving...' : 'Mark complete'}
          </Button>
        ) : null}
        {status === 'completed' ? (
          <span className="inline-flex items-center gap-2 text-sm text-emerald-600">
            <CheckCircle2 className="size-4" />
            Completed
          </span>
        ) : null}
      </div>
      {error ? (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  );
}
