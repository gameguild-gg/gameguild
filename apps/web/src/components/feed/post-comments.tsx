"use client";

import {
  createPostComment,
  deletePostComment,
  loadPostCommentRepliesPageAction,
  loadPostCommentsPageAction,
  updatePostComment,
} from "@/lib/feed/actions";
import type { PostComment } from "@/lib/feed/contracts";
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
import {
  Drawer,
  DrawerContent,
  DrawerDescription,
  DrawerHeader,
  DrawerTitle,
} from "@game-guild/ui/components/drawer";
import { Textarea } from "@game-guild/ui/components/textarea";
import { Loader2, Send } from "lucide-react";
import Image from "next/image";
import * as React from "react";
import { toast } from "sonner";

const COMMENT_PAGE_SIZE = 10;
const REPLY_PAGE_SIZE = 2;

function useResponsiveMobile() {
  const [mobile, setMobile] = React.useState(() => typeof window !== "undefined" && window.innerWidth < 768);
  React.useEffect(() => {
    if (typeof window.matchMedia !== "function") return;
    const query = window.matchMedia("(max-width: 767px)");
    const update = () => setMobile(query.matches);
    update();
    query.addEventListener("change", update);
    return () => query.removeEventListener("change", update);
  }, []);
  return mobile;
}

function initials(name: string, id: string) {
  const parts = (name.trim() || id.replace(/[-_]+/g, " ") || "GG").split(/\s+/).filter(Boolean);
  return parts.length > 1
    ? `${parts[0]?.[0] ?? ""}${parts.at(-1)?.[0] ?? ""}`.toUpperCase()
    : parts[0]?.slice(0, 2).toUpperCase() || "GG";
}

function CommentAvatar({ comment }: { comment: PostComment }) {
  return (
    <span className="flex size-9 shrink-0 items-center justify-center overflow-hidden rounded-full bg-muted text-xs font-bold text-foreground">
      {comment.authorAvatarUrl ? (
        <Image src={comment.authorAvatarUrl} alt="" width={36} height={36} unoptimized className="size-9 object-cover" />
      ) : initials(comment.authorName, comment.authorId)}
    </span>
  );
}

function findComment(comments: PostComment[], id: string): PostComment | null {
  for (const comment of comments) {
    if (comment.id === id) return comment;
    const child = findComment(comment.replies, id);
    if (child) return child;
  }
  return null;
}

function flattenComments(comments: PostComment[]): PostComment[] {
  return comments.flatMap((comment) => [{ ...comment, replies: [] }, ...flattenComments(comment.replies)]);
}

function appendComments(current: PostComment[], incoming: PostComment[]) {
  const next = structuredClone(current) as PostComment[];
  for (const comment of flattenComments(incoming)) {
    if (findComment(next, comment.id)) continue;
    const parent = comment.parentCommentId ? findComment(next, comment.parentCommentId) : null;
    if (parent) parent.replies.push(comment);
    else next.push(comment);
  }
  return next;
}

function replaceComment(comments: PostComment[], replacement: PostComment): PostComment[] {
  return comments.map((comment) => {
    if (comment.id === replacement.id) {
      return { ...comment, ...replacement, replies: replacement.replies?.length ? replacement.replies : comment.replies };
    }
    return { ...comment, replies: replaceComment(comment.replies, replacement) };
  });
}

function removeComment(comments: PostComment[], commentId: string): PostComment[] {
  return comments
    .filter((comment) => comment.id !== commentId)
    .map((comment) => ({ ...comment, replies: removeComment(comment.replies, commentId) }));
}

function seedReplyOffsets(comments: PostComment[], current: Record<string, number | null> = {}) {
  const next = { ...current };
  for (const comment of comments) {
    if (!(comment.id in next)) {
      next[comment.id] = comment.replies.length >= REPLY_PAGE_SIZE ? comment.replies.length : null;
    }
    Object.assign(next, seedReplyOffsets(comment.replies, next));
  }
  return next;
}

function CommentThread({
  comments,
  currentUserId,
  replyNextSkips,
  replyPendingIds,
  editingId,
  editValue,
  editPending,
  deletePendingIds,
  onReply,
  onMoreReplies,
  onBeginEdit,
  onEditValue,
  onCancelEdit,
  onSaveEdit,
  onDelete,
}: {
  comments: PostComment[];
  currentUserId?: string | null;
  replyNextSkips: Record<string, number | null>;
  replyPendingIds: Set<string>;
  editingId: string | null;
  editValue: string;
  editPending: boolean;
  deletePendingIds: Set<string>;
  onReply: (comment: PostComment) => void;
  onMoreReplies: (comment: PostComment) => void;
  onBeginEdit: (comment: PostComment) => void;
  onEditValue: (value: string) => void;
  onCancelEdit: () => void;
  onSaveEdit: (comment: PostComment) => void;
  onDelete: (comment: PostComment) => void;
}) {
  return (
    <div className="space-y-4">
      {comments.map((comment) => {
        const nextReplySkip = comment.id in replyNextSkips
          ? replyNextSkips[comment.id]
          : comment.replies.length >= REPLY_PAGE_SIZE ? comment.replies.length : null;
        const replyPending = replyPendingIds.has(comment.id);
        const deletePending = deletePendingIds.has(comment.id);
        return (
          <div key={comment.id} className="flex gap-3">
            <CommentAvatar comment={comment} />
            <div className="min-w-0 flex-1">
              <div className="rounded-xl bg-accent/45 px-3 py-2.5">
                <p className="text-xs font-semibold text-foreground">{comment.authorName}</p>
                {editingId === comment.id ? (
                  <div className="mt-2 space-y-2">
                    <Textarea
                      aria-label="Edit comment"
                      rows={2}
                      maxLength={2000}
                      autoFocus
                      value={editValue}
                      onChange={(event) => onEditValue(event.target.value)}
                    />
                    <div className="flex justify-end gap-2">
                      <Button type="button" size="xs" variant="ghost" disabled={editPending} onClick={onCancelEdit}>Cancel</Button>
                      <Button type="button" size="xs" disabled={editPending || !editValue.trim()} aria-label="Save comment" onClick={() => onSaveEdit(comment)}>
                        {editPending ? "Saving…" : "Save"}
                      </Button>
                    </div>
                  </div>
                ) : (
                  <p className="mt-0.5 whitespace-pre-wrap text-sm leading-5 text-foreground">{comment.content}</p>
                )}
              </div>
              <button type="button" aria-label={`Reply to ${comment.authorName}`} onClick={() => onReply(comment)} className="mt-1 px-2 text-xs text-muted-foreground hover:text-foreground">Reply</button>
              {currentUserId === comment.authorId ? (
                <>
                  <button type="button" aria-label={`Edit comment by ${comment.authorName}`} onClick={() => onBeginEdit(comment)} className="mt-1 px-2 text-xs text-muted-foreground hover:text-foreground">Edit</button>
                  <AlertDialog>
                    <AlertDialogTrigger asChild>
                      <button type="button" aria-label={`Delete comment by ${comment.authorName}`} className="mt-1 px-2 text-xs text-muted-foreground hover:text-destructive">Delete</button>
                    </AlertDialogTrigger>
                    <AlertDialogContent>
                      <AlertDialogHeader>
                        <AlertDialogTitle>Delete comment?</AlertDialogTitle>
                        <AlertDialogDescription>This permanently removes your comment and its replies.</AlertDialogDescription>
                      </AlertDialogHeader>
                      <AlertDialogFooter>
                        <AlertDialogCancel disabled={deletePending}>Cancel</AlertDialogCancel>
                        <AlertDialogAction
                          disabled={deletePending}
                          aria-label="Delete comment"
                          onClick={(event) => {
                            event.preventDefault();
                            onDelete(comment);
                          }}
                        >
                          {deletePending ? "Deleting…" : "Delete"}
                        </AlertDialogAction>
                      </AlertDialogFooter>
                    </AlertDialogContent>
                  </AlertDialog>
                </>
              ) : null}
              {comment.replies.length > 0 ? (
                <div className="mt-3 border-l border-border/50 pl-3">
                  <CommentThread
                    comments={comment.replies}
                    currentUserId={currentUserId}
                    replyNextSkips={replyNextSkips}
                    replyPendingIds={replyPendingIds}
                    editingId={editingId}
                    editValue={editValue}
                    editPending={editPending}
                    deletePendingIds={deletePendingIds}
                    onReply={onReply}
                    onMoreReplies={onMoreReplies}
                    onBeginEdit={onBeginEdit}
                    onEditValue={onEditValue}
                    onCancelEdit={onCancelEdit}
                    onSaveEdit={onSaveEdit}
                    onDelete={onDelete}
                  />
                </div>
              ) : null}
              {nextReplySkip !== null ? (
                <button type="button" disabled={replyPending} aria-label={`Load more replies to ${comment.authorName}`} onClick={() => onMoreReplies(comment)} className="mt-2 text-xs font-medium text-primary disabled:opacity-60">
                  {replyPending ? "Loading…" : "Load more replies"}
                </button>
              ) : null}
            </div>
          </div>
        );
      })}
    </div>
  );
}

function CommentsPanel({
  postId,
  currentUserId,
  onCommentCountChange,
  onCommentCountReconciled,
  onCommentCountInvalidated,
}: {
  postId: string;
  currentUserId?: string | null;
  onCommentCountChange?: (change: number) => void;
  onCommentCountReconciled?: (count: number) => void;
  onCommentCountInvalidated?: () => void;
}) {
  const [comments, setComments] = React.useState<PostComment[]>([]);
  const [nextSkip, setNextSkip] = React.useState<number | null>(null);
  const [loading, setLoading] = React.useState(true);
  const [pagePending, setPagePending] = React.useState(false);
  const [commentPending, setCommentPending] = React.useState(false);
  const [commentText, setCommentText] = React.useState("");
  const [replyingTo, setReplyingTo] = React.useState<PostComment | null>(null);
  const [replyNextSkips, setReplyNextSkips] = React.useState<Record<string, number | null>>({});
  const [replyPendingIds, setReplyPendingIds] = React.useState<Set<string>>(new Set());
  const [editingId, setEditingId] = React.useState<string | null>(null);
  const [editValue, setEditValue] = React.useState("");
  const [editPending, setEditPending] = React.useState(false);
  const [deletePendingIds, setDeletePendingIds] = React.useState<Set<string>>(new Set());
  const [error, setError] = React.useState<string | null>(null);
  const pagePendingRef = React.useRef(false);
  const commentPendingRef = React.useRef(false);
  const editPendingRef = React.useRef(false);
  const deletePendingIdsRef = React.useRef(new Set<string>());
  const replyPendingIdsRef = React.useRef(new Set<string>());

  React.useEffect(() => {
    let active = true;
    void loadPostCommentsPageAction(postId, 0, COMMENT_PAGE_SIZE)
      .then((page) => {
        if (!active) return;
        setComments(page.items);
        setReplyNextSkips(seedReplyOffsets(page.items));
        setNextSkip(page.nextSkip);
      })
      .catch((reason) => {
        if (!active) return;
        const message = reason instanceof Error ? reason.message : "Comments could not be loaded.";
        setError(message);
        toast.error(message);
      })
      .finally(() => active && setLoading(false));
    return () => { active = false; };
  }, [postId]);

  async function loadMore() {
    if (nextSkip === null || pagePendingRef.current) return;
    pagePendingRef.current = true;
    setPagePending(true);
    setError(null);
    try {
      const page = await loadPostCommentsPageAction(postId, nextSkip, COMMENT_PAGE_SIZE);
      setComments((current) => appendComments(current, page.items));
      setNextSkip(page.nextSkip);
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : "More comments could not be loaded.";
      setError(message);
      toast.error(message);
    } finally {
      pagePendingRef.current = false;
      setPagePending(false);
    }
  }

  async function loadMoreReplies(comment: PostComment) {
    if (replyPendingIdsRef.current.has(comment.id)) return;
    const skip = comment.id in replyNextSkips ? replyNextSkips[comment.id] : comment.replies.length;
    if (skip === null) return;
    replyPendingIdsRef.current.add(comment.id);
    setReplyPendingIds(new Set(replyPendingIdsRef.current));
    setError(null);
    try {
      const page = await loadPostCommentRepliesPageAction(postId, comment.id, skip, REPLY_PAGE_SIZE);
      setComments((current) => appendComments(current, page.items));
      setReplyNextSkips((current) => seedReplyOffsets(page.items, { ...current, [comment.id]: page.nextSkip }));
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : "More replies could not be loaded.";
      setError(message);
      toast.error(message);
    } finally {
      replyPendingIdsRef.current.delete(comment.id);
      setReplyPendingIds(new Set(replyPendingIdsRef.current));
    }
  }

  async function submitComment(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const value = commentText.trim();
    if (!value || commentPendingRef.current) return;
    commentPendingRef.current = true;
    setCommentPending(true);
    setError(null);
    try {
      const persisted = await createPostComment(postId, {
        content: value,
        ...(replyingTo ? { parentCommentId: replyingTo.id } : {}),
      });
      setComments((current) => {
        if (!persisted.parentCommentId) return [persisted, ...current];
        return appendComments(current, [persisted]);
      });
      setCommentText("");
      setReplyingTo(null);
      onCommentCountChange?.(1);
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : "Comment could not be published.";
      setError(message);
      toast.error(message);
    } finally {
      commentPendingRef.current = false;
      setCommentPending(false);
    }
  }

  async function saveEdit(comment: PostComment) {
    const value = editValue.trim();
    if (!value || editPendingRef.current) return;
    editPendingRef.current = true;
    setEditPending(true);
    setError(null);
    try {
      const persisted = await updatePostComment(postId, comment.id, value);
      setComments((current) => replaceComment(current, persisted));
      setEditingId(null);
      setEditValue("");
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : "Comment could not be updated.";
      setError(message);
      toast.error(message);
    } finally {
      editPendingRef.current = false;
      setEditPending(false);
    }
  }

  async function remove(comment: PostComment) {
    if (deletePendingIdsRef.current.has(comment.id)) return;
    deletePendingIdsRef.current.add(comment.id);
    setDeletePendingIds(new Set(deletePendingIdsRef.current));
    setError(null);
    try {
      const result = await deletePostComment(postId, comment.id);
      setComments((current) => removeComment(current, comment.id));
      if (result.kind === "confirmed") onCommentCountReconciled?.(result.commentsCount);
      else onCommentCountInvalidated?.();
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : "Comment could not be deleted.";
      setError(message);
      toast.error(message);
    } finally {
      deletePendingIdsRef.current.delete(comment.id);
      setDeletePendingIds(new Set(deletePendingIdsRef.current));
    }
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col px-4 pb-4 sm:px-0 sm:pb-0">
      <div className="min-h-0 flex-1 overflow-y-auto py-2">
        {loading ? (
          <p className="flex items-center gap-2 text-sm text-muted-foreground"><Loader2 className="size-4 animate-spin" /> Loading comments…</p>
        ) : comments.length > 0 ? (
          <CommentThread
            comments={comments}
            currentUserId={currentUserId}
            replyNextSkips={replyNextSkips}
            replyPendingIds={replyPendingIds}
            editingId={editingId}
            editValue={editValue}
            editPending={editPending}
            deletePendingIds={deletePendingIds}
            onReply={setReplyingTo}
            onMoreReplies={(comment) => void loadMoreReplies(comment)}
            onBeginEdit={(comment) => {
              setEditingId(comment.id);
              setEditValue(comment.content);
            }}
            onEditValue={setEditValue}
            onCancelEdit={() => setEditingId(null)}
            onSaveEdit={(comment) => void saveEdit(comment)}
            onDelete={(comment) => void remove(comment)}
          />
        ) : <p className="text-sm text-muted-foreground">No comments yet.</p>}
        {nextSkip !== null ? (
          <Button type="button" variant="ghost" size="sm" disabled={pagePending} onClick={() => void loadMore()} className="mt-4 w-full">
            {pagePending ? "Loading…" : "Load more comments"}
          </Button>
        ) : null}
      </div>
      {error ? <p role="alert" className="mt-2 text-sm text-destructive">{error}</p> : null}
      <form onSubmit={submitComment} className="mt-4 flex items-end gap-2 border-t pt-4">
        <div className="min-w-0 flex-1">
          {replyingTo ? (
            <p className="mb-1 text-xs text-muted-foreground">
              Replying to {replyingTo.authorName}
              <button type="button" onClick={() => setReplyingTo(null)} className="ml-1 text-primary">Cancel</button>
            </p>
          ) : null}
          <Textarea
            value={commentText}
            onChange={(event) => setCommentText(event.target.value)}
            rows={1}
            maxLength={2000}
            placeholder={replyingTo ? "Add a reply…" : "Add a comment…"}
            className="min-h-10 resize-none"
          />
        </div>
        <Button type="submit" size="icon" disabled={commentPending || !commentText.trim()} aria-label={replyingTo ? "Publish reply" : "Publish comment"}>
          <Send className="size-4" />
        </Button>
      </form>
    </div>
  );
}

export function PostComments({
  postId,
  currentUserId,
  open,
  onOpenChange,
  onCommentCountChange,
  onCommentCountReconciled,
  onCommentCountInvalidated,
}: {
  postId: string;
  currentUserId?: string | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCommentCountChange?: (change: number) => void;
  onCommentCountReconciled?: (count: number) => void;
  onCommentCountInvalidated?: () => void;
}): React.JSX.Element {
  const isMobile = useResponsiveMobile();
  const panel = open ? (
    <CommentsPanel
      key={postId}
      postId={postId}
      currentUserId={currentUserId}
      onCommentCountChange={onCommentCountChange}
      onCommentCountReconciled={onCommentCountReconciled}
      onCommentCountInvalidated={onCommentCountInvalidated}
    />
  ) : null;

  return isMobile ? (
    <Drawer open={open} onOpenChange={onOpenChange}>
      <DrawerContent className="max-h-[85vh]">
        <DrawerHeader>
          <DrawerTitle>Comments</DrawerTitle>
          <DrawerDescription>Read the discussion or add a reply.</DrawerDescription>
        </DrawerHeader>
        {panel}
      </DrawerContent>
    </Drawer>
  ) : open ? (
    <section aria-label="Comments" className="mx-4 mt-3 border-t px-0 pb-4 pt-4 sm:mx-6">
      <div className="mb-3 flex items-center justify-between gap-3">
        <div>
          <h3 className="text-sm font-semibold text-foreground">Comments</h3>
          <p className="text-xs text-muted-foreground">Read the discussion or add a reply.</p>
        </div>
        <Button type="button" variant="ghost" size="xs" onClick={() => onOpenChange(false)}>Close</Button>
      </div>
      {panel}
    </section>
  ) : <></>;
}
