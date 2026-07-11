'use client';

import React, { useState, useTransition, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { AlertTriangle, Archive, Loader2, Trash2 } from 'lucide-react';
import { archiveCourse, deleteCourse, fetchCourse } from '@/lib/learning/actions';
import type { CourseDetails } from '@/lib/learning/types';

export default function DangerPage({ params }: { params: Promise<{ locale: string; course: string }> }) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [course, setCourse] = useState<CourseDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [courseId, setCourseId] = useState('');
  const [locale, setLocale] = useState('');
  const [error, setError] = useState<string | null>(null);

  // Delete confirmation
  const [deleteConfirm, setDeleteConfirm] = useState('');
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  useEffect(() => {
    params.then(async (p) => {
      setLocale(p.locale);
      try {
        const data = await fetchCourse(p.course);
        if (data) {
          setCourseId(data.id);
        }
        setCourse(data);
      } catch {
        // ignore
      }
      setLoading(false);
    });
  }, [params]);

  function handleArchive() {
    setError(null);
    startTransition(async () => {
      const result = await archiveCourse(courseId);
      if (result.success) {
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function handleDelete() {
    if (deleteConfirm !== course?.title) return;
    setError(null);
    startTransition(async () => {
      const result = await deleteCourse(courseId);
      if (result.success) {
        router.push(`/${locale}/dashboard/learning/courses`);
      } else {
        setError(result.error);
      }
    });
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center p-12">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!course) {
    return <div className="p-6 text-muted-foreground">Course not found.</div>;
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h2 className="text-lg font-semibold text-destructive">Danger Zone</h2>
        <p className="text-sm text-muted-foreground">Irreversible actions that affect your course.</p>
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">
          {error}
        </div>
      )}

      {/* Archive */}
      {course.status !== 'archived' && (
        <Card className="border-amber-200 dark:border-amber-900">
          <CardHeader>
            <div className="flex items-center gap-2">
              <Archive className="size-5 text-amber-600" />
              <CardTitle className="text-base">Archive this course</CardTitle>
            </div>
            <CardDescription>
              Archiving hides the course from students and stops new enrollments. Existing student data is preserved. You can unarchive later from the Overview page.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Button variant="outline" onClick={handleArchive} disabled={isPending}>
              {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Archive className="mr-2 size-4" />}
              Archive Course
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Delete */}
      <Card className="border-red-200 dark:border-red-900">
        <CardHeader>
          <div className="flex items-center gap-2">
            <AlertTriangle className="size-5 text-destructive" />
            <CardTitle className="text-base text-destructive">Delete this course</CardTitle>
          </div>
          <CardDescription>
            Once deleted, this course and all its content, assessments, student enrollments, and analytics data will be permanently removed. This action cannot be undone.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {!showDeleteConfirm ? (
            <Button variant="destructive" onClick={() => setShowDeleteConfirm(true)}>
              <Trash2 className="mr-2 size-4" />
              Delete Course
            </Button>
          ) : (
            <>
              <div className="flex flex-col gap-2">
                <Label htmlFor="delete-confirm">
                  Type <strong>{course.title}</strong> to confirm deletion
                </Label>
                <Input
                  id="delete-confirm"
                  value={deleteConfirm}
                  onChange={(e) => setDeleteConfirm(e.target.value)}
                  placeholder={course.title}
                  autoFocus
                />
              </div>
              <div className="flex gap-2">
                <Button
                  variant="destructive"
                  onClick={handleDelete}
                  disabled={isPending || deleteConfirm !== course.title}
                >
                  {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Trash2 className="mr-2 size-4" />}
                  Permanently Delete
                </Button>
                <Button variant="outline" onClick={() => { setShowDeleteConfirm(false); setDeleteConfirm(''); }}>
                  Cancel
                </Button>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
