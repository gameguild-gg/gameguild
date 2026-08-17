import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { TriangleAlert } from 'lucide-react';
import React from 'react';
import { getMyTasks } from '@/lib/learning/queries/tasks';
import { TasksView } from './tasks-view';

/**
 * Cross-course task list for instructors and students.
 *
 * Route: `/dashboard/tasks`
 *
 * Server Component — fetches /me/tasks via the learning query lib (slug
 * resolution for course links included) and renders the tabbed client view.
 * All counts come from the API; nothing is aggregated client-side.
 */
export default async function TasksPage(): Promise<React.JSX.Element> {
  const result = await getMyTasks();

  if (!result.ok) {
    return (
      <div className="flex flex-col gap-6 p-6">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Tasks</h1>
          <p className="text-muted-foreground">Everything you need to grade, review, and do.</p>
        </div>
        <Card className="border-destructive/50">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-destructive">
              <TriangleAlert className="size-5" />
              Unable to load tasks
            </CardTitle>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">{result.error}</CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Tasks</h1>
        <p className="text-muted-foreground">Everything you need to grade, review, and do.</p>
      </div>
      <TasksView tasks={result.tasks} />
    </div>
  );
}
