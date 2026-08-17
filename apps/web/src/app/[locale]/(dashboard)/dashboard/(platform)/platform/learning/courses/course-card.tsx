'use client';

import { Link } from '@/i18n/navigation';
import { buildPlatformCoursePath } from '@/lib/learning/course-route';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardDescription, CardFooter, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@game-guild/ui/components/dropdown-menu';
import { BookOpen, Edit, Eye, MoreHorizontal, Star, Users } from 'lucide-react';

function getStatusBadge(status: string) {
  switch (status) {
    case 'published':
      return <Badge className="bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">Published</Badge>;
    case 'draft':
      return <Badge variant="secondary">Draft</Badge>;
    case 'archived':
      return <Badge variant="outline">Archived</Badge>;
    default:
      return null;
  }
}

interface CourseCardProps {
  course: {
    id: string;
    slug?: string;
    routeParam?: string;
    title: string;
    status: string;
    visibility: string;
    enrolledCount: number;
    completionPercent: number | null;
    avgRating: string | null;
  };
  locale: string;
}

export function CourseCard({ course, locale }: CourseCardProps) {
  const coursePath = buildPlatformCoursePath(course.routeParam ?? course);
  const previewPath = buildPlatformCoursePath(course.routeParam ?? course, 'preview');
  const overviewPath = buildPlatformCoursePath(course.routeParam ?? course, 'overview');

  return (
    <Card className="flex h-full flex-col overflow-hidden transition-shadow hover:shadow-lg">
      <Link href={coursePath} locale={locale} prefetch={false} className="relative aspect-video bg-muted">
        <div className="absolute inset-0 flex items-center justify-center">
          <BookOpen className="size-12 text-muted-foreground" />
        </div>
      </Link>
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between gap-2">
          {getStatusBadge(course.status)}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="size-8">
                <span className="sr-only">Open {course.title} actions</span>
                <MoreHorizontal className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link href={coursePath} locale={locale} prefetch={false}>
                  <Edit className="mr-2 size-4" />
                  Edit Course
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={previewPath} locale={locale} prefetch={false}>
                  <Eye className="mr-2 size-4" />
                  Preview
                </Link>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem asChild>
                <Link href={overviewPath} locale={locale} prefetch={false}>
                  <BookOpen className="mr-2 size-4" />
                  Manage lifecycle
                </Link>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
        <CardTitle className="line-clamp-2 text-lg">
          <Link href={coursePath} locale={locale} prefetch={false}>{course.title}</Link>
        </CardTitle>
        <CardDescription className="line-clamp-1">{course.visibility}</CardDescription>
      </CardHeader>
      <CardFooter className="mt-auto flex flex-col gap-3 border-t pt-4">
        <div className="flex w-full items-center justify-between text-sm text-muted-foreground">
          <div className="flex items-center gap-1">
            <Users className="size-4" />
            <span>{course.enrolledCount} enrolled</span>
          </div>
          {course.avgRating && (
            <div className="flex items-center gap-1">
              <Star className="size-4 text-yellow-500" />
              <span>{course.avgRating}</span>
            </div>
          )}
        </div>
        {course.completionPercent !== null && <div className="w-full text-xs text-muted-foreground">{course.completionPercent}% completion rate</div>}
      </CardFooter>
    </Card>
  );
}

export function CourseTableActions({ courseRouteParam, courseTitle, locale }: { courseRouteParam: string; courseTitle: string; locale: string }) {
  const coursePath = buildPlatformCoursePath(courseRouteParam);
  const previewPath = buildPlatformCoursePath(courseRouteParam, 'preview');
  const overviewPath = buildPlatformCoursePath(courseRouteParam, 'overview');

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="size-8">
          <span className="sr-only">Open {courseTitle} actions</span>
          <MoreHorizontal className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem asChild>
          <Link href={coursePath} locale={locale} prefetch={false}>
            <Edit className="mr-2 size-4" />
            Edit
          </Link>
        </DropdownMenuItem>
        <DropdownMenuItem asChild>
          <Link href={previewPath} locale={locale} prefetch={false}>
            <Eye className="mr-2 size-4" />
            Preview
          </Link>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem asChild>
          <Link href={overviewPath} locale={locale} prefetch={false}>
            <BookOpen className="mr-2 size-4" />
            Manage lifecycle
          </Link>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
