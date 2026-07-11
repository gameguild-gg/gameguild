'use client';

import React, { useState, useTransition } from 'react';
import { updateCourse } from '@/lib/learning/actions';
import type { CourseDetails } from '@/lib/learning/types';
import { ENROLLMENT_STATUSES, formatEnumLabel } from '@/lib/learning/enums';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Loader2, Save } from 'lucide-react';

interface ListingLaunchFormProps {
  course: CourseDetails;
}

function toDateTimeLocal(value: string | null): string {
  if (!value) {
    return '';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return localDate.toISOString().slice(0, 16);
}

function toIsoDateTime(value: string): string | null {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return date.toISOString();
}

function parseEnrollmentCap(value: string): number | null {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
}

export function ListingLaunchForm({ course }: ListingLaunchFormProps) {
  const [isPending, startTransition] = useTransition();
  const [visibility, setVisibility] = useState<'public' | 'private'>(course.visibility === 'public' ? 'public' : 'private');
  const [enrollmentStatus, setEnrollmentStatus] = useState(course.enrollmentStatus || 'Open');
  const [enrollmentDeadline, setEnrollmentDeadline] = useState(toDateTimeLocal(course.enrollmentDeadline));
  const [maxEnrollments, setMaxEnrollments] = useState(course.maxEnrollments?.toString() ?? '');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(false);

    startTransition(async () => {
      const result = await updateCourse({
        courseId: course.id,
        visibility: visibility === 'public' ? 'Public' : 'Private',
        enrollmentStatus,
        enrollmentDeadline: toIsoDateTime(enrollmentDeadline),
        maxEnrollments: parseEnrollmentCap(maxEnrollments),
      });

      if (!result.success) {
        setError(result.error);
        return;
      }

      setSuccess(true);
    });
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Launch Controls</CardTitle>
        <CardDescription>
          Control visibility and enrollment on today&apos;s shared course contract. Teaser-first scheduling stays blocked until the API adds explicit launch timestamps.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="space-y-2">
            <Label htmlFor="catalog-visibility">Catalog visibility</Label>
            <Select value={visibility} onValueChange={(value: 'public' | 'private') => setVisibility(value)}>
              <SelectTrigger id="catalog-visibility">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="public">Public</SelectItem>
                <SelectItem value="private">Private</SelectItem>
              </SelectContent>
            </Select>
            <p className="text-xs text-muted-foreground">Public courses can appear in the catalog. Private courses stay hidden.</p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="listing-enrollment-status">Enrollment status</Label>
            <Select value={enrollmentStatus} onValueChange={setEnrollmentStatus}>
              <SelectTrigger id="listing-enrollment-status">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {ENROLLMENT_STATUSES.map((value) => (
                  <SelectItem key={value} value={value}>
                    {formatEnumLabel(value)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="enrollmentDeadline">Enrollment deadline</Label>
            <Input
              id="enrollmentDeadline"
              type="datetime-local"
              value={enrollmentDeadline}
              onChange={(event) => setEnrollmentDeadline(event.target.value)}
            />
            <p className="text-xs text-muted-foreground">Leave blank to keep enrollments open until manually closed.</p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="maxEnrollments">Enrollment cap</Label>
            <Input
              id="maxEnrollments"
              type="number"
              min="0"
              value={maxEnrollments}
              onChange={(event) => setMaxEnrollments(event.target.value)}
              placeholder="0 for unlimited"
            />
            <p className="text-xs text-muted-foreground">Use 0 or leave blank for unlimited seats.</p>
          </div>

          {error ? (
            <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">
              {error}
            </div>
          ) : null}

          {success ? (
            <div className="rounded-md border border-green-200 bg-green-50 p-3 text-sm text-green-700 dark:border-green-900 dark:bg-green-950 dark:text-green-300">
              Listing controls updated successfully.
            </div>
          ) : null}

          <Button type="submit" disabled={isPending} className="w-full">
            {isPending ? (
              <>
                <Loader2 className="mr-2 size-4 animate-spin" /> Saving...
              </>
            ) : (
              <>
                <Save className="mr-2 size-4" /> Save launch controls
              </>
            )}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
