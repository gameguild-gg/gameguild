"use client";

import type { SocialProfile, SocialStory } from "@/lib/feed/contracts";
import Image from "next/image";
import * as React from "react";
import { StoryComposer } from "./story-composer";
import { StoryViewer, type SocialStoryPreview } from "./story-viewer";

export type { SocialStoryPreview } from "./story-viewer";

function initials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toUpperCase();
}

function fallbackHandle(name: string) {
  return name.toLowerCase().replace(/[^a-z0-9]+/g, "").slice(0, 64) || "member";
}

export function BuildStories({
  userName,
  stories,
  currentProfile = null,
}: {
  userName: string;
  stories: SocialStoryPreview[];
  currentProfile?: SocialProfile | null;
}): React.JSX.Element {
  const [storyItems, setStoryItems] = React.useState(stories);
  const [activeStoryId, setActiveStoryId] = React.useState<string | null>(null);

  function insertPublished(story: SocialStory) {
    const existingOwnStory = storyItems.find((item) => item.isOwn);
    const preview: SocialStoryPreview = {
      ...story,
      authorName: currentProfile?.displayName || userName,
      authorHandle:
        currentProfile?.handle || existingOwnStory?.authorHandle || fallbackHandle(userName),
      authorAvatarUrl:
        currentProfile?.avatarUrl ?? existingOwnStory?.authorAvatarUrl ?? null,
      isOwn: true,
      isViewed: true,
    };
    setStoryItems((current) =>
      current.some((item) => item.id === preview.id) ? current : [preview, ...current],
    );
  }

  return (
    <>
      <section aria-label="Stories" className="overflow-hidden px-4 py-4 sm:px-6">
        <div className="flex gap-4 overflow-x-auto pb-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
          <StoryComposer userName={userName} onPublished={insertPublished} />
          {storyItems.map((story) => (
            <button
              key={story.id}
              type="button"
              onClick={() => setActiveStoryId(story.id)}
              className="group flex w-[4.5rem] shrink-0 flex-col items-center gap-2 text-center"
              aria-label={`View ${story.authorName}'s story`}
            >
              <span
                className={`rounded-full p-[2px] ${
                  story.isViewed
                    ? "bg-border"
                    : "bg-gradient-to-br from-primary via-highlight to-success"
                }`}
              >
                <span className="flex size-[3.25rem] items-center justify-center overflow-hidden rounded-full border-2 border-background bg-muted text-xs font-bold text-foreground transition-transform group-hover:scale-[1.03]">
                  {story.authorAvatarUrl ? (
                    <Image
                      src={story.authorAvatarUrl}
                      alt=""
                      width={52}
                      height={52}
                      unoptimized
                      className="size-[3.25rem] object-cover"
                    />
                  ) : (
                    initials(story.authorName)
                  )}
                </span>
              </span>
              <span className="w-full truncate text-[11px] font-medium text-muted-foreground">
                {story.isOwn ? "Your story" : story.authorName}
              </span>
            </button>
          ))}
        </div>
      </section>

      <StoryViewer
        stories={storyItems}
        activeStoryId={activeStoryId}
        onActiveStoryChange={setActiveStoryId}
        onStoryViewed={(storyId) =>
          setStoryItems((current) =>
            current.map((story) =>
              story.id === storyId ? { ...story, isViewed: true } : story,
            ),
          )
        }
        onStoryDeleted={(storyId) =>
          setStoryItems((current) => current.filter((story) => story.id !== storyId))
        }
      />
    </>
  );
}
