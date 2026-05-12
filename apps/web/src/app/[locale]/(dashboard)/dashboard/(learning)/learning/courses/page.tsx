import { Link } from '@/i18n/navigation';
import { getCourses } from '@/lib/learning';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { ArrowLeft, BookOpen, Plus } from 'lucide-react';
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

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">
          <p className="font-medium">API Error</p>
          <p>{error}</p>
        </div>
      )}

      {courses.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12 text-center">
            <BookOpen className="mb-4 size-12 text-muted-foreground" />
            <h3 className="text-lg font-semibold">No courses yet</h3>
            <p className="text-sm text-muted-foreground">Create your first course to start teaching.</p>
          </CardContent>
        </Card>
      ) : (
        <CourseList courses={courses} locale={locale} />
      )}
    </div>
  );
}
