import React from 'react';
import { getCourse, getCourseDiscussions } from '@/lib/learning';
import { Link } from '@/i18n/navigation';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { MessageSquare, Pin, Reply } from 'lucide-react';

/**
 * Discussions Page
 *
 * Route: /courses/[course]/support/discussions
 * Condition: course.features.hasDiscussions = true
 */
export default async function DiscussionsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/support/discussions'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  if (!course) return <div className="text-muted-foreground p-6">Course not found.</div>;

  const discussions = await getCourseDiscussions(courseId);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2"><MessageSquare className="size-5" />Course Discussions</CardTitle>
        <CardDescription>{course.title} learner questions and discussion threads.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {discussions.threads.length === 0 ? (
          <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No discussions have been started for this course.</div>
        ) : (
          discussions.threads.map((thread) => (
            <Link key={thread.id} href={`/dashboard/learning/courses/${courseId}/support/discussions/${thread.id}`} className="flex items-center justify-between rounded-lg border p-4 transition-colors hover:bg-muted/50">
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <p className="font-medium">{thread.title}</p>
                  {thread.pinned && <Badge variant="outline"><Pin className="mr-1 size-3" />Pinned</Badge>}
                  {thread.locked && <Badge variant="secondary">Resolved</Badge>}
                </div>
                <p className="text-sm text-muted-foreground">{thread.authorName} · {new Date(thread.createdAt).toLocaleDateString('en-US')}</p>
              </div>
              <span className="flex items-center gap-1 text-sm text-muted-foreground"><Reply className="size-4" />{thread.replyCount}</span>
            </Link>
          ))
        )}
      </CardContent>
    </Card>
  );
}
