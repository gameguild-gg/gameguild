import type { CourseAttendanceData } from '@/lib/learner/courses';
import { createLearnerRoutes } from '@/lib/learner/routes';
import { Badge } from '@game-guild/ui/components/badge';
import { BookOpen, ChevronRight, Clock3 } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';

export function MyCoursesList({ courses }: { courses: CourseAttendanceData[] }) {
  const routes = createLearnerRoutes();

  return (
    <div className="space-y-8">
      <header className="border-b pb-6">
        <p className="text-sm font-medium text-primary">Learning library</p>
        <h1 className="mt-2 text-3xl font-semibold">My courses</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Courses tied to your authenticated enrollments.
        </p>
      </header>

      {courses.length === 0 ? (
        <section className="flex min-h-72 flex-col items-center justify-center border-y text-center">
          <BookOpen className="size-9 text-muted-foreground" />
          <h2 className="mt-4 text-lg font-semibold">No active courses</h2>
          <p className="mt-2 max-w-md text-sm text-muted-foreground">
            Enrolled courses appear here after access is confirmed.
          </p>
          <Link href={routes.catalog} className="mt-5 text-sm font-medium text-primary hover:underline">
            Browse the course catalog
          </Link>
        </section>
      ) : (
        <div className="divide-y border-y">
          {courses.map((course) => (
            <Link
              key={course.id}
              href={routes.course(course.slug)}
              className="grid gap-4 py-5 transition-colors hover:bg-muted/40 sm:grid-cols-[8rem_minmax(0,1fr)_10rem_auto] sm:items-center sm:px-3"
            >
              <div className="relative aspect-video overflow-hidden rounded-md bg-muted">
                {course.thumbnail ? (
                  <Image src={course.thumbnail} alt="" fill sizes="8rem" className="object-cover" />
                ) : (
                  <div className="flex h-full items-center justify-center">
                    <BookOpen className="size-6 text-muted-foreground" />
                  </div>
                )}
              </div>
              <div className="min-w-0">
                <h2 className="truncate font-semibold">{course.title}</h2>
                <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">
                  {course.currentItem?.title ?? course.description}
                </p>
              </div>
              <div>
                <Badge variant="outline">{course.overallProgress}% complete</Badge>
                <p className="mt-2 flex items-center gap-1 text-xs text-muted-foreground">
                  <Clock3 className="size-3.5" />
                  {Math.ceil(course.remainingMinutes / 60)}h remaining
                </p>
              </div>
              <ChevronRight className="size-4 text-muted-foreground" />
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
