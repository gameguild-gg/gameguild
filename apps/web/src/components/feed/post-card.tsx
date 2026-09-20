"use client";

import { Link } from "@/i18n/navigation";
import { hydrateSocialPost, recordPostView } from "@/lib/feed/actions";
import type { SocialFeedItem } from "@/lib/feed/contracts";
import { formatSocialDate, formatSocialDateTime } from "@/lib/feed/format";
import { Button } from "@game-guild/ui/components/button";
import { CalendarDays, CheckCircle2, Clock3, Users } from "lucide-react";
import Image from "next/image";
import * as React from "react";
import { PostComments } from "./post-comments";
import { PostAuthorFollow, PostEngagement } from "./post-engagement";
import { PostOwnerMenu } from "./post-owner-menu";
import { RepostDialog } from "./repost-dialog";

function initials(name: string | null | undefined, id: string | null | undefined) {
  const parts = (name?.trim() || id?.replace(/[-_]+/g, " ") || "GG").split(/\s+/).filter(Boolean);
  return parts.length > 1
    ? `${parts[0]?.[0] ?? ""}${parts.at(-1)?.[0] ?? ""}`.toUpperCase()
    : parts[0]?.slice(0, 2).toUpperCase() || "GG";
}

function timeAgo(iso: string) {
  const milliseconds = new Date(iso).getTime();
  if (Number.isNaN(milliseconds)) return "";
  const minutes = Math.max(1, Math.round((Date.now() - milliseconds) / 60_000));
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours}h`;
  const days = Math.round(hours / 24);
  return days < 7 ? `${days}d` : formatSocialDate(milliseconds);
}

const TOKEN_PATTERN = /(#[\p{L}\p{N}_]+|@[\p{L}\p{N}_]+)/gu;

function Caption({ text }: { text: string }) {
  const nodes: React.ReactNode[] = [];
  let last = 0;
  for (const [key, match] of [...text.matchAll(TOKEN_PATTERN)].entries()) {
    const start = match.index ?? 0;
    if (start > last) nodes.push(text.slice(last, start));
    nodes.push(<span key={key} className="font-semibold text-primary">{match[0]}</span>);
    last = start + match[0].length;
  }
  if (last < text.length) nodes.push(text.slice(last));
  return <p className="whitespace-pre-wrap break-words text-[15px] leading-6 text-foreground">{nodes}</p>;
}

function Avatar({ name, id, url }: { name: string; id: string; url: string | null }) {
  return (
    <span className="flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-full bg-muted text-xs font-bold text-foreground">
      {url ? <Image src={url} alt="" width={40} height={40} unoptimized className="size-10 object-cover" /> : initials(name, id)}
    </span>
  );
}

function TestingSessionCard({ item }: { item: SocialFeedItem }) {
  const session = item.testingSession;
  if (!session) return null;
  return (
    <article data-testid="post-card" className="bg-card px-4 py-5 text-card-foreground sm:px-6">
      <div className="flex items-start gap-3">
        <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/12 text-primary"><CalendarDays className="size-5" aria-hidden="true" /></span>
        <div className="min-w-0 flex-1">
          <p className="text-xs font-semibold uppercase tracking-wide text-primary">Testing Lab</p>
          <h2 className="mt-1 text-base font-semibold text-foreground">{session.name}</h2>
          <div className="mt-3 flex flex-wrap gap-x-5 gap-y-2 text-sm text-muted-foreground">
            <span className="inline-flex items-center gap-1.5"><Clock3 className="size-4" aria-hidden="true" />{formatSocialDateTime(new Date(session.startsAt))}</span>
            <span className="inline-flex items-center gap-1.5"><Users className="size-4" aria-hidden="true" />{session.availableTesterCount} spots available</span>
          </div>
        </div>
        <Button size="sm" render={<Link href={`/testing-lab/events/${item.id}`} />}>View session</Button>
      </div>
    </article>
  );
}

export function PostCard({ item, currentUserId }: { item: SocialFeedItem; currentUserId?: string | null }): React.JSX.Element {
  const [deleted, setDeleted] = React.useState(false);
  const [content, setContent] = React.useState(item.post?.content ?? "");
  const [commentsOpen, setCommentsOpen] = React.useState(false);
  const [commentCount, setCommentCount] = React.useState(item.engagement.commentsCount);
  const articleRef = React.useRef<HTMLElement>(null);

  async function refreshCommentCount() {
    try {
      const post = await hydrateSocialPost(item.id);
      setCommentCount(post.engagement.commentsCount);
    } catch {
      // A committed deletion remains removed locally; a later feed refresh will
      // reconcile a projection that is temporarily unavailable.
    }
  }

  React.useEffect(() => {
    if (item.kind === "TestingSession" || typeof IntersectionObserver === "undefined") return;
    const node = articleRef.current;
    if (!node) return;
    let recorded = false;
    const observer = new IntersectionObserver(
      (entries) => {
        if (recorded || !entries.some((entry) => entry.isIntersecting && entry.intersectionRatio >= 0.5)) return;
        recorded = true;
        observer.disconnect();
        void recordPostView(item.id).catch(() => {
          // Analytics must never interrupt reading the feed.
        });
      },
      { threshold: 0.5 },
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, [item.id, item.kind]);

  if (item.kind === "TestingSession") return <TestingSessionCard item={item} />;
  const post = item.post;
  if (!post || deleted) return <></>;

  return (
    <article ref={articleRef} data-testid="post-card" className="bg-card py-2 text-card-foreground">
      <header className="flex items-center gap-3 px-4 py-4 sm:px-6">
        <Link href={`/social/profiles/${item.author.handle || item.author.userId}`} className="rounded-full bg-gradient-to-br from-primary via-highlight to-success p-[2px]">
          <span className="block rounded-full border-2 border-background"><Avatar name={item.author.displayName} id={item.author.userId} url={item.author.avatarUrl} /></span>
        </Link>
        <div className="min-w-0 flex-1">
          <p className="flex items-center gap-1.5 text-sm">
            <span className="truncate font-semibold text-foreground">{item.author.displayName}</span>
            {item.author.isVerified ? <CheckCircle2 className="size-3.5 shrink-0 text-primary" aria-label="Verified" /> : null}
            <span className="hidden truncate text-muted-foreground sm:inline">@{item.author.handle}</span>
          </p>
          <p suppressHydrationWarning className="text-xs text-muted-foreground">{timeAgo(item.createdAt)}{post.isEdited ? " · edited" : ""}</p>
        </div>
        <PostAuthorFollow authorId={item.author.userId} authorName={item.author.displayName} currentUserId={currentUserId} initialFollowing={item.viewer.isFollowingAuthor} />
        <PostOwnerMenu postId={item.id} content={content} canEdit={item.viewer.canEdit} canDelete={item.viewer.canDelete} onContentChange={setContent} onDeleted={() => setDeleted(true)} />
      </header>

      {post.mediaUrl ? (
        post.mediaType?.toLowerCase().startsWith("video") ? (
          <video src={post.mediaUrl} controls preload="metadata" className="max-h-[620px] w-full bg-background object-contain" />
        ) : (
          <div className="relative aspect-[4/3] max-h-[620px] w-full overflow-hidden bg-background">
            <Image src={post.mediaUrl} alt={`Media shared by ${item.author.displayName}`} fill unoptimized className="object-cover" sizes="(min-width: 1280px) 47.5rem, 100vw" />
          </div>
        )
      ) : null}

      <div className="px-4 pt-4 sm:px-6">
        <Caption text={content} />
        {post.repostedPost ? (
          <div className="mt-3 rounded-xl bg-accent/40 p-3">
            <p className="text-xs font-semibold text-foreground">{post.repostedPost.author.displayName} <span className="font-normal text-muted-foreground">@{post.repostedPost.author.handle}</span></p>
            <p className="mt-1 text-sm text-foreground">{post.repostedPost.content}</p>
          </div>
        ) : null}
        {item.tags.length > 0 ? (
          <div className="mt-3 flex flex-wrap gap-2">{item.tags.map((tag) => <Link key={tag} href={`/?tag=${encodeURIComponent(tag)}`} className="text-xs font-medium text-primary">#{tag}</Link>)}</div>
        ) : null}
        <PostEngagement
          postId={item.id}
          authorName={item.author.displayName}
          initialReaction={item.viewer.reaction}
          initialReactionCount={item.engagement.reactionsCount}
          initialSaved={item.viewer.isSaved}
          commentCount={commentCount}
          onOpenComments={() => setCommentsOpen(true)}
          repostControl={<RepostDialog postId={item.id} initialReposted={item.viewer.hasReposted} initialCount={item.engagement.repostsCount} />}
        />
      </div>
      <PostComments
        postId={item.id}
        currentUserId={currentUserId}
        open={commentsOpen}
        onOpenChange={setCommentsOpen}
        onCommentCountChange={(change) => setCommentCount((count) => Math.max(0, count + change))}
        onCommentCountReconciled={setCommentCount}
        onCommentCountInvalidated={() => void refreshCommentCount()}
      />
    </article>
  );
}
