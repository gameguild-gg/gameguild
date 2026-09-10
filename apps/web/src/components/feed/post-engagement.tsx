"use client";

import {
  followCreator,
  savePost,
  setPostReaction,
  sharePost,
} from "@/lib/feed/actions";
import type { SocialReaction } from "@/lib/feed/contracts";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@game-guild/ui/components/dropdown-menu";
import {
  Bookmark,
  ChevronDown,
  Heart,
  MessageCircle,
  Share2,
} from "lucide-react";
import * as React from "react";
import { toast } from "sonner";

const REACTIONS: Array<{ value: SocialReaction; label: string; symbol: string }> = [
  { value: "Like", label: "Like", symbol: "👍" },
  { value: "Love", label: "Love", symbol: "❤️" },
  { value: "Insightful", label: "Insightful", symbol: "💡" },
  { value: "Celebrate", label: "Celebrate", symbol: "🎉" },
  { value: "Support", label: "Support", symbol: "🙌" },
  { value: "Curious", label: "Curious", symbol: "🤔" },
];

function errorMessage(error: unknown, fallback: string) {
  return error instanceof Error ? error.message : fallback;
}

function authoritativeReactionCount(value: unknown): number | null {
  if (!value || typeof value !== "object") return null;
  const count = (value as { reactionsCount?: unknown }).reactionsCount;
  return typeof count === "number" && Number.isFinite(count) ? count : null;
}

function localizedPostUrl(postId: string) {
  const localePrefix = window.location.pathname.match(/^\/(?:pt-BR|en-US)(?=\/|$)/)?.[0] ?? "";
  return `${window.location.origin}${localePrefix}/social/posts/${postId}`;
}

export function PostAuthorFollow({
  authorId,
  authorName,
  currentUserId,
  initialFollowing,
}: {
  authorId: string;
  authorName: string;
  currentUserId?: string | null;
  initialFollowing: boolean;
}): React.JSX.Element | null {
  const [following, setFollowing] = React.useState(initialFollowing);
  const [pending, setPending] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const pendingRef = React.useRef(false);

  if (currentUserId === authorId) return null;

  async function toggleFollow() {
    if (pendingRef.current) return;
    pendingRef.current = true;
    const previous = following;
    const next = !previous;
    setFollowing(next);
    setPending(true);
    setError(null);
    try {
      const state = await followCreator(authorId, next);
      setFollowing(state.isFollowing);
    } catch (reason) {
      setFollowing(previous);
      const message = errorMessage(reason, "Creator follow could not be updated.");
      setError(message);
      toast.error(message);
    } finally {
      pendingRef.current = false;
      setPending(false);
    }
  }

  return (
    <div className="flex items-center gap-2">
      <button
        type="button"
        onClick={() => void toggleFollow()}
        disabled={pending}
        aria-label={`${following ? "Unfollow" : "Follow"} ${authorName}`}
        aria-pressed={following}
        className="rounded-full px-3 py-1 text-xs font-semibold text-primary hover:bg-primary/10 disabled:opacity-60"
      >
        {following ? "Following" : "Follow"}
      </button>
      {error ? <span role="alert" className="sr-only">{error}</span> : null}
    </div>
  );
}

export function PostEngagement({
  postId,
  authorName,
  initialReaction,
  initialReactionCount,
  initialSaved,
  commentCount,
  onOpenComments,
  repostControl,
}: {
  postId: string;
  authorName: string;
  initialReaction: SocialReaction | null;
  initialReactionCount: number;
  initialSaved: boolean;
  commentCount: number;
  onOpenComments: () => void;
  repostControl?: React.ReactNode;
}): React.JSX.Element {
  const [reaction, setReaction] = React.useState(initialReaction);
  const [reactionCount, setReactionCount] = React.useState(initialReactionCount);
  const [saved, setSaved] = React.useState(initialSaved);
  const [reactionPending, setReactionPending] = React.useState(false);
  const [savePending, setSavePending] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const reactionPendingRef = React.useRef(false);
  const savePendingRef = React.useRef(false);

  async function react(next: SocialReaction | null) {
    if (reactionPendingRef.current) return;
    reactionPendingRef.current = true;
    const previous = reaction;
    const previousCount = reactionCount;
    setReaction(next);
    setReactionCount(Math.max(0, previousCount + (previous ? -1 : 0) + (next ? 1 : 0)));
    setReactionPending(true);
    setError(null);
    try {
      const state = await setPostReaction(postId, next);
      const persisted = state?.type;
      const authoritative = REACTIONS.some((entry) => entry.value === persisted)
        ? (persisted as SocialReaction)
        : null;
      setReaction(authoritative);
      const count = authoritativeReactionCount(state);
      if (count !== null) setReactionCount(count);
    } catch (reason) {
      setReaction(previous);
      setReactionCount(previousCount);
      const message = errorMessage(reason, "Reaction could not be saved.");
      setError(message);
      toast.error(message);
    } finally {
      reactionPendingRef.current = false;
      setReactionPending(false);
    }
  }

  async function toggleSave() {
    if (savePendingRef.current) return;
    savePendingRef.current = true;
    const previous = saved;
    const next = !previous;
    setSaved(next);
    setSavePending(true);
    setError(null);
    try {
      const state = await savePost(postId, next);
      setSaved(state.isSaved);
    } catch (reason) {
      setSaved(previous);
      const message = errorMessage(reason, "Post could not be saved.");
      setError(message);
      toast.error(message);
    } finally {
      savePendingRef.current = false;
      setSavePending(false);
    }
  }

  async function share() {
    setError(null);
    try {
      await sharePost(postId);
      const url = localizedPostUrl(postId);
      if (navigator.share) {
        try {
          await navigator.share({ title: `${authorName} on GameGuild`, url });
          return;
        } catch {
          // A rejected or dismissed native sheet still gets a useful copy fallback.
        }
      }
      await navigator.clipboard.writeText(url);
      toast.success("Post link copied.");
    } catch (reason) {
      const message = errorMessage(reason, "Post could not be shared.");
      setError(message);
      toast.error(message);
    }
  }

  const selectedReaction = REACTIONS.find((entry) => entry.value === reaction);
  const reactionLabel = reaction ? `Remove ${reaction} reaction` : "React to post";

  return (
    <div className="mt-3 flex items-center gap-1">
      <button
        type="button"
        onClick={() => void react(reaction ? null : "Like")}
        disabled={reactionPending}
        aria-label={reactionLabel}
        aria-pressed={Boolean(reaction)}
        className={`inline-flex min-h-9 items-center gap-2 rounded-lg px-2 text-sm transition hover:bg-accent disabled:opacity-60 ${reaction ? "text-destructive" : "text-muted-foreground hover:text-foreground"}`}
      >
        {selectedReaction ? <span aria-hidden="true">{selectedReaction.symbol}</span> : <Heart className="size-[19px]" />}
        {reactionCount > 0 ? reactionCount : null}
      </button>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <button type="button" aria-label="Choose reaction" className="flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent">
            <ChevronDown className="size-3.5" />
          </button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="flex min-w-0 gap-1 p-2">
          {REACTIONS.map((entry) => (
            <DropdownMenuItem
              key={entry.value}
              onClick={() => void react(entry.value)}
              disabled={reactionPending}
              className="flex size-10 justify-center p-0 text-lg"
              aria-label={entry.label}
            >
              {entry.symbol}
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>
      <button
        type="button"
        onClick={onOpenComments}
        aria-label={`${commentCount} comments`}
        className="inline-flex min-h-9 items-center gap-2 rounded-lg px-2 text-sm text-muted-foreground hover:bg-accent hover:text-foreground"
      >
        <MessageCircle className="size-[19px]" />
        {commentCount > 0 ? commentCount : null}
      </button>
      {repostControl}
      <button
        type="button"
        onClick={() => void share()}
        aria-label="Share post"
        className="flex size-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-accent hover:text-foreground"
      >
        <Share2 className="size-[19px]" />
      </button>
      <button
        type="button"
        onClick={() => void toggleSave()}
        disabled={savePending}
        aria-label={saved ? "Remove saved post" : "Save post"}
        aria-pressed={saved}
        className={`ml-auto flex size-9 items-center justify-center rounded-lg hover:bg-accent disabled:opacity-60 ${saved ? "text-primary" : "text-muted-foreground hover:text-foreground"}`}
      >
        <Bookmark className={`size-[19px] ${saved ? "fill-current" : ""}`} />
      </button>
      {error ? <span role="alert" className="sr-only">{error}</span> : null}
    </div>
  );
}
