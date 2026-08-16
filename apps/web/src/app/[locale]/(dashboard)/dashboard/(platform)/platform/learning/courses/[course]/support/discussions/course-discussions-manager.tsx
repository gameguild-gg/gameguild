'use client';

import {
  createCourseDiscussion,
  deleteDiscussion,
  resolveDiscussion,
  updateDiscussionPin,
} from '@/lib/learning/actions';
import type { DiscussionThread } from '@/lib/learning/queries/support';
import { Link } from '@/i18n/navigation';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { CheckCircle2, Loader2, MessageSquare, Pin, Plus, Reply, Trash2 } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';

interface CourseDiscussionsManagerProps {
  courseId: string;
  courseTitle: string;
  threads: DiscussionThread[];
}

export function CourseDiscussionsManager({ courseId, courseTitle, threads }: CourseDiscussionsManagerProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [items, setItems] = useState(threads);
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const submitDiscussion = () => {
    setMessage(null);
    const submittedTitle = title.trim();
    const submittedContent = content.trim();

    startTransition(async () => {
      const result = await createCourseDiscussion({
        courseId,
        title: submittedTitle,
        content: submittedContent,
      });

      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      const now = new Date().toISOString();
      setItems((current) => [
        {
          id: result.data.id,
          courseId,
          authorId: 'current-user',
          authorName: 'You',
          title: submittedTitle,
          content: submittedContent,
          pinned: false,
          locked: false,
          replyCount: 0,
          viewCount: 0,
          lastReplyAt: null,
          tags: [],
          createdAt: now,
          updatedAt: now,
        },
        ...current,
      ]);
      setTitle('');
      setContent('');
      setMessage({ type: 'success', text: 'Discussion created.' });
      router.refresh();
    });
  };

  const togglePin = (thread: DiscussionThread) => {
    setMessage(null);
    startTransition(async () => {
      const result = await updateDiscussionPin(courseId, thread.id, !thread.pinned);
      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      setItems((current) => current.map((item) => item.id === thread.id ? { ...item, pinned: !thread.pinned } : item));
      setMessage({ type: 'success', text: thread.pinned ? 'Discussion unpinned.' : 'Discussion pinned.' });
      router.refresh();
    });
  };

  const markResolved = (thread: DiscussionThread) => {
    setMessage(null);
    startTransition(async () => {
      const result = await resolveDiscussion(courseId, thread.id);
      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      setItems((current) => current.map((item) => item.id === thread.id ? { ...item, locked: true } : item));
      setMessage({ type: 'success', text: 'Discussion marked resolved.' });
      router.refresh();
    });
  };

  const removeDiscussion = (thread: DiscussionThread) => {
    setMessage(null);
    startTransition(async () => {
      const result = await deleteDiscussion(courseId, thread.id);
      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      setItems((current) => current.filter((item) => item.id !== thread.id));
      setMessage({ type: 'success', text: 'Discussion deleted.' });
      router.refresh();
    });
  };

  return (
    <div className="grid min-w-0 max-w-full gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(280px,360px)]">
      <Card className="min-w-0">
        <CardHeader className="min-w-0">
          <CardTitle className="flex items-center gap-2">
            <MessageSquare className="size-5" />
            Course Discussions
          </CardTitle>
          <CardDescription className="break-words">{courseTitle} learner questions and discussion threads.</CardDescription>
        </CardHeader>
        <CardContent className="min-w-0 space-y-3">
          {items.length === 0 ? (
            <div className="min-w-0 rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No discussions have been started for this course.</div>
          ) : (
            items.map((thread) => (
              <div key={thread.id} className="min-w-0 space-y-3 rounded-lg border p-4">
                <div className="flex min-w-0 flex-col gap-3 md:flex-row md:items-start md:justify-between">
                  <Link href={`/dashboard/platform/learning/courses/${courseId}/support/discussions/${thread.id}`} className="min-w-0 flex-1 space-y-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="break-words font-medium">{thread.title}</p>
                      {thread.pinned ? <Badge variant="outline"><Pin className="mr-1 size-3" />Pinned</Badge> : null}
                      {thread.locked ? <Badge variant="secondary"><CheckCircle2 className="mr-1 size-3" />Resolved</Badge> : null}
                    </div>
                    <p className="text-sm text-muted-foreground">{thread.authorName} · {new Date(thread.createdAt).toLocaleDateString('en-US')}</p>
                  </Link>
                  <span className="flex items-center gap-1 text-sm text-muted-foreground">
                    <Reply className="size-4" />
                    {thread.replyCount}
                  </span>
                </div>
                <div className="flex flex-wrap gap-2">
                  <Button type="button" size="sm" variant="outline" disabled={isPending} onClick={() => togglePin(thread)}>
                    <Pin className="mr-2 size-4" />
                    {thread.pinned ? 'Unpin' : 'Pin'}
                  </Button>
                  <Button type="button" size="sm" variant="outline" disabled={isPending || thread.locked} onClick={() => markResolved(thread)}>
                    <CheckCircle2 className="mr-2 size-4" />
                    Resolve
                  </Button>
                  <Button type="button" size="sm" variant="outline" disabled={isPending} onClick={() => removeDiscussion(thread)} aria-label={`Delete ${thread.title}`}>
                    <Trash2 className="mr-2 size-4" />
                    Delete
                  </Button>
                </div>
              </div>
            ))
          )}
        </CardContent>
      </Card>

      <Card className="min-w-0">
        <CardHeader className="min-w-0">
          <CardTitle className="flex items-center gap-2 text-lg">
            <Plus className="size-4" />
            Start discussion
          </CardTitle>
          <CardDescription className="break-words">Open a learner-facing thread for questions, blockers, or feedback.</CardDescription>
        </CardHeader>
        <CardContent className="min-w-0 space-y-4">
          <div className="space-y-2">
            <Label htmlFor="discussion-title">Title</Label>
            <Input id="discussion-title" value={title} onChange={(event) => setTitle(event.target.value)} disabled={isPending} placeholder="Question about milestone review" />
          </div>
          <div className="space-y-2">
            <Label htmlFor="discussion-content">Content</Label>
            <Textarea id="discussion-content" value={content} onChange={(event) => setContent(event.target.value)} disabled={isPending} rows={5} placeholder="Add the context, blockers, and expected outcome." />
          </div>
          {message ? (
            <p role={message.type === 'success' ? 'status' : 'alert'} className={message.type === 'success' ? 'text-sm text-emerald-600' : 'text-sm text-destructive'}>
              {message.text}
            </p>
          ) : null}
          <Button type="button" className="w-full" disabled={isPending} onClick={submitDiscussion}>
            {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Plus className="mr-2 size-4" />}
            Create discussion
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
