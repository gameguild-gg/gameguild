'use client';

import {
  acceptDiscussionReply,
  createDiscussionReply,
  resolveDiscussion,
  updateDiscussionPin,
  upvoteDiscussionReply,
} from '@/lib/learning/actions';
import type { DiscussionReply } from '@/lib/learning/queries/support';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { CheckCircle2, Loader2, Pin, Reply, ThumbsUp } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';

interface ThreadActionPanelProps {
  courseId: string;
  threadId: string;
  pinned: boolean;
  resolved: boolean;
  replies: DiscussionReply[];
}

export function ThreadActionPanel({ courseId, threadId, pinned, resolved, replies }: ThreadActionPanelProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [isPinned, setIsPinned] = useState(pinned);
  const [isResolved, setIsResolved] = useState(resolved);
  const [replyText, setReplyText] = useState('');
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const runAction = (action: () => Promise<{ success: true; data: unknown } | { success: false; error: string }>, success: string) => {
    setMessage(null);
    startTransition(async () => {
      const result = await action();
      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      setMessage({ type: 'success', text: success });
      router.refresh();
    });
  };

  const submitReply = () => {
    const content = replyText.trim();
    runAction(
      async () => {
        const result = await createDiscussionReply({ courseId, discussionId: threadId, content });
        if (result.success) setReplyText('');
        return result;
      },
      'Reply posted.',
    );
  };

  const togglePinned = () => {
    const nextPinned = !isPinned;
    runAction(
      async () => {
        const result = await updateDiscussionPin(courseId, threadId, nextPinned);
        if (result.success) setIsPinned(nextPinned);
        return result;
      },
      nextPinned ? 'Discussion pinned.' : 'Discussion unpinned.',
    );
  };

  const markResolved = () => {
    runAction(
      async () => {
        const result = await resolveDiscussion(courseId, threadId);
        if (result.success) setIsResolved(true);
        return result;
      },
      'Discussion marked resolved.',
    );
  };

  const acceptReply = (replyId: string) => {
    runAction(() => acceptDiscussionReply(courseId, threadId, replyId), 'Answer accepted.');
  };

  const upvoteReply = (replyId: string) => {
    runAction(() => upvoteDiscussionReply(courseId, threadId, replyId), 'Reply upvoted.');
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">Instructor Actions</CardTitle>
        <CardDescription>Moderate the thread and respond without leaving the course dashboard.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-5">
        <div className="flex flex-wrap gap-2">
          <Badge variant={isPinned ? 'default' : 'outline'}>{isPinned ? 'Pinned' : 'Not pinned'}</Badge>
          <Badge variant={isResolved ? 'secondary' : 'outline'}>{isResolved ? 'Resolved' : 'Open'}</Badge>
        </div>

        <div className="flex flex-wrap gap-2">
          <Button type="button" size="sm" variant="outline" disabled={isPending} onClick={togglePinned}>
            <Pin className="mr-2 size-4" />
            {isPinned ? 'Unpin' : 'Pin'}
          </Button>
          <Button type="button" size="sm" variant="outline" disabled={isPending || isResolved} onClick={markResolved}>
            <CheckCircle2 className="mr-2 size-4" />
            Resolve
          </Button>
        </div>

        <div className="space-y-2">
          <Label htmlFor="discussion-reply">Reply</Label>
          <Textarea
            id="discussion-reply"
            rows={5}
            value={replyText}
            onChange={(event) => setReplyText(event.target.value)}
            disabled={isPending}
            placeholder="Answer the question, ask for more context, or link the right course content."
          />
          <Button type="button" className="w-full" disabled={isPending} onClick={submitReply}>
            {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Reply className="mr-2 size-4" />}
            Post reply
          </Button>
        </div>

        {replies.length > 0 ? (
          <div className="space-y-2 border-t pt-4">
            <p className="text-sm font-medium">Reply moderation</p>
            {replies.map((reply) => (
              <div key={reply.id} className="flex items-center justify-between gap-3 rounded-lg border p-3 text-sm">
                <div className="min-w-0">
                  <p className="truncate font-medium">{reply.authorName}</p>
                  <p className="text-muted-foreground">{reply.upvotes} upvotes{reply.isAnswer ? ' · accepted' : ''}</p>
                </div>
                <div className="flex gap-2">
                  <Button type="button" size="sm" variant="outline" disabled={isPending || reply.isAnswer} onClick={() => acceptReply(reply.id)}>
                    <CheckCircle2 className="mr-2 size-4" />
                    Accept
                  </Button>
                  <Button type="button" size="sm" variant="outline" disabled={isPending} onClick={() => upvoteReply(reply.id)}>
                    <ThumbsUp className="mr-2 size-4" />
                    Upvote
                  </Button>
                </div>
              </div>
            ))}
          </div>
        ) : null}

        {message ? (
          <p role={message.type === 'success' ? 'status' : 'alert'} className={message.type === 'success' ? 'text-sm text-emerald-600' : 'text-sm text-destructive'}>
            {message.text}
          </p>
        ) : null}
      </CardContent>
    </Card>
  );
}
