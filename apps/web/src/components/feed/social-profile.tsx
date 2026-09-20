"use client";

import { Link } from "@/i18n/navigation";
import { followCreator } from "@/lib/feed/actions";
import type {
  SocialProfile,
  SocialProfilePost,
  SocialProfileProject,
} from "@/lib/feed/contracts";
import { Button } from "@game-guild/ui/components/button";
import { ArrowLeft, BadgeCheck, ExternalLink, MapPin } from "lucide-react";
import Image from "next/image";
import * as React from "react";
import { toast } from "sonner";

function initials(name: string) {
  return name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]).join("").toUpperCase();
}

export function SocialProfileView({
  profile,
  currentUserId,
  posts,
  projects,
  collectionsError,
}: {
  profile: SocialProfile;
  currentUserId: string | null;
  posts: SocialProfilePost[];
  projects: SocialProfileProject[];
  collectionsError?: string | null;
}): React.JSX.Element {
  const [following, setFollowing] = React.useState(profile.isFollowing);
  const [followerCount, setFollowerCount] = React.useState(profile.followerCount);
  const [pending, setPending] = React.useState(false);
  const isOwn = profile.userId === currentUserId;

  async function toggleFollow() {
    if (pending) return;
    const previous = following;
    const previousFollowerCount = followerCount;
    const next = !previous;
    setFollowing(next);
    setFollowerCount(Math.max(0, previousFollowerCount + (next ? 1 : -1)));
    setPending(true);
    try {
      const state = await followCreator(profile.userId, next);
      setFollowing(state.isFollowing);
      setFollowerCount(
        Math.max(
          0,
          previousFollowerCount +
            (state.isFollowing === previous ? 0 : state.isFollowing ? 1 : -1),
        ),
      );
    } catch (error) {
      setFollowing(previous);
      setFollowerCount(previousFollowerCount);
      toast.error(error instanceof Error ? error.message : "Follow could not be updated.");
    } finally {
      setPending(false);
    }
  }

  return (
    <main className="mx-auto min-h-[calc(100svh-4rem)] w-full max-w-[820px] bg-background pb-16 text-foreground">
      <div className="px-4 py-4 sm:px-6">
        <Button variant="ghost" size="sm" render={<Link href="/" />}>
          <ArrowLeft className="size-4" /> Back to feed
        </Button>
      </div>

      <section className="overflow-hidden bg-card text-card-foreground">
        <div className="relative h-40 bg-accent sm:h-52">
          {profile.bannerUrl ? (
            <Image src={profile.bannerUrl} alt="" fill unoptimized className="object-cover" sizes="820px" />
          ) : (
            <div className="absolute inset-0 bg-[radial-gradient(circle_at_20%_20%,color-mix(in_oklab,var(--primary)_30%,transparent),transparent_55%)]" />
          )}
        </div>
        <div className="px-4 pb-6 sm:px-6">
          <div className="-mt-12 flex items-end justify-between gap-4 sm:-mt-14">
            <span className="relative flex size-24 shrink-0 items-center justify-center overflow-hidden rounded-full ring-4 ring-card bg-muted text-xl font-bold sm:size-28">
              {profile.avatarUrl ? (
                <Image src={profile.avatarUrl} alt={`${profile.displayName} avatar`} fill unoptimized className="object-cover" sizes="112px" />
              ) : initials(profile.displayName)}
            </span>
            {!isOwn ? (
              <Button
                type="button"
                variant={following ? "secondary" : "default"}
                disabled={pending}
                aria-label={`${following ? "Unfollow" : "Follow"} ${profile.displayName}`}
                aria-pressed={following}
                onClick={() => void toggleFollow()}
              >
                {following ? "Following" : "Follow"}
              </Button>
            ) : (
              <Button variant="secondary" render={<Link href="/workspace/settings/profile" />}>Edit profile</Button>
            )}
          </div>

          <div className="mt-4">
            <h1 className="flex items-center gap-1.5 text-xl font-semibold sm:text-2xl">
              {profile.displayName}
              {profile.isVerified ? <BadgeCheck className="size-5 text-primary" aria-label="Verified" /> : null}
            </h1>
            <p className="text-sm text-muted-foreground">@{profile.handle}</p>
            {profile.headline ? <p className="mt-3 text-sm font-medium">{profile.headline}</p> : null}
            {profile.bio ? <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">{profile.bio}</p> : null}
            <div className="mt-3 flex flex-wrap gap-x-4 gap-y-2 text-xs text-muted-foreground">
              {profile.location ? <span className="inline-flex items-center gap-1"><MapPin className="size-3.5" /> {profile.location}</span> : null}
              {profile.websiteUrl ? (
                <a href={profile.websiteUrl} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-primary hover:underline">
                  Website <ExternalLink className="size-3" />
                </a>
              ) : null}
            </div>
          </div>

          <dl className="mt-5 flex gap-6 text-sm">
            {[
              [profile.postCount, "Posts"],
              [profile.projectCount, "Projects"],
              [followerCount, "Followers"],
              [profile.followingCount, "Following"],
            ].map(([value, label]) => (
              <div key={label}><dt className="text-xs text-muted-foreground">{label}</dt><dd className="font-semibold">{value}</dd></div>
            ))}
          </dl>
        </div>
      </section>

      <div className="space-y-8 px-4 py-8 sm:px-6">
        {collectionsError ? (
          <p role="alert" className="rounded-lg border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            {collectionsError}
          </p>
        ) : null}

        <section aria-labelledby="profile-projects-heading">
          <h2 id="profile-projects-heading" className="text-lg font-semibold">Projects</h2>
          {projects.length > 0 ? (
            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              {projects.map((project) => (
                <article key={project.id} className="overflow-hidden rounded-xl border bg-card text-card-foreground">
                  {project.imageUrl ? (
                    <div className="relative aspect-video bg-muted">
                      <Image src={project.imageUrl} alt="" fill unoptimized className="object-cover" sizes="(min-width: 640px) 386px, 100vw" />
                    </div>
                  ) : null}
                  <div className="space-y-2 p-4">
                    <h3 className="font-semibold">
                      <Link href={`/projects/${project.slug}`} className="hover:underline">{project.title}</Link>
                    </h3>
                    {project.shortDescription ? <p className="text-sm text-muted-foreground">{project.shortDescription}</p> : null}
                  </div>
                </article>
              ))}
            </div>
          ) : (
            <p className="mt-3 text-sm text-muted-foreground">No public projects yet.</p>
          )}
        </section>

        <section aria-labelledby="profile-posts-heading">
          <h2 id="profile-posts-heading" className="text-lg font-semibold">Posts</h2>
          {posts.length > 0 ? (
            <div className="mt-4 space-y-4">
              {posts.map((post) => (
                <article key={post.id} className="overflow-hidden rounded-xl border bg-card p-4 text-card-foreground">
                  <p className="whitespace-pre-wrap text-sm leading-6">{post.content}</p>
                  {post.mediaUrl && post.mediaType?.startsWith("image/") ? (
                    <div className="relative mt-4 aspect-video overflow-hidden rounded-lg bg-muted">
                      <Image src={post.mediaUrl} alt="" fill unoptimized className="object-cover" sizes="772px" />
                    </div>
                  ) : null}
                  {post.mediaUrl && post.mediaType?.startsWith("video/") ? (
                    <video src={post.mediaUrl} controls preload="metadata" className="mt-4 aspect-video w-full rounded-lg bg-black" />
                  ) : null}
                  <Link href={`/social/posts/${post.id}`} className="mt-4 inline-flex text-sm font-medium text-primary hover:underline">Open post</Link>
                </article>
              ))}
            </div>
          ) : (
            <p className="mt-3 text-sm text-muted-foreground">No public posts yet.</p>
          )}
        </section>
      </div>
    </main>
  );
}
