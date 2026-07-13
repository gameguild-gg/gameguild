'use client';

import React, { useEffect, useState } from 'react';
import { Button } from '@game-guild/ui/components/button';
import { Badge } from '@game-guild/ui/components/badge';
import { Archive, Eye, EyeOff, Globe, Loader2, Lock } from 'lucide-react';
import { publishCourse, unpublishCourse } from '@/lib/learning/actions';

interface CourseLifecycleActionsProps {
  courseId: string;
  status: 'draft' | 'published' | 'archived';
  locale: string;
}

export function CourseLifecycleActions({ courseId, status, locale }: CourseLifecycleActionsProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentStatus, setCurrentStatus] = useState(status);

  useEffect(() => {
    setCurrentStatus(status);
  }, [status]);

  async function handleAction(action: 'publish' | 'unpublish') {
    setError(null);
    setIsSubmitting(true);

    try {
      let result;
      switch (action) {
        case 'publish':
          result = await publishCourse(courseId);
          break;
        case 'unpublish':
          result = await unpublishCourse(courseId);
          break;
      }
      if (result && !result.success) {
        setError(result.error);
      } else {
        if (action === 'publish') setCurrentStatus('published');
        if (action === 'unpublish') setCurrentStatus('draft');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'The course lifecycle action failed.');
    } finally {
      setIsSubmitting(false);
    }
  }

  const statusConfig = {
    draft: {
      icon: <Lock className="size-4" />,
      label: 'Draft',
      variant: 'secondary' as const,
      message: 'This course is in draft mode and not visible to students.',
      bgClass: 'border-amber-200 bg-amber-50 dark:border-amber-900 dark:bg-amber-950',
    },
    published: {
      icon: <Globe className="size-4" />,
      label: 'Published',
      variant: 'default' as const,
      message: 'This course is live and visible to students.',
      bgClass: 'border-green-200 bg-green-50 dark:border-green-900 dark:bg-green-950',
    },
    archived: {
      icon: <Archive className="size-4" />,
      label: 'Archived',
      variant: 'outline' as const,
      message: 'This course is archived and no longer accepting enrollments.',
      bgClass: 'border-gray-200 bg-gray-50 dark:border-gray-800 dark:bg-gray-950',
    },
  };

  const config = statusConfig[currentStatus];

  return (
    <div className="flex flex-col gap-3">
      <div className={`flex items-center justify-between rounded-lg border p-4 ${config.bgClass}`}>
        <div className="flex items-center gap-3">
          <Badge variant={config.variant} className="gap-1">
            {config.icon}
            {config.label}
          </Badge>
          <span className="text-sm text-muted-foreground">{config.message}</span>
        </div>
        <div className="flex items-center gap-2">
          {currentStatus === 'draft' && (
            <Button size="sm" onClick={() => handleAction('publish')} disabled={isSubmitting}>
              {isSubmitting ? <Loader2 className="mr-1 size-3 animate-spin" /> : <Eye className="mr-1 size-3" />}
              Publish
            </Button>
          )}
          {currentStatus === 'published' && (
            <>
              <Button size="sm" variant="outline" onClick={() => handleAction('unpublish')} disabled={isSubmitting}>
                {isSubmitting ? <Loader2 className="mr-1 size-3 animate-spin" /> : <EyeOff className="mr-1 size-3" />}
                Unpublish
              </Button>
            </>
          )}
          {currentStatus === 'archived' && (
            <Button size="sm" variant="outline" onClick={() => handleAction('publish')} disabled={isSubmitting}>
              {isSubmitting ? <Loader2 className="mr-1 size-3 animate-spin" /> : <Eye className="mr-1 size-3" />}
              Re-publish
            </Button>
          )}
        </div>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">{error}</div>
      )}
    </div>
  );
}
