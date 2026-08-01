'use client';

import { createCourseDiscussion } from '@/lib/learner/activity-actions';
import type { LearningExperienceSocialServicesCourseDiscussion } from '@game-guild/client';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@game-guild/ui/components/dialog';
import { Input } from '@game-guild/ui/components/input';
import { Textarea } from '@game-guild/ui/components/textarea';
import { CheckCircle2, MessageCircle, MessagesSquare, Plus } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { type FormEvent, useState } from 'react';

interface CourseCommunityProps {
  courseId: string;
  courseSlug: string;
  courseTitle: string;
  discussions: LearningExperienceSocialServicesCourseDiscussion[];
}

export function CourseCommunity({
  courseId,
  courseSlug,
  courseTitle,
  discussions,
}: CourseCommunityProps) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setError(null);
    const result = await createCourseDiscussion(new FormData(event.currentTarget));
    setPending(false);

    if (!result.success) {
      setError(result.error || 'The discussion could not be published.');
      return;
    }

    setSuccess(true);
    router.refresh();
  }

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 border-b pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-sm font-medium text-primary">{courseTitle}</p>
          <h1 className="mt-2 text-3xl font-semibold">Course community</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Ask questions, coordinate peer learning, and continue cohort conversations.
          </p>
        </div>

        <Dialog
          open={open}
          onOpenChange={(value) => {
            setOpen(value);
            if (!value) {
              setError(null);
              setSuccess(false);
            }
          }}
        >
          <DialogTrigger asChild>
            <Button>
              <Plus className="size-4" />
              Start discussion
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Start a course discussion</DialogTitle>
              <DialogDescription>
                Everyone enrolled in this course can read and reply.
              </DialogDescription>
            </DialogHeader>

            {success ? (
              <Alert>
                <CheckCircle2 className="size-4 text-emerald-600" />
                <AlertTitle>Discussion published</AlertTitle>
                <AlertDescription>
                  Your thread is now available to the course community.
                </AlertDescription>
              </Alert>
            ) : (
              <form onSubmit={handleSubmit} className="space-y-4">
                <input type="hidden" name="courseId" value={courseId} />
                <input type="hidden" name="courseSlug" value={courseSlug} />
                <div className="space-y-2">
                  <label htmlFor="discussion-title" className="text-sm font-medium">
                    Title
                  </label>
                  <Input id="discussion-title" name="title" required maxLength={160} />
                </div>
                <div className="space-y-2">
                  <label htmlFor="discussion-message" className="text-sm font-medium">
                    Message
                  </label>
                  <Textarea id="discussion-message" name="content" required rows={7} />
                </div>
                {error ? (
                  <Alert variant="destructive">
                    <AlertTitle>Could not publish</AlertTitle>
                    <AlertDescription>{error}</AlertDescription>
                  </Alert>
                ) : null}
                <div className="flex justify-end">
                  <Button type="submit" disabled={pending}>
                    {pending ? 'Publishing...' : 'Publish discussion'}
                  </Button>
                </div>
              </form>
            )}
          </DialogContent>
        </Dialog>
      </header>

      {discussions.length === 0 ? (
        <Card>
          <CardContent className="flex min-h-56 flex-col items-center justify-center text-center">
            <MessagesSquare className="size-9 text-muted-foreground" />
            <h2 className="mt-4 font-semibold">No discussions yet</h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Start the first conversation for this course.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="divide-y border-y">
          {discussions.map((discussion) => (
            <article key={discussion.id} className="px-3 py-5">
              <div className="flex flex-wrap items-center gap-2">
                <h2 className="font-semibold">{discussion.title || 'Course discussion'}</h2>
                {discussion.isPinned ? <Badge variant="secondary">Pinned</Badge> : null}
                {discussion.isResolved ? <Badge variant="outline">Resolved</Badge> : null}
              </div>
              <p className="mt-2 line-clamp-3 text-sm leading-6 text-muted-foreground">
                {discussion.content}
              </p>
              <p className="mt-4 inline-flex items-center gap-2 text-xs text-muted-foreground">
                <MessageCircle className="size-3.5" />
                {discussion.replyCount ?? 0} replies
              </p>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}
