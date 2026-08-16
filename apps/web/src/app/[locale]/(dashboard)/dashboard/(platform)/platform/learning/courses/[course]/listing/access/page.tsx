'use client';

import React, { useEffect, useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Loader2, Save, Shield, Users } from 'lucide-react';
import { fetchCourse, updateCourse } from '@/lib/learning/actions';
import { CONTENT_VISIBILITIES, ENROLLMENT_STATUSES, formatEnumLabel } from '@/lib/learning/enums';
import type { CourseDetails } from '@/lib/learning/types';

function parseEnrollmentCap(value: string): number | null {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
}

export default function ListingAccessPage({ params }: { params: Promise<{ locale: string; course: string }> }) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [course, setCourse] = useState<CourseDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [courseId, setCourseId] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const [visibility, setVisibility] = useState('Public');
  const [enrollmentStatus, setEnrollmentStatus] = useState('Open');
  const [maxEnrollments, setMaxEnrollments] = useState('');
  const [enrollmentDeadline, setEnrollmentDeadline] = useState('');

  useEffect(() => {
    params.then(async (p) => {
      try {
        const data = await fetchCourse(p.course);
        if (data) {
          setCourseId(data.id);
          setCourse(data);
          const visMap: Record<string, string> = { public: 'Public', private: 'Private', unlisted: 'Internal' };
          setVisibility(visMap[data.visibility] ?? 'Public');
          setEnrollmentStatus(data.enrollmentStatus);
          setMaxEnrollments(data.maxEnrollments?.toString() ?? '');
          setEnrollmentDeadline(data.enrollmentDeadline?.split('T')[0] ?? '');
        }
      } finally {
        setLoading(false);
      }
    });
  }, [params]);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    startTransition(async () => {
      const result = await updateCourse({
        courseId,
        visibility,
        enrollmentStatus,
        maxEnrollments: parseEnrollmentCap(maxEnrollments),
        enrollmentDeadline: enrollmentDeadline || null,
      });
      if (result.success) {
        setSuccess(true);
        router.refresh();
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
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="size-5" />
            Listing visibility
          </CardTitle>
          <CardDescription>Control how this course appears in the public catalog and who can find it.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="course-visibility">Course visibility</Label>
            <Select value={visibility} onValueChange={setVisibility}>
              <SelectTrigger id="course-visibility">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {CONTENT_VISIBILITIES.map((value) => (
                  <SelectItem key={value} value={value}>
                    {formatEnumLabel(value)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="size-5" />
            Enrollment
          </CardTitle>
          <CardDescription>Configure how prospective students can join this course from the listing.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-5">
          <div className="flex flex-col gap-2">
            <Label htmlFor="course-enrollment-status">Enrollment status</Label>
            <Select value={enrollmentStatus} onValueChange={setEnrollmentStatus}>
              <SelectTrigger id="course-enrollment-status">
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

          <div className="flex flex-col gap-2">
            <Label htmlFor="maxEnrollments">Maximum enrollments</Label>
            <Input id="maxEnrollments" type="number" min="0" placeholder="0 for unlimited" value={maxEnrollments} onChange={(e) => setMaxEnrollments(e.target.value)} />
            <p className="text-xs text-muted-foreground">Use 0 or leave empty for unlimited enrollments.</p>
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="enrollmentDeadline">Enrollment deadline</Label>
            <Input id="enrollmentDeadline" type="date" value={enrollmentDeadline} onChange={(e) => setEnrollmentDeadline(e.target.value)} />
            <p className="text-xs text-muted-foreground">Optional. After this date, new enrollments are blocked.</p>
          </div>

          <div className="flex items-center gap-3 rounded-lg border bg-muted/50 p-3">
            <span className="text-sm text-muted-foreground">Current enrollment:</span>
            <Badge variant="secondary">{course.currentEnrollments} students</Badge>
            {course.isEnrollmentOpen ? <Badge className="bg-green-600">Open</Badge> : <Badge variant="outline">Closed</Badge>}
          </div>
        </CardContent>
      </Card>

      {error && <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">{error}</div>}
      {success && (
        <div className="rounded-md border border-green-200 bg-green-50 p-3 text-sm text-green-700 dark:border-green-900 dark:bg-green-950 dark:text-green-300">
          Listing access settings saved successfully.
        </div>
      )}

      <div className="flex gap-3">
        <Button type="submit" disabled={isPending}>
          {isPending ? (
            <>
              <Loader2 className="mr-2 size-4 animate-spin" /> Saving...
            </>
          ) : (
            <>
              <Save className="mr-2 size-4" /> Save Listing Access
            </>
          )}
        </Button>
      </div>
    </form>
  );
}
