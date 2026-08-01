import type { CourseAttendanceData } from '@/lib/learner/courses';
import { createLearnerRoutes } from '@/lib/learner/routes';
import { Badge } from '@game-guild/ui/components/badge';
import { CheckCircle2, ChevronRight, Circle, Clock3, Lock } from 'lucide-react';
import Link from 'next/link';

function statusIcon(status: string) {
  if (status === 'completed') return <CheckCircle2 className="size-4 text-emerald-500" />;
  if (status === 'locked') return <Lock className="size-4 text-muted-foreground" />;
  if (status === 'in-progress') return <Clock3 className="size-4 text-primary" />;
  return <Circle className="size-4 text-muted-foreground" />;
}

export function CourseContentOutline({ course }: { course: CourseAttendanceData }) {
  const routes = createLearnerRoutes();

  return (
    <div className="space-y-8">
      <header className="border-b pb-6">
        <p className="text-sm font-medium text-primary">{course.title}</p>
        <h1 className="mt-2 text-3xl font-semibold">Course content</h1>
        <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
          Follow the published syllabus in order. Locked lessons become available when their
          prerequisites are complete.
        </p>
      </header>

      <div className="space-y-6">
        {course.modules.map((module, moduleIndex) => (
          <section key={module.id} aria-labelledby={`module-${module.id}`} className="border-y">
            <div className="flex flex-wrap items-center justify-between gap-3 bg-muted/30 px-4 py-4">
              <div>
                <p className="text-xs font-medium uppercase text-muted-foreground">
                  Module {moduleIndex + 1}
                </p>
                <h2 id={`module-${module.id}`} className="mt-1 font-semibold">
                  {module.title}
                </h2>
              </div>
              <Badge variant="outline">{module.progress}% complete</Badge>
            </div>

            <div className="divide-y">
              {module.items.map((item) => {
                const isActivity = item.type !== 'lesson';
                const isParticipatory = ['Discussion', 'Reflection', 'Survey'].includes(item.contentType || '');
                const href = !isActivity
                  ? routes.lesson(course.slug, item.id)
                  : isParticipatory
                    ? routes.activity(course.slug, `content-${item.id}`)
                    : routes.activities(course.slug);
                const locked = item.status === 'locked';

                if (locked) {
                  return (
                    <div key={item.id} className="flex items-center gap-3 px-4 py-4 opacity-60">
                      {statusIcon(item.status)}
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium">{item.title}</p>
                        <p className="mt-1 text-xs text-muted-foreground">Locked</p>
                      </div>
                    </div>
                  );
                }

                return (
                  <Link
                    key={item.id}
                    href={href}
                    className="flex items-center gap-3 px-4 py-4 transition-colors hover:bg-muted/40"
                  >
                    {statusIcon(item.status)}
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium">{item.title}</p>
                      <p className="mt-1 text-xs text-muted-foreground">
                        {isActivity ? item.type : 'Lesson'}
                        {item.duration ? ` · ${item.duration} min` : ''}
                      </p>
                    </div>
                    <ChevronRight className="size-4 text-muted-foreground" />
                  </Link>
                );
              })}
            </div>
          </section>
        ))}
      </div>
    </div>
  );
}
