'use client';

import React, { useEffect, useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import { Button } from '@game-guild/ui/components/button';
import { Badge } from '@game-guild/ui/components/badge';
import { AlertTriangle, Archive, Eye, EyeOff, Globe, Loader2, Lock, Trash2 } from 'lucide-react';
import { publishCourse, unpublishCourse, archiveCourse, deleteCourse } from '@/lib/learning/actions';

interface CourseLifecycleActionsProps {
  courseId: string;
  status: 'draft' | 'published' | 'archived';
  locale: string;
}

export function CourseLifecycleActions({ courseId, status, locale }: CourseLifecycleActionsProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [currentStatus, setCurrentStatus] = useState(status);

  useEffect(() => {
    setCurrentStatus(status);
  }, [status]);

  function handleAction(action: 'publish' | 'unpublish' | 'archive' | 'delete') {
    setError(null);
    startTransition(async () => {
      let result;
      switch (action) {
        case 'publish':
          result = await publishCourse(courseId);
          break;
        case 'unpublish':
          result = await unpublishCourse(courseId);
          break;
        case 'archive':
          result = await archiveCourse(courseId);
          break;
        case 'delete':
          result = await deleteCourse(courseId);
          if (result.success) {
            router.push(`/${locale}/dashboard/learning/courses`);
            return;
          }
          break;
      }
      if (result && !result.success) {
        setError(result.error);
      } else {
        if (action === 'publish') setCurrentStatus('published');
        if (action === 'unpublish') setCurrentStatus('draft');
        if (action === 'archive') setCurrentStatus('archived');
        router.refresh();
      }
    });
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
            <Button size="sm" onClick={() => handleAction('publish')} disabled={isPending}>
              {isPending ? <Loader2 className="mr-1 size-3 animate-spin" /> : <Eye className="mr-1 size-3" />}
              Publish
            </Button>
          )}
          {currentStatus === 'published' && (
            <>
              <Button size="sm" variant="outline" onClick={() => handleAction('unpublish')} disabled={isPending}>
                {isPending ? <Loader2 className="mr-1 size-3 animate-spin" /> : <EyeOff className="mr-1 size-3" />}
                Unpublish
              </Button>
              <Button size="sm" variant="outline" onClick={() => handleAction('archive')} disabled={isPending}>
                <Archive className="mr-1 size-3" />
                Archive
              </Button>
            </>
          )}
          {currentStatus === 'archived' && (
            <Button size="sm" variant="outline" onClick={() => handleAction('publish')} disabled={isPending}>
              {isPending ? <Loader2 className="mr-1 size-3 animate-spin" /> : <Eye className="mr-1 size-3" />}
              Re-publish
            </Button>
          )}
          {!confirmDelete ? (
            <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setConfirmDelete(true)} disabled={isPending}>
              <Trash2 className="size-3" />
            </Button>
          ) : (
            <div className="flex items-center gap-1">
              <Button size="sm" variant="destructive" onClick={() => handleAction('delete')} disabled={isPending}>
                {isPending ? <Loader2 className="mr-1 size-3 animate-spin" /> : <AlertTriangle className="mr-1 size-3" />}
                Confirm Delete
              </Button>
              <Button size="sm" variant="ghost" onClick={() => setConfirmDelete(false)} disabled={isPending}>
                Cancel
              </Button>
            </div>
          )}
        </div>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">{error}</div>
      )}
    </div>
  );
}
