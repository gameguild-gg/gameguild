"use client";

import { addComment, deleteComment } from "@/lib/blogs/actions";
import { getBlogPostComments } from "@/lib/blogs/queries";
import type { BlogComment } from "@/lib/blogs/types";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@game-guild/ui/components/alert-dialog";
import { Button } from "@game-guild/ui/components/button";
import { Textarea } from "@game-guild/ui/components/textarea";
import { Loader2, Send } from "lucide-react";
import * as React from "react";
import { toast } from "sonner";

const COMMENT_MAX_LENGTH = 2000;

export interface BlogCommentsProps {
  postId: string;
  allowComments: boolean;
  currentUserId: string | null;
  isPostAuthor: boolean;
  coAuthorIds: string[];
  /**
   * Viewer handle when known. The public comment DTO carries only
   * `authorHandle`, so comment-author delete visibility matches on handle.
   */
  currentUserHandle?: string | null;
}

interface CommentNode extends BlogComment {
  replies: CommentNode[];
}

function asNode(comment: BlogComment): CommentNode {
  return { ...comment, replies: [] };
}

/** Root comments keep insertion order; each reply attaches under its parent. */
function attach(comments: CommentNode[], incoming: BlogComment[]): CommentNode[] {
  const next = comments.map((comment) => ({ ...comment, replies: [...comment.replies] }));
  for (const item of incoming) {
    const node = asNode(item);
    const parentId = node.parentCommentId ?? null;
    if (parentId) {
      const parent = next.find((candidate) => candidate.id === parentId);
      if (parent) {
        parent.replies.push(node);
        continue;
      }
    }
    if (!next.some((candidate) => candidate.id === node.id)) next.push(node);
  }
  return next;
}

function detach(comments: CommentNode[], commentId: string): CommentNode[] {
  return comments
    .filter((comment) => comment.id !== commentId)
    .map((comment) => ({ ...comment, replies: detach(comment.replies, commentId) }));
}

function canDelete(
  comment: BlogComment,
  props: Pick<BlogCommentsProps, "currentUserId" | "currentUserHandle" | "isPostAuthor" | "coAuthorIds">,
) {
  if (props.isPostAuthor) return true;
  const userId = props.currentUserId;
  if (!userId) return false;
  if (props.coAuthorIds.includes(userId)) return true;
  const handle = comment.authorHandle ?? null;
  return handle != null && handle.length > 0 && handle === (props.currentUserHandle ?? null);
}

function CommentBody({
  comment,
  canDeleteComment,
  onReply,
  onDelete,
  isReply,
}: {
  comment: CommentNode;
  canDeleteComment: (comment: CommentNode) => boolean;
  onReply?: (comment: CommentNode) => void;
  onDelete: (comment: CommentNode) => void;
  isReply?: boolean;
}) {
  const deletable = canDeleteComment(comment);
  const [deletePending, setDeletePending] = React.useState(false);

  async function handleDelete() {
    setDeletePending(true);
    try {
      onDelete(comment);
    } finally {
      setDeletePending(false);
    }
  }

  return (
    <div className="flex gap-3" data-comment-id={comment.id}>
      <span
        aria-hidden="true"
        className="flex size-9 shrink-0 items-center justify-center overflow-hidden rounded-full bg-muted text-xs font-bold text-foreground"
      >
        {(comment.authorDisplayName ?? comment.authorHandle ?? "?").slice(0, 2).toUpperCase()}
      </span>
      <div className="min-w-0 flex-1">
        <div className="rounded-xl bg-accent/45 px-3 py-2.5">
          <p className="text-xs font-semibold text-foreground">{comment.authorDisplayName ?? comment.authorHandle}</p>
          <p className="mt-0.5 whitespace-pre-wrap text-sm leading-5 text-foreground">{comment.content}</p>
        </div>
        <div className="mt-1 flex items-center gap-1">
          {onReply ? (
            <button
              type="button"
              aria-label={`Reply to ${comment.authorDisplayName ?? comment.authorHandle ?? "comment"}`}
              onClick={() => onReply(comment)}
              className="px-2 text-xs text-muted-foreground hover:text-foreground"
            >
              Reply
            </button>
          ) : null}
          {deletable ? (
            <AlertDialog>
              <AlertDialogTrigger
                render={
                  <button
                    type="button"
                    aria-label={`Delete comment by ${comment.authorDisplayName ?? comment.authorHandle ?? "author"}`}
                    className="px-2 text-xs text-muted-foreground hover:text-destructive"
                  />
                }
              >
                Delete
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>Delete comment?</AlertDialogTitle>
                  <AlertDialogDescription>This permanently removes the comment{isReply ? " and any further replies" : " and its replies"}.</AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel disabled={deletePending}>Cancel</AlertDialogCancel>
                  <AlertDialogAction
                    disabled={deletePending}
                    aria-label="Confirm delete comment"
                    onClick={(event) => {
                      event.preventDefault();
                      void handleDelete();
                    }}
                  >
                    {deletePending ? "Deleting…" : "Delete"}
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          ) : null}
        </div>
        {comment.replies.length > 0 ? (
          <div className="mt-3 space-y-3 border-l border-border/50 pl-3">
            {comment.replies.map((reply) => (
              <CommentBody
                key={reply.id}
                comment={reply}
                isReply
                canDeleteComment={canDeleteComment}
                onReply={undefined}
                onDelete={onDelete}
              />
            ))}
          </div>
        ) : null}
      </div>
    </div>
  );
}

export function BlogComments(props: BlogCommentsProps): React.JSX.Element {
  const { postId, allowComments, currentUserId } = props;
  const signedIn = currentUserId !== null;

  const [threads, setThreads] = React.useState<CommentNode[]>([]);
  const [hasMore, setHasMore] = React.useState(false);
  const [cursor, setCursor] = React.useState<{ afterCreatedAt?: string; afterId?: string } | null>(null);
  const [loading, setLoading] = React.useState(true);
  const [pagePending, setPagePending] = React.useState(false);
  const [commentText, setCommentText] = React.useState("");
  const [replyingTo, setReplyingTo] = React.useState<CommentNode | null>(null);
  const [submitPending, setSubmitPending] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const pagePendingRef = React.useRef(false);
  const submitPendingRef = React.useRef(false);

  React.useEffect(() => {
    let active = true;
    void getBlogPostComments(postId)
      .then((page) => {
        if (!active || !page) return;
        setThreads(attach([], page.items));
        setHasMore(page.hasMore);
        const last = page.items.at(-1);
        setCursor(last ? { afterCreatedAt: last.createdAt ?? undefined, afterId: last.id ?? undefined } : null);
      })
      .catch((reason) => {
        if (!active) return;
        const message = reason instanceof Error ? reason.message : "Comments could not be loaded.";
        setError(message);
        toast.error(message);
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [postId]);

  async function loadMore() {
    if (cursor === null || pagePendingRef.current) return;
    pagePendingRef.current = true;
    setPagePending(true);
    setError(null);
    try {
      const page = await getBlogPostComments(postId, cursor);
      if (page) {
        setThreads((current) => attach(current, page.items));
        setHasMore(page.hasMore);
        const last = page.items.at(-1);
        setCursor(last ? { afterCreatedAt: last.createdAt ?? undefined, afterId: last.id ?? undefined } : null);
      }
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : "More comments could not be loaded.";
      setError(message);
      toast.error(message);
    } finally {
      pagePendingRef.current = false;
      setPagePending(false);
    }
  }

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const value = commentText.trim();
    if (!value || submitPendingRef.current) return;
    submitPendingRef.current = true;
    setSubmitPending(true);
    setError(null);
    try {
      const result = await addComment(postId, { content: value, ...(replyingTo ? { parentCommentId: replyingTo.id } : {}) });
      if (result.success) {
        setThreads((current) => attach(current, [result.data]));
        setCommentText("");
        setReplyingTo(null);
      } else {
        const message = result.error;
        setError(message);
        toast.error(message);
      }
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : "Comment could not be published.";
      setError(message);
      toast.error(message);
    } finally {
      submitPendingRef.current = false;
      setSubmitPending(false);
    }
  }

  async function remove(comment: CommentNode) {
    const commentId = comment.id;
    if (!commentId) return;
    setThreads((current) => detach(current, commentId));
    const reconcile = async () => {
      const page = await getBlogPostComments(postId).catch(() => null);
      if (page) setThreads(attach([], page.items));
    };
    try {
      const result = await deleteComment(commentId);
      if (!result.success) {
        await reconcile();
        setError(result.error);
        toast.error(result.error);
      }
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : "Comment could not be deleted.";
      await reconcile();
      setError(message);
      toast.error(message);
    }
  }

  const rootComments = threads;

  return (
    <section aria-label="Comments" className="mt-8 border-t pt-6">
      <h3 className="text-sm font-semibold text-foreground">Comments</h3>

      {allowComments ? null : (
        <p role="note" data-testid="comments-disabled-notice" className="mt-3 text-sm text-muted-foreground">
          Comments are disabled for this post.
        </p>
      )}

      <div className="mt-4">
        {loading ? (
          <p className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="size-4 animate-spin" aria-hidden="true" /> Loading comments…
          </p>
        ) : rootComments.length > 0 ? (
          <div className="space-y-4">
            {rootComments.map((comment) => (
              <CommentBody
                key={comment.id}
                comment={comment}
                canDeleteComment={(target) => canDelete(target, props)}
                onReply={allowComments && signedIn ? () => setReplyingTo(comment) : undefined}
                onDelete={(target) => void remove(target)}
              />
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">No comments yet.</p>
        )}
        {hasMore && cursor !== null ? (
          <Button type="button" variant="ghost" size="sm" disabled={pagePending} onClick={() => void loadMore()} className="mt-4 w-full">
            {pagePending ? "Loading…" : "Load more comments"}
          </Button>
        ) : null}
        {error ? (
          <p role="alert" className="mt-2 text-sm text-destructive">
            {error}
          </p>
        ) : null}
      </div>

      {allowComments && signedIn ? (
        <form onSubmit={submit} className="mt-6 flex items-end gap-2 border-t pt-4">
          <div className="min-w-0 flex-1">
            {replyingTo ? (
              <p className="mb-1 text-xs text-muted-foreground">
                Replying to {replyingTo.authorDisplayName ?? replyingTo.authorHandle}
                <button type="button" onClick={() => setReplyingTo(null)} className="ml-1 text-primary">
                  Cancel
                </button>
              </p>
            ) : null}
            <Textarea
              aria-label={replyingTo ? "Write a reply" : "Write a comment"}
              value={commentText}
              onChange={(event) => setCommentText(event.target.value)}
              rows={2}
              maxLength={COMMENT_MAX_LENGTH}
              placeholder={replyingTo ? "Add a reply…" : "Add a comment…"}
              className="min-h-10 resize-none"
            />
          </div>
          <Button type="submit" size="icon" disabled={submitPending || !commentText.trim()} aria-label={replyingTo ? "Publish reply" : "Publish comment"}>
            {submitPending ? <Loader2 className="size-4 animate-spin" aria-hidden="true" /> : <Send className="size-4" aria-hidden="true" />}
          </Button>
        </form>
      ) : null}
    </section>
  );
}
