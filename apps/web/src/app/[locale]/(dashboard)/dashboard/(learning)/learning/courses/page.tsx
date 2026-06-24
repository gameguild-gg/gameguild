import { Link } from '@/i18n/navigation';
import { getCourses } from '@/lib/learning';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { AlertTriangle, ArrowLeft, BarChart3, BookOpen, Eye, Plus, RefreshCw } from 'lucide-react';
import React from 'react';
import { CourseList } from './course-list';

export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/courses'>): Promise<React.JSX.Element> {
  const { locale } = await params;

  const { courses, error } = await getCourses();
  const publishedCourses = courses.filter((course) => course.status === 'published').length;
  const publicCourses = courses.filter((course) => course.visibility === 'public').length;
  const totalEnrollments = courses.reduce((total, course) => total + course.enrolledCount, 0);

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link href="/dashboard/learning" locale={locale}>
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
        <div className="flex flex-wrap gap-2">
          <Button asChild variant="outline">
            <Link href="/courses" locale={locale}>
              <Eye className="mr-2 size-4" />
              Storefront preview
            </Link>
          </Button>
          <Button asChild>
            <Link href="/dashboard/learning/courses/new" locale={locale}>
              <Plus className="mr-2 size-4" />
              Create Course
            </Link>
          </Button>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="flex items-center justify-between gap-4 p-5">
            <div>
              <p className="text-sm font-medium text-muted-foreground">Courses</p>
              <p className="text-2xl font-semibold">{courses.length}</p>
            </div>
            <BookOpen className="size-5 text-muted-foreground" />
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center justify-between gap-4 p-5">
            <div>
              <p className="text-sm font-medium text-muted-foreground">Published</p>
              <p className="text-2xl font-semibold">{publishedCourses}</p>
            </div>
            <Eye className="size-5 text-muted-foreground" />
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center justify-between gap-4 p-5">
            <div>
              <p className="text-sm font-medium text-muted-foreground">Public catalog</p>
              <p className="text-2xl font-semibold">{publicCourses}</p>
            </div>
            <Eye className="size-5 text-muted-foreground" />
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center justify-between gap-4 p-5">
            <div>
              <p className="text-sm font-medium text-muted-foreground">Enrollments</p>
              <p className="text-2xl font-semibold">{totalEnrollments}</p>
            </div>
            <BarChart3 className="size-5 text-muted-foreground" />
          </CardContent>
        </Card>
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
              <Link href="/dashboard/learning/courses" locale={locale}>
                <RefreshCw className="mr-2 size-4" />
                Retry
              </Link>
            </Button>
          </CardContent>
        </Card>
      ) : null}

      {!error && courses.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center gap-4 py-12 text-center">
            <div className="flex size-14 items-center justify-center rounded-2xl bg-muted">
              <BookOpen className="size-7 text-muted-foreground" />
            </div>
            <div>
              <h3 className="text-lg font-semibold">No courses in the live catalog</h3>
              <p className="mt-1 max-w-md text-sm text-muted-foreground">
                Create or seed courses here. The public storefront reads from the same course source, so published changes can be previewed immediately.
              </p>
            </div>
            <div className="flex flex-wrap justify-center gap-2">
              <Button asChild>
                <Link href="/dashboard/learning/courses/new" locale={locale}>
                  <Plus className="mr-2 size-4" />
                  Create Course
                </Link>
              </Button>
              <Button asChild variant="outline">
                <Link href="/courses" locale={locale}>
                  <Eye className="mr-2 size-4" />
                  Open storefront
                </Link>
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : null}

      {courses.length > 0 ? (
        <CourseList courses={courses} locale={locale} />
      ) : null}
    </div>
  );
}
