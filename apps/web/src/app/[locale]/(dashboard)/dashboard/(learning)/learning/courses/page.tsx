import { Link } from '@/i18n/navigation';
import { getCourses } from '@/lib/learning';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { AlertTriangle, ArrowLeft, BookOpen, Plus, RefreshCw } from 'lucide-react';
import React from 'react';
import { CourseList } from './course-list';

export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/courses'>): Promise<React.JSX.Element> {
  const { locale } = await params;

  const { courses, error } = await getCourses();

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link href="/dashboard/learning">
              <ArrowLeft className="size-5" />
            </Link>
          </Button>
          <div className="flex size-12 items-center justify-center rounded-lg bg-linear-to-br from-emerald-500 to-teal-600">
            <BookOpen className="size-6 text-white" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Courses</h1>
            <p className="text-muted-foreground">Manage your courses and track performance.</p>
          </div>
        </div>
        <Button asChild>
          <Link href="/dashboard/learning/courses/new">
            <Plus className="mr-2 size-4" />
            Create Course
          </Link>
        </Button>
      </div>

      {error ? (
        <Card className="border-destructive/30 bg-destructive/5">
          <CardContent className="flex flex-col gap-4 p-6 sm:flex-row sm:items-start sm:justify-between">
            <div className="flex gap-4">
              <div className="flex size-11 shrink-0 items-center justify-center rounded-lg bg-destructive/10 text-destructive">
                <AlertTriangle className="size-5" />
              </div>
              <div>
                <h2 className="text-lg font-semibold">Courses could not be loaded</h2>
                <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
                  The learning API did not return the course catalog. This is not an empty course library.
                </p>
                <p className="mt-3 rounded-md bg-background/60 px-3 py-2 text-sm text-destructive">{error}</p>
              </div>
            </div>
            <Button asChild variant="outline" className="shrink-0">
              <Link href="/dashboard/learning/courses">
                <RefreshCw className="mr-2 size-4" />
                Retry
              </Link>
            </Button>
          </CardContent>
        </Card>
      ) : null}

      {!error && courses.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12 text-center">
            <BookOpen className="mb-4 size-12 text-muted-foreground" />
            <h3 className="text-lg font-semibold">No courses yet</h3>
            <p className="text-sm text-muted-foreground">Create your first course to start teaching.</p>
          </CardContent>
        </Card>
      ) : null}

      {courses.length > 0 ? (
        <CourseList courses={courses} locale={locale} />
      ) : null}
    </div>
  );
}
