'use client';

import { Link } from '@/i18n/navigation';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardDescription, CardFooter, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@game-guild/ui/components/dropdown-menu';
import { Archive, BookOpen, Edit, Eye, MoreHorizontal, Star, Trash2, Users } from 'lucide-react';

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
    title: string;
    status: string;
    visibility: string;
    enrolledCount: number;
    completionPercent: number;
    avgRating: string | null;
  };
  locale: string;
}

export function CourseCard({ course, locale }: CourseCardProps) {
  void locale;
  return (
    <Card className="flex h-full flex-col overflow-hidden transition-shadow hover:shadow-lg">
      <Link href={`/dashboard/learning/courses/${course.id}`} className="relative aspect-video bg-muted">
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
                <MoreHorizontal className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem>
                <Edit className="mr-2 size-4" />
                Edit Course
              </DropdownMenuItem>
              <DropdownMenuItem>
                <Eye className="mr-2 size-4" />
                Preview
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              {course.status === 'published' && (
                <DropdownMenuItem>
                  <Archive className="mr-2 size-4" />
                  Archive
                </DropdownMenuItem>
              )}
              {course.status === 'draft' && (
                <DropdownMenuItem>
                  <Eye className="mr-2 size-4" />
                  Publish
                </DropdownMenuItem>
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem className="text-destructive">
                <Trash2 className="mr-2 size-4" />
                Delete
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
        <CardTitle className="line-clamp-2 text-lg">
          <Link href={`/dashboard/learning/courses/${course.id}`}>{course.title}</Link>
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
        {course.completionPercent > 0 && <div className="w-full text-xs text-muted-foreground">{course.completionPercent}% completion rate</div>}
      </CardFooter>
    </Card>
  );
}

export function CourseTableActions({ courseId, locale }: { courseId: string; locale: string }) {
  void locale;
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="size-8">
          <MoreHorizontal className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem asChild>
          <Link href={`/dashboard/learning/courses/${courseId}`}>
            <Edit className="mr-2 size-4" />
            Edit
          </Link>
        </DropdownMenuItem>
        <DropdownMenuItem>
          <Eye className="mr-2 size-4" />
          Preview
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem className="text-destructive">
          <Trash2 className="mr-2 size-4" />
          Delete
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
