import { CourseAccessGate } from '@/components/learning/course-access-gate';
import { LearnerLessonRenderer } from '@/components/learning/learner-lesson-renderer';
import { LessonProgressControls } from '@/components/learning/lesson-progress-controls';
import { getCourseAccessData } from '@/lib/learner/courses';
import { createLearnerRoutes } from '@/lib/learner/routes';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { ArrowLeft, ArrowRight, Lock } from 'lucide-react';
import Link from 'next/link';
import { notFound } from 'next/navigation';

export default async function LessonPage({
  params,
}: {
  params: Promise<{ lessonId: string; slug: string }>;
}) {
  const { lessonId, slug } = await params;
  const access = await getCourseAccessData(slug);

  if (access.kind === 'not-found') notFound();
  if (access.kind !== 'ready') return <CourseAccessGate access={access} />;

  const items = access.course.modules.flatMap((module) => module.items);
  const itemIndex = items.findIndex((candidate) => candidate.id === lessonId);
  const item = items[itemIndex];
  if (!item || item.type !== 'lesson') notFound();

  const routes = createLearnerRoutes();
  const previous = [...items.slice(0, itemIndex)].reverse().find((candidate) => candidate.type === 'lesson');
  const next = items.slice(itemIndex + 1).find((candidate) => candidate.type === 'lesson');

  return (
    <article className="mx-auto max-w-4xl space-y-8">
      <header className="border-b pb-6">
        <Button asChild variant="ghost" className="-ml-3 mb-4">
          <Link href={routes.content(slug)}>
            <ArrowLeft className="size-4" />
            Course content
          </Link>
        </Button>
        <div className="flex flex-wrap items-center gap-2">
          <Badge variant="outline">Lesson</Badge>
          {item.duration ? <Badge variant="secondary">{item.duration} min</Badge> : null}
        </div>
        <h1 className="mt-4 text-3xl font-semibold sm:text-4xl">{item.title}</h1>
        {item.description ? (
          <p className="mt-3 text-sm leading-6 text-muted-foreground">{item.description}</p>
        ) : null}
        <div className="mt-5">
          <LessonProgressControls
            contentId={item.id}
            courseId={access.course.id}
            status={item.status}
          />
        </div>
      </header>

      {item.status === 'locked' ? (
        <section className="flex min-h-64 flex-col items-center justify-center border-y text-center">
          <Lock className="size-8 text-muted-foreground" />
          <h2 className="mt-4 font-semibold">This lesson is locked</h2>
          <p className="mt-2 text-sm text-muted-foreground">
            Complete the prerequisite lesson before opening this content.
          </p>
        </section>
      ) : (
        <div className="min-w-0">
          <LearnerLessonRenderer
            courseId={access.course.id}
            enrollmentId={access.course.enrollmentId}
            itemId={item.id}
            format={item.lessonFormat}
            content={item.content}
          />
        </div>
      )}

      <nav aria-label="Lesson navigation" className="flex justify-between gap-4 border-t pt-6">
        {previous ? (
          <Button asChild variant="outline">
            <Link href={routes.lesson(slug, previous.id)}>
              <ArrowLeft className="size-4" />
              Previous
            </Link>
          </Button>
        ) : (
          <span />
        )}
        {next ? (
          <Button asChild>
            <Link href={routes.lesson(slug, next.id)}>
              Next
              <ArrowRight className="size-4" />
            </Link>
          </Button>
        ) : null}
      </nav>
    </article>
  );
}
