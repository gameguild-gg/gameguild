import { Link } from '@/i18n/navigation';
import { getCourse, getCourseAnalytics, getCourseContent } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Progress } from '@game-guild/ui/components/progress';
import {
    AlertCircle,
    BookOpen,
    CheckCircle2,
    ClipboardList,
    Clock,
    Edit,
    FileText,
    Image,
    Settings,
    Star,
    TrendingUp,
    Users,
} from 'lucide-react';
import { notFound } from 'next/navigation';
import React from 'react';
import { CourseLifecycleActions } from './course-lifecycle-actions';

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/courses/[course]/overview'>): Promise<React.JSX.Element> {
  const { locale, course: courseId } = await params;

  const [course, analytics, content] = await Promise.all([getCourse(courseId), getCourseAnalytics(courseId), getCourseContent(courseId)]);

  if (!course) {
    notFound();
  }

  const totalEnrollments = analytics.enrollments.length;
  const completedCount = analytics.enrollments.filter((e) => e.completedAt).length;
  const completionRate = totalEnrollments > 0 ? Math.round((completedCount / totalEnrollments) * 100) : 0;
  const avgRating = analytics.ratings.length > 0 ? (analytics.ratings.reduce((acc, r) => acc + r.score, 0) / analytics.ratings.length).toFixed(1) : null;

  const modules = content.items.filter((i) => !i.parentId);
  const lessons = content.items.filter((i) => i.parentId);
  const totalDuration = content.items.reduce((acc, item) => acc + (item.duration ?? 0), 0);
  const durationStr = totalDuration >= 60 ? `${Math.floor(totalDuration / 60)}h ${totalDuration % 60}m` : `${totalDuration}m`;

  // Readiness checklist items
  const readinessChecks = [
    {
      label: 'Add a description',
      done: !!course.description?.trim(),
      href: `/dashboard/learning/courses/${courseId}/listing/info` as const,
      icon: FileText,
    },
    {
      label: 'Upload a cover image',
      done: !!course.thumbnail,
      href: `/dashboard/learning/courses/${courseId}/listing/media` as const,
      icon: Image,
    },
    {
      label: 'Create at least one module',
      done: modules.length > 0,
      href: `/dashboard/learning/courses/${courseId}/content` as const,
      icon: BookOpen,
    },
    {
      label: 'Add a lesson to a module',
      done: lessons.length > 0,
      href: `/dashboard/learning/courses/${courseId}/content` as const,
      icon: ClipboardList,
    },
  ];
  const readinessDone = readinessChecks.filter((c) => c.done).length;
  const readinessTotal = readinessChecks.length;
  const readinessPercent = Math.round((readinessDone / readinessTotal) * 100);

  return (
    <div className="flex flex-col gap-6">
      {/* Lifecycle Status Banner */}
      <CourseLifecycleActions courseId={courseId} status={course.status} locale={locale} />

      {/* Key Metrics */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex size-10 items-center justify-center rounded-lg bg-blue-100 dark:bg-blue-900">
              <Users className="size-5 text-blue-600 dark:text-blue-400" />
            </div>
            <div>
              <p className="text-2xl font-bold">{totalEnrollments}</p>
              <p className="text-sm text-muted-foreground">Enrolled</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex size-10 items-center justify-center rounded-lg bg-green-100 dark:bg-green-900">
              <CheckCircle2 className="size-5 text-green-600 dark:text-green-400" />
            </div>
            <div>
              <p className="text-2xl font-bold">{completionRate}%</p>
              <p className="text-sm text-muted-foreground">Completion Rate</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex size-10 items-center justify-center rounded-lg bg-purple-100 dark:bg-purple-900">
              <BookOpen className="size-5 text-purple-600 dark:text-purple-400" />
            </div>
            <div>
              <p className="text-2xl font-bold">{modules.length}</p>
              <p className="text-sm text-muted-foreground">Modules</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex size-10 items-center justify-center rounded-lg bg-orange-100 dark:bg-orange-900">
              <Clock className="size-5 text-orange-600 dark:text-orange-400" />
            </div>
            <div>
              <p className="text-2xl font-bold">{durationStr}</p>
              <p className="text-sm text-muted-foreground">Duration</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Main Content */}
      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          {/* Course Readiness */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>Course Readiness</CardTitle>
                <Badge variant={readinessPercent === 100 ? 'default' : 'secondary'}>
                  {readinessDone}/{readinessTotal} complete
                </Badge>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              <Progress value={readinessPercent} className="h-2" />
              <div className="space-y-2">
                {readinessChecks.map((check) => {
                  const Icon = check.icon;
                  return (
                    <Link
                      key={check.label}
                      href={check.href}
                      className="flex items-center gap-3 rounded-lg border p-3 transition-colors hover:bg-muted/50"
                    >
                      {check.done ? (
                        <CheckCircle2 className="size-5 shrink-0 text-green-500" />
                      ) : (
                        <AlertCircle className="size-5 shrink-0 text-amber-500" />
                      )}
                      <Icon className="size-4 shrink-0 text-muted-foreground" />
                      <span className={`text-sm ${check.done ? 'text-muted-foreground line-through' : 'font-medium'}`}>
                        {check.label}
                      </span>
                    </Link>
                  );
                })}
              </div>
            </CardContent>
          </Card>

          {/* Course Performance */}
          <Card>
            <CardHeader>
              <CardTitle>Course Performance</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 sm:grid-cols-3">
                {avgRating ? (
                  <div className="flex items-center gap-3 rounded-lg border p-3">
                    <Star className="size-5 text-yellow-500" />
                    <div>
                      <p className="text-lg font-bold">{avgRating}</p>
                      <p className="text-xs text-muted-foreground">{analytics.ratings.length} reviews</p>
                    </div>
                  </div>
                ) : (
                  <div className="flex items-center gap-3 rounded-lg border p-3">
                    <Star className="size-5 text-muted-foreground/40" />
                    <div>
                      <p className="text-sm text-muted-foreground">No ratings yet</p>
                    </div>
                  </div>
                )}
                <div className="flex items-center gap-3 rounded-lg border p-3">
                  <TrendingUp className="size-5 text-green-500" />
                  <div>
                    <p className="text-lg font-bold">{completedCount}</p>
                    <p className="text-xs text-muted-foreground">Completed</p>
                  </div>
                </div>
                <div className="flex items-center gap-3 rounded-lg border p-3">
                  <BookOpen className="size-5 text-purple-500" />
                  <div>
                    <p className="text-lg font-bold">{lessons.length}</p>
                    <p className="text-xs text-muted-foreground">Lessons</p>
                  </div>
                </div>
              </div>
              <div className="space-y-2">
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">Completion Rate</span>
                  <span className="font-medium">{completionRate}%</span>
                </div>
                <Progress value={completionRate} className="h-2" />
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Quick Actions */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Quick Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Button variant="outline" className="w-full justify-start" asChild>
                <Link href={`/dashboard/learning/courses/${courseId}/listing/info`}>
                  <Edit className="mr-2 size-4" />
                  Edit Course Info
                </Link>
              </Button>
              <Button variant="outline" className="w-full justify-start" asChild>
                <Link href={`/dashboard/learning/courses/${courseId}/content`}>
                  <BookOpen className="mr-2 size-4" />
                  Manage Content
                </Link>
              </Button>
              <Button variant="outline" className="w-full justify-start" asChild>
                <Link href={`/dashboard/learning/courses/${courseId}/students`}>
                  <Users className="mr-2 size-4" />
                  Manage Students
                </Link>
              </Button>
              <Button variant="outline" className="w-full justify-start" asChild>
                <Link href={`/dashboard/learning/courses/${courseId}/settings`}>
                  <Settings className="mr-2 size-4" />
                  Course Settings
                </Link>
              </Button>
            </CardContent>
          </Card>

          {/* Course Details */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Status</span>
                <Badge variant="outline" className="capitalize">{course.status}</Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Category</span>
                <Badge variant="outline">{course.category}</Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Difficulty</span>
                <Badge variant="outline">{course.difficulty}</Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Enrollment</span>
                <Badge variant="outline">{course.enrollmentStatus}</Badge>
              </div>
              {course.estimatedHours && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Est. Hours</span>
                  <span>{course.estimatedHours}h</span>
                </div>
              )}
              <div className="border-t pt-3">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Created</span>
                  <span>{formatDate(course.createdAt)}</span>
                </div>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Last Updated</span>
                <span>{formatDate(course.updatedAt)}</span>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
