'use client';

import { Link } from '@/i18n/navigation';
import type { LearningTask } from '@/lib/learning/queries/tasks';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@game-guild/ui/components/tabs';
import { CalendarClock } from 'lucide-react';

interface TasksViewProps {
  tasks: LearningTask[];
}

function formatDue(dueAt: string | null): string {
  if (!dueAt) return 'No due date';
  const date = new Date(dueAt);
  return Number.isNaN(date.getTime())
    ? 'No due date'
    : `Due ${date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}`;
}

function TaskCard({
  task,
  href,
  badge,
  detail,
}: {
  task: LearningTask;
  href: string | null;
  badge?: string;
  detail?: string;
}) {
  const content = (
    <>
      <div className="flex min-w-0 items-start justify-between gap-3">
        <div className="min-w-0">
          <CardTitle className="truncate text-base">{task.assessmentTitle}</CardTitle>
          <p className="mt-1 text-sm text-muted-foreground">{task.courseTitle}</p>
        </div>
        {badge && <Badge variant="secondary">{badge}</Badge>}
      </div>
      <div className="mt-3 flex items-center justify-between gap-3">
        <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
          <CalendarClock className="size-3.5" />
          {formatDue(task.dueAt)}
        </span>
        {detail && <span className="text-xs font-medium text-muted-foreground">{detail}</span>}
      </div>
    </>
  );

  if (!href) {
    return (
      <Card>
        <CardHeader>{content}</CardHeader>
      </Card>
    );
  }

  return (
    <Card className="transition-colors hover:border-primary/40">
      <CardContent className="pt-6">
        <Link href={href} className="block">
          {content}
        </Link>
      </CardContent>
    </Card>
  );
}

function EmptyState({ message }: { message: string }) {
  return (
    <p className="rounded-md border border-dashed p-8 text-center text-sm text-muted-foreground">
      {message}
    </p>
  );
}

export function TasksView({ tasks }: TasksViewProps) {
  const gradeTasks = tasks.filter((task) => task.type === 'grade');
  const reviewTasks = tasks.filter((task) => task.type === 'review');
  const doTasks = tasks.filter((task) => task.type === 'do');
  const hasGrade = gradeTasks.length > 0;

  return (
    <Tabs defaultValue={hasGrade ? 'grade' : 'todo'}>
      <TabsList>
        {hasGrade && <TabsTrigger value="grade">To grade</TabsTrigger>}
        <TabsTrigger value="review">To review</TabsTrigger>
        <TabsTrigger value="todo">To do</TabsTrigger>
      </TabsList>

      {hasGrade && (
        <TabsContent value="grade" className="mt-4 grid gap-3 md:grid-cols-2">
          {gradeTasks.map((task) => (
            <TaskCard
              key={`${task.courseId}:${task.assessmentId}`}
              task={task}
              href={
                task.courseSlug
                  ? `/dashboard/learning/courses/${task.courseSlug}/assessments/${task.assessmentId}/submissions`
                  : null
              }
              badge={`${task.countSubmitted ?? 0}`}
              detail={`${task.countSubmitted ?? 0} submissions awaiting grading`}
            />
          ))}
        </TabsContent>
      )}

      <TabsContent value="review" className="mt-4 grid gap-3 md:grid-cols-2">
        {reviewTasks.length === 0 ? (
          <div className="md:col-span-2">
            <EmptyState message="No peer reviews to complete right now." />
          </div>
        ) : (
          reviewTasks.map((task) => (
            <TaskCard
              key={`${task.courseId}:${task.assessmentId}`}
              task={task}
              href="/learn/reviews"
              detail={`${task.reviewsCompleted ?? 0} / ${task.reviewsRequired ?? 0} reviews completed`}
            />
          ))
        )}
      </TabsContent>

      <TabsContent value="todo" className="mt-4 grid gap-3 md:grid-cols-2">
        {doTasks.length === 0 ? (
          <div className="md:col-span-2">
            <EmptyState message="Nothing to do — you are all caught up." />
          </div>
        ) : (
          doTasks.map((task) => (
            <TaskCard
              key={`${task.courseId}:${task.assessmentId}`}
              task={task}
              href={task.courseSlug ? `/learn/courses/${task.courseSlug}` : '/learn/courses'}
            />
          ))
        )}
      </TabsContent>
    </Tabs>
  );
}
