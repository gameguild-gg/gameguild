"use client";

import { fetchViewerReaction, setReaction } from "@/lib/blogs/actions";
import { Button } from "@game-guild/ui/components/button";
import { Heart, Loader2 } from "lucide-react";
import Link from "next/link";
import * as React from "react";

export interface BlogReactionsProps {
  postId: string;
  currentUserId: string | null;
  /** Public reaction count carried by the post detail/summary DTO. */
  initialReactionCount: number;
}

export const __testHooks: {
  fetchViewerReaction: ((postId: string) => Promise<{ reacted: boolean }>) | null;
  setReaction: ((postId: string, react: boolean) => Promise<{ ok: boolean }>) | null;
} = { fetchViewerReaction: null, setReaction: null };

export function BlogReactions({ postId, currentUserId, initialReactionCount }: BlogReactionsProps): React.JSX.Element {
  const signedIn = currentUserId !== null;
  const [reacted, setReacted] = React.useState(false);
  const [count, setCount] = React.useState(Math.max(0, initialReactionCount));
  const [pending, setPending] = React.useState(false);
  const hydratedRef = React.useRef(false);

  const fetchViewer = __testHooks.fetchViewerReaction ?? fetchViewerReaction;
  const mutateReaction = __testHooks.setReaction ?? setReaction;

  React.useEffect(() => {
    if (!signedIn || hydratedRef.current) return;
    hydratedRef.current = true;
    void fetchViewer(postId).then((state) => setReacted(state.reacted));
  }, [postId, signedIn, fetchViewer]);

  async function toggle() {
    if (pending || !signedIn) return;
    const previous = reacted;
    const next = !previous;
    // Optimistic.
    setReacted(next);
    setCount((current) => Math.max(0, current + (next ? 1 : -1)));
    setPending(true);
    try {
      const result = await mutateReaction(postId, next);
      if (!result.ok) {
        setReacted(previous);
        setCount((current) => Math.max(0, current + (previous ? 1 : -1)));
      }
    } catch {
      setReacted(previous);
      setCount((current) => Math.max(0, current + (previous ? 1 : -1)));
    } finally {
      setPending(false);
    }
  }

  if (!signedIn) {
    return (
      <span className="inline-flex items-center gap-2 text-sm text-muted-foreground">
        <Button type="button" variant="ghost" size="sm" className="gap-2" disabled aria-label="Sign in to like this post">
          <Heart className="size-[18px]" aria-hidden="true" />
          {count > 0 ? count : null}
        </Button>
        <Link href="/sign-in?redirectTo=blogs" className="text-xs text-primary hover:underline">
          Sign in to like
        </Link>
      </span>
    );
  }

  return (
    <button
      type="button"
      onClick={() => void toggle()}
      disabled={pending}
      aria-pressed={reacted}
      aria-label={reacted ? "Remove like" : "Like this post"}
      className={`inline-flex min-h-9 items-center gap-2 rounded-lg px-2 text-sm transition hover:bg-accent disabled:opacity-60 ${reacted ? "text-destructive" : "text-muted-foreground hover:text-foreground"}`}
    >
      {pending ? (
        <Loader2 className="size-[18px] animate-spin" aria-hidden="true" />
      ) : (
        <Heart className={`size-[18px] ${reacted ? "fill-current" : ""}`} aria-hidden="true" />
      )}
      {count > 0 ? count : null}
    </button>
  );
}
