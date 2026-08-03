"use client";

import { createCourseDiscussionReply } from "@/lib/learner/activity-actions";
import type {
  LearningExperienceSocialServicesCourseDiscussion,
  LearningExperienceSocialServicesDiscussionReply,
} from "@game-guild/client";
import {
  Alert,
  AlertDescription,
  AlertTitle,
} from "@game-guild/ui/components/alert";
import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
import { Textarea } from "@game-guild/ui/components/textarea";
import {
  ArrowLeft,
  CheckCircle2,
  MessageCircle,
  Send,
  ThumbsUp,
} from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { type FormEvent, useState } from "react";

interface CourseDiscussionThreadProps {
  courseSlug: string;
  courseTitle: string;
  discussion: LearningExperienceSocialServicesCourseDiscussion;
  replies: LearningExperienceSocialServicesDiscussionReply[];
}

function formatDate(value?: string) {
  if (!value) return null;
  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function CourseDiscussionThread({
  courseSlug,
  courseTitle,
  discussion,
  replies,
}: CourseDiscussionThreadProps) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  async function handleReply(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    setPending(true);
    setError(null);
    setSuccess(false);
    const result = await createCourseDiscussionReply(new FormData(form));
    setPending(false);

    if (!result.success) {
      setError(result.error || "The reply could not be published.");
      return;
    }

    form.reset();
    setSuccess(true);
    router.refresh();
  }

  return (
    <div className="mx-auto w-full max-w-4xl space-y-8">
      <header className="space-y-5 border-b pb-6">
        <Button asChild size="sm" variant="ghost">
          <Link href={`/courses/${courseSlug}/community`}>
            <ArrowLeft className="size-4" />
            Back to community
          </Link>
        </Button>
        <div>
          <p className="text-sm font-medium text-primary">{courseTitle}</p>
          <div className="mt-2 flex flex-wrap items-center gap-2">
            <h1 className="text-3xl font-semibold">
              {discussion.title || "Course discussion"}
            </h1>
            {discussion.isPinned ? (
              <Badge variant="secondary">Pinned</Badge>
            ) : null}
            {discussion.isResolved ? (
              <Badge variant="outline">Resolved</Badge>
            ) : null}
          </div>
          <p className="mt-3 text-sm text-muted-foreground">
            {formatDate(discussion.createdAt) || "Course conversation"}
            {" / "}
            {discussion.viewCount ?? 0} views
          </p>
        </div>
      </header>

      <article
        aria-label="Discussion message"
        className="border-l-2 border-primary pl-5"
      >
        <p className="whitespace-pre-wrap text-base leading-7">
          {discussion.content || "No discussion message was provided."}
        </p>
      </article>

      <section aria-labelledby="discussion-replies" className="space-y-4">
        <div className="flex items-center justify-between gap-3">
          <h2 id="discussion-replies" className="text-xl font-semibold">
            Replies
          </h2>
          <Badge variant="outline">
            <MessageCircle className="size-3" />
            {replies.length}
          </Badge>
        </div>

        {replies.length === 0 ? (
          <div className="flex min-h-32 items-center justify-center border-y text-center text-sm text-muted-foreground">
            No replies yet. Continue the conversation below.
          </div>
        ) : (
          <div className="divide-y border-y">
            {replies.map((reply) => (
              <article
                key={reply.id}
                className={
                  reply.parentReplyId ? "ml-8 py-5 pl-4 border-l" : "py-5"
                }
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-sm font-medium">
                      Community member
                    </span>
                    {reply.isAcceptedAnswer ? (
                      <Badge variant="secondary">
                        <CheckCircle2 className="size-3" />
                        Accepted answer
                      </Badge>
                    ) : null}
                  </div>
                  <time className="text-xs text-muted-foreground">
                    {formatDate(reply.createdAt)}
                  </time>
                </div>
                <p className="mt-3 whitespace-pre-wrap text-sm leading-6">
                  {reply.content}
                </p>
                <p className="mt-3 inline-flex items-center gap-1 text-xs text-muted-foreground">
                  <ThumbsUp className="size-3" />
                  {reply.upvoteCount ?? 0} helpful
                </p>
              </article>
            ))}
          </div>
        )}
      </section>

      <section
        aria-labelledby="reply-heading"
        className="rounded-md border p-5"
      >
        <h2 id="reply-heading" className="font-semibold">
          Add a reply
        </h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Keep the response specific to this course conversation.
        </p>
        <form onSubmit={handleReply} className="mt-4 space-y-4">
          <input type="hidden" name="discussionId" value={discussion.id} />
          <input type="hidden" name="courseSlug" value={courseSlug} />
          <Textarea
            aria-label="Reply message"
            name="content"
            rows={6}
            required
            placeholder="Write your reply..."
          />
          {error ? (
            <Alert variant="destructive">
              <AlertTitle>Could not publish reply</AlertTitle>
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          ) : null}
          {success ? (
            <Alert>
              <CheckCircle2 className="size-4 text-emerald-600" />
              <AlertTitle>Reply published</AlertTitle>
              <AlertDescription>
                The conversation has been updated.
              </AlertDescription>
            </Alert>
          ) : null}
          <div className="flex justify-end">
            <Button type="submit" disabled={pending}>
              <Send className="size-4" />
              {pending ? "Publishing..." : "Publish reply"}
            </Button>
          </div>
        </form>
      </section>
    </div>
  );
}
