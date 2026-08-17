import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getDiscussionThread } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CheckCircle2, MessageSquare } from 'lucide-react';
import { ThreadActionPanel } from '@/components/learning/console/courses/[course]/support/thread-action-panel';

/**
 * Discussion Thread Detail Page
 *
 * Route: /courses/[course]/support/discussions/[threadId]
 * Condition: course.features.hasDiscussions = true
 */
export default async function DiscussionThreadPage({
  params,
}: PageProps<'/[locale]/console/learning/courses/[course]/support/discussions/[threadId]'>): Promise<React.JSX.Element> {
  const { course: courseId, threadId } = await params;

  const course = await getCourse(courseId);
  if (!course) {
    notFound();
  }

  const thread = await getDiscussionThread(threadId);

  if (!thread) {
    notFound();
  }

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <Card className="lg:col-span-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2"><MessageSquare className="size-5" />{thread.title}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-2">
            <Badge variant="outline">{course.title}</Badge>
            {thread.locked && <Badge variant="secondary"><CheckCircle2 className="mr-1 size-3" />Resolved</Badge>}
          </div>
          <div className="rounded-lg border p-4">
            <p className="mb-2 text-sm text-muted-foreground">{thread.authorName} · {new Date(thread.createdAt).toLocaleString('en-US')}</p>
            <p className="whitespace-pre-wrap text-sm">{thread.content}</p>
          </div>
          <div className="space-y-3">
            {thread.replies.map((reply) => (
              <div key={reply.id} className="rounded-lg border p-4">
                <div className="mb-2 flex items-center justify-between text-sm">
                  <span className="font-medium">{reply.authorName}</span>
                  {reply.isAnswer && <Badge variant="default">Accepted answer</Badge>}
                </div>
                <p className="whitespace-pre-wrap text-sm text-muted-foreground">{reply.content}</p>
              </div>
            ))}
            {thread.replies.length === 0 && <div className="rounded-lg border border-dashed p-6 text-center text-sm text-muted-foreground">No replies yet.</div>}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle className="text-lg">Thread Activity</CardTitle></CardHeader>
        <CardContent className="space-y-3 text-sm">
          <div className="flex justify-between"><span className="text-muted-foreground">Replies</span><span>{thread.replyCount}</span></div>
          <div className="flex justify-between"><span className="text-muted-foreground">Views</span><span>{thread.viewCount}</span></div>
          <div className="flex justify-between"><span className="text-muted-foreground">Pinned</span><span>{thread.pinned ? 'Yes' : 'No'}</span></div>
        </CardContent>
      </Card>
      <ThreadActionPanel
        courseId={courseId}
        threadId={thread.id}
        pinned={thread.pinned}
        resolved={thread.locked}
        replies={thread.replies}
      />
    </div>
  );
}
