"use client";

import {
  followCreator,
  hydrateSocialPost,
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
  CircleHelp,
  Heart,
  HeartHandshake,
  Lightbulb,
  MessageCircle,
  PartyPopper,
  Share2,
  ThumbsUp,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import * as React from "react";
import { toast } from "sonner";

const REACTIONS: Array<{
  value: SocialReaction;
  label: string;
  icon: LucideIcon;
}> = [
  { value: "Like", label: "Like", icon: ThumbsUp },
  { value: "Love", label: "Love", icon: Heart },
  { value: "Insightful", label: "Insightful", icon: Lightbulb },
  { value: "Celebrate", label: "Celebrate", icon: PartyPopper },
  { value: "Support", label: "Support", icon: HeartHandshake },
  { value: "Curious", label: "Curious", icon: CircleHelp },
];

function errorMessage(error: unknown, fallback: string) {
  return error instanceof Error ? error.message : fallback;
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
  const [reactionHydrationPending, setReactionHydrationPending] = React.useState(false);
  const [reactionRetryAvailable, setReactionRetryAvailable] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [status, setStatus] = React.useState<string | null>(null);
  const reactionPendingRef = React.useRef(false);
  const savePendingRef = React.useRef(false);
  const reactionHydrationPendingRef = React.useRef(false);

  async function react(next: SocialReaction | null) {
    if (reactionPendingRef.current) return;
    reactionPendingRef.current = true;
    const previous = reaction;
    const previousCount = reactionCount;
    setReaction(next);
    setReactionCount(Math.max(0, previousCount + (previous ? -1 : 0) + (next ? 1 : 0)));
    setReactionPending(true);
    setError(null);
    setStatus(null);
    setReactionRetryAvailable(false);
    try {
      const state = await setPostReaction(postId, next);
      setReaction(state.reaction);
      if (state.kind === "confirmed") {
        setReactionCount(state.reactionsCount);
      } else {
        setReactionRetryAvailable(true);
        setStatus("Reaction saved. Refresh the authoritative count.");
      }
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

  async function retryReactionHydration() {
    if (reactionHydrationPendingRef.current) return;
    reactionHydrationPendingRef.current = true;
    setReactionHydrationPending(true);
    setStatus("Refreshing the authoritative reaction count.");
    try {
      const post = await hydrateSocialPost(postId);
      setReaction(post.viewer.reaction);
      setReactionCount(post.engagement.reactionsCount);
      setReactionRetryAvailable(false);
      setStatus(null);
    } catch {
      setStatus("Reaction saved, but the authoritative count is still unavailable.");
    } finally {
      reactionHydrationPendingRef.current = false;
      setReactionHydrationPending(false);
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
        disabled={reactionPending || reactionHydrationPending}
        aria-label={reactionLabel}
        aria-pressed={Boolean(reaction)}
        className={`inline-flex min-h-9 items-center gap-2 rounded-lg px-2 text-sm transition hover:bg-accent disabled:opacity-60 ${reaction ? "text-destructive" : "text-muted-foreground hover:text-foreground"}`}
      >
        {selectedReaction ? (
          <selectedReaction.icon
            className={`size-[19px] ${selectedReaction.value === "Love" ? "fill-current" : ""}`}
            aria-hidden="true"
          />
        ) : (
          <Heart className="size-[19px]" aria-hidden="true" />
        )}
        {reactionCount > 0 ? reactionCount : null}
      </button>
      <DropdownMenu>
        <DropdownMenuTrigger
          render={
            <button
              type="button"
              aria-label="Choose reaction"
              className="-ml-1.5 flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent"
            />
          }
        >
          <ChevronDown className="size-3.5" />
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="flex min-w-0 gap-1 p-1.5">
          {REACTIONS.map((entry) => (
            <DropdownMenuItem
              key={entry.value}
              onClick={() => void react(entry.value)}
              disabled={reactionPending || reactionHydrationPending}
              className="flex size-8 justify-center p-0"
              aria-label={entry.label}
            >
              <entry.icon className="size-4" aria-hidden="true" />
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
      {reactionRetryAvailable ? (
        <button
          type="button"
          aria-label="Retry reaction count refresh"
          disabled={reactionHydrationPending}
          onClick={() => void retryReactionHydration()}
          className="text-xs font-medium text-primary disabled:opacity-60"
        >
          {reactionHydrationPending ? "Refreshing…" : "Refresh count"}
        </button>
      ) : null}
      {error ? <span role="alert" className="sr-only">{error}</span> : null}
      {status ? <span role="status" className="sr-only">{status}</span> : null}
    </div>
  );
}
