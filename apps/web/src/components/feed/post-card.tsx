"use client";

import { Link } from "@/i18n/navigation";
import { hydrateSocialPost, recordPostView } from "@/lib/feed/actions";
import type { SocialFeedItem } from "@/lib/feed/contracts";
import {
  hydratePublicTestingEvent,
  type TestingEventHydration,
} from "@/lib/testing-lab/public-event-hydration";
import { formatSocialDate, formatSocialDateTime } from "@/lib/feed/format";
import { EventCoverArt } from "@/components/testing-lab/landing/event-cover-art";
import { CalendarDays, CheckCircle2, Users } from "lucide-react";
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

/** Matches auto-published testing event announcements ("🧪 New testing event: …"). */
const TESTING_EVENT_ANNOUNCEMENT =
  /🧪 New testing event:\s*(.+?)\s*Event starts\s*([^.]*)\.\s*Details:\s*(\/testing-lab\/events\/[a-z0-9-]+)/iu;

function parseTestingEventAnnouncement(content: string) {
  const match = content.match(TESTING_EVENT_ANNOUNCEMENT);
  if (!match) return null;
  return { name: match[1]!.trim(), startsLabel: match[2]!.trim(), href: match[3]! };
}

/**
 * The single feed pattern for testing events: artwork hero, then a footer with
 * schedule and capacity, the event name, description, and a Join row.
 * Session items pass their structured data; announcement posts hydrate it from
 * the public events API.
 */
function TestingEventEmbed({
  href,
  name,
  startsAt: startsAtProp,
  startsLabel,
  registeredTesterCount,
  maxTesters,
  availableTesterCount,
}: {
  href: string;
  name: string;
  startsAt?: string;
  startsLabel?: string;
  registeredTesterCount?: number;
  maxTesters?: number | null;
  availableTesterCount?: number | null;
}) {
  const eventId = href.split("/").at(-1) ?? "";
  const [hydrated, setHydrated] = React.useState<TestingEventHydration | null>(
    null,
  );

  React.useEffect(() => {
    let cancelled = false;
    Promise.resolve(hydratePublicTestingEvent(eventId))
      .then((data) => {
        if (!cancelled && data) setHydrated(data);
      })
      .catch(() => {
        // Hydration is optional; the embed keeps its parsed fallback.
      });
    return () => {
      cancelled = true;
    };
  }, [eventId]);

  const startsAt = startsAtProp ?? hydrated?.startsAt ?? null;
  const registered = registeredTesterCount ?? hydrated?.registeredTesterCount ?? null;
  const max = maxTesters ?? hydrated?.maxTesters ?? null;
  const available = availableTesterCount ?? hydrated?.availableTesterCount ?? null;
  const showCapacity = registered != null || hydrated != null;
  return (
    <div className="overflow-hidden rounded-xl border border-border bg-card">
      {/* Even pixel heights (the 21:9 ratio produced odd values like 749x321). */}
      <div className="relative h-[336px] w-full max-md:h-[152px]">
        <EventCoverArt seed={eventId} />
      </div>
      <div className="flex flex-col gap-2 px-4 pb-4 pt-3 sm:px-5">
        {/* Schedule on the left, capacity on the right. */}
        <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1 text-sm text-muted-foreground">
          <span className="inline-flex items-center gap-1.5">
            <CalendarDays className="size-4 shrink-0" aria-hidden="true" />
            {startsAt
              ? formatSocialDateTime(startsAt)
              : startsLabel || "Schedule pending"}
          </span>
          {showCapacity ? (
            <span className="inline-flex items-center gap-3">
              <span className="inline-flex items-center gap-1.5">
                <Users className="size-4 shrink-0" aria-hidden="true" />
                {max == null
                  ? `${registered ?? 0} ${(registered ?? 0) === 1 ? "tester" : "testers"} signed in`
                  : `${registered ?? 0}/${max} testers signed in`}
              </span>
              {available != null && available > 0 ? (
                <span className="font-medium text-primary">
                  {available} spots left
                </span>
              ) : null}
            </span>
          ) : null}
        </div>
        <h3 className="truncate text-lg font-bold leading-snug text-foreground sm:text-xl">
          {hydrated?.name ?? name}
        </h3>
        {hydrated?.description ? (
          <p className="line-clamp-2 text-sm leading-6 text-muted-foreground">
            {hydrated.description}
          </p>
        ) : null}
      </div>
      <div className="flex justify-end border-t border-border p-3 sm:px-4">
        <Link
          href={href}
          className="inline-flex h-8 items-center rounded-md border border-border px-4 text-sm font-medium text-primary transition hover:bg-primary/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
        >
          Join
        </Link>
      </div>
    </div>
  );
}

function TestingSessionCard({
  item,
  currentUserId,
}: {
  item: SocialFeedItem;
  currentUserId?: string | null;
}) {
  const session = item.testingSession;
  if (!session) return null;
  return (
    <article data-testid="post-card" className="py-2 text-card-foreground">
      <header className="flex items-center gap-3 px-4 py-4 pb-2 sm:px-6">
        <Link href={`/social/profiles/${item.author.handle || item.author.userId}`} className="rounded-full bg-gradient-to-br from-primary via-highlight to-success p-[2px]">
          <span className="block rounded-full border-2 border-background">
            <Avatar name={item.author.displayName} id={item.author.userId} url={item.author.avatarUrl} />
          </span>
        </Link>
        <div className="min-w-0 flex-1">
          <p className="flex items-center gap-1.5 text-sm">
            <span className="truncate font-semibold text-foreground">{item.author.displayName}</span>
            {item.author.isVerified ? <CheckCircle2 className="size-3.5 shrink-0 text-primary" aria-label="Verified" /> : null}
            <span className="hidden truncate text-muted-foreground sm:inline">@{item.author.handle}</span>
          </p>
          <p suppressHydrationWarning className="text-xs text-muted-foreground">
            {timeAgo(item.createdAt)} · Testing Lab event
          </p>
        </div>
        <PostAuthorFollow
          authorId={item.author.userId}
          authorName={item.author.displayName}
          currentUserId={currentUserId}
          initialFollowing={item.viewer.isFollowingAuthor}
        />
      </header>
      <div className="px-4 pt-0 sm:px-6">
        <TestingEventEmbed
          href={`/testing-lab/events/${item.id}`}
          name={session.name}
          startsAt={session.startsAt}
          registeredTesterCount={session.registeredTesterCount}
          maxTesters={session.maxTesters}
          availableTesterCount={session.availableTesterCount}
        />
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

  if (item.kind === "TestingSession") return <TestingSessionCard item={item} currentUserId={currentUserId} />;
  const post = item.post;
  if (!post || deleted) return <></>;
  const announcement = parseTestingEventAnnouncement(content);

  return (
    <article
      ref={articleRef}
      data-testid="post-card"
      // Announcement posts let the event embed carry the visual weight; the
      // card slab background and divider are reserved for regular posts.
      className={
        announcement
          ? "py-2 text-card-foreground"
          : "border-b border-border/35 bg-card py-2 text-card-foreground"
      }
    >
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

      <div className={announcement ? "px-4 pt-0 sm:px-6" : "px-4 pt-4 sm:px-6"}>
        {announcement ? null : <Caption text={content} />}
        {post.repostedPost ? (
          <div className="mt-3 rounded-xl bg-accent/40 p-3">
            <p className="text-xs font-semibold text-foreground">{post.repostedPost.author.displayName} <span className="font-normal text-muted-foreground">@{post.repostedPost.author.handle}</span></p>
            <p className="mt-1 text-sm text-foreground">{post.repostedPost.content}</p>
          </div>
        ) : null}
        {item.tags.length > 0 ? (
          <div className="mt-3 flex flex-wrap gap-2">{item.tags.map((tag) => <Link key={tag} href={`/?tag=${encodeURIComponent(tag)}`} className="text-xs font-medium text-primary">#{tag}</Link>)}</div>
        ) : null}
        {announcement ? (
          <TestingEventEmbed
            href={announcement.href}
            name={announcement.name}
            startsLabel={announcement.startsLabel}
          />
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
