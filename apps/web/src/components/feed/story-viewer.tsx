"use client";

import { deleteStory, markStoryViewed } from "@/lib/feed/actions";
import type { SocialStory } from "@/lib/feed/contracts";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@game-guild/ui/components/alert-dialog";
import { Button } from "@game-guild/ui/components/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@game-guild/ui/components/dialog";
import { ChevronLeft, ChevronRight, Pause, Play, Trash2 } from "lucide-react";
import Image from "next/image";
import * as React from "react";

const IMAGE_STORY_DURATION_MS = 5_000;
const STORY_TICK_MS = 100;

export interface SocialStoryPreview extends SocialStory {
  authorName: string;
  authorHandle: string;
  authorAvatarUrl: string | null;
  isOwn: boolean;
}

interface StoryViewerProps {
  stories: SocialStoryPreview[];
  activeStoryId: string | null;
  onActiveStoryChange: (storyId: string | null) => void;
  onStoryViewed: (storyId: string) => void;
  onStoryDeleted: (storyId: string) => void;
}

function useReducedMotion() {
  const [reduced, setReduced] = React.useState(false);
  React.useEffect(() => {
    const media = window.matchMedia("(prefers-reduced-motion: reduce)");
    const update = () => setReduced(media.matches);
    update();
    media.addEventListener?.("change", update);
    return () => media.removeEventListener?.("change", update);
  }, []);
  return reduced;
}

export function StoryViewer({
  ...props
}: StoryViewerProps): React.JSX.Element {
  return <StoryViewerSession key={props.activeStoryId ?? "closed"} {...props} />;
}

function StoryViewerSession({
  stories,
  activeStoryId,
  onActiveStoryChange,
  onStoryViewed,
  onStoryDeleted,
}: StoryViewerProps): React.JSX.Element {
  const activeIndex = activeStoryId
    ? stories.findIndex((story) => story.id === activeStoryId)
    : -1;
  const active = activeIndex >= 0 ? stories[activeIndex] ?? null : null;
  const reducedMotion = useReducedMotion();
  const [elapsed, setElapsed] = React.useState(0);
  const [videoProgress, setVideoProgress] = React.useState(0);
  const [manuallyPaused, setManuallyPaused] = React.useState(false);
  const [pointerPaused, setPointerPaused] = React.useState(false);
  const [deleteOpen, setDeleteOpen] = React.useState(false);
  const [deletePending, setDeletePending] = React.useState(false);
  const [deleteError, setDeleteError] = React.useState<string | null>(null);
  const deletePendingRef = React.useRef(false);
  const viewedRef = React.useRef(new Set<string>());
  const videoRef = React.useRef<HTMLVideoElement>(null);
  const isVideo = active?.mediaType?.startsWith("video") ?? false;
  const paused = reducedMotion || manuallyPaused || pointerPaused;

  const selectOffset = React.useCallback(
    (offset: -1 | 1) => {
      if (!active) return;
      const next = activeIndex + offset;
      if (next < 0) return;
      if (next >= stories.length) {
        onActiveStoryChange(null);
        return;
      }
      onActiveStoryChange(stories[next]?.id ?? null);
    },
    [active, activeIndex, onActiveStoryChange, stories],
  );

  React.useEffect(() => {
    if (!active || active.isViewed || viewedRef.current.has(active.id)) return;
    viewedRef.current.add(active.id);
    onStoryViewed(active.id);
    void markStoryViewed(active.id).catch(() => {
      // Viewing stays available even when the idempotent receipt cannot be saved.
    });
  }, [active, onStoryViewed]);

  React.useEffect(() => {
    if (!active || isVideo || paused) return;
    const timer = window.setInterval(() => {
      setElapsed((current) => Math.min(IMAGE_STORY_DURATION_MS, current + STORY_TICK_MS));
    }, STORY_TICK_MS);
    return () => window.clearInterval(timer);
  }, [active, isVideo, paused]);

  React.useEffect(() => {
    if (!active || isVideo || elapsed < IMAGE_STORY_DURATION_MS || paused) return;
    selectOffset(1);
  }, [active, elapsed, isVideo, paused, selectOffset]);

  React.useEffect(() => {
    const video = videoRef.current;
    if (!video) return;
    if (paused) video.pause();
    else {
      const playback = video.play();
      if (playback) void playback.catch(() => undefined);
    }
  }, [activeStoryId, paused]);

  async function confirmDelete() {
    if (!active || deletePendingRef.current) return;
    deletePendingRef.current = true;
    setDeletePending(true);
    setDeleteError(null);
    try {
      await deleteStory(active.id);
      onStoryDeleted(active.id);
      onActiveStoryChange(null);
      setDeleteOpen(false);
    } catch (reason) {
      setDeleteError(
        reason instanceof Error ? reason.message : "Story could not be deleted.",
      );
    } finally {
      deletePendingRef.current = false;
      setDeletePending(false);
    }
  }

  const progress = isVideo
    ? videoProgress
    : Math.round((elapsed / IMAGE_STORY_DURATION_MS) * 100);

  return (
    <>
      <Dialog
        open={Boolean(active)}
        onOpenChange={(open) => {
          if (!open) onActiveStoryChange(null);
        }}
      >
        <DialogContent
          className="overflow-hidden p-0 sm:max-w-lg"
          showCloseButton
          onKeyDown={(event) => {
            if (event.key === "ArrowLeft") {
              event.preventDefault();
              selectOffset(-1);
            } else if (event.key === "ArrowRight") {
              event.preventDefault();
              selectOffset(1);
            } else if (event.key === "Escape") {
              event.preventDefault();
              onActiveStoryChange(null);
            }
          }}
        >
          {active ? (
            <>
              <div className="absolute inset-x-3 top-2 z-10 flex gap-1 pr-9">
                {stories.map((story, index) => {
                  const value = index < activeIndex ? 100 : index === activeIndex ? progress : 0;
                  return (
                    <div
                      key={story.id}
                      role={index === activeIndex ? "progressbar" : undefined}
                      aria-label={index === activeIndex ? "Story progress" : undefined}
                      aria-valuemin={index === activeIndex ? 0 : undefined}
                      aria-valuemax={index === activeIndex ? 100 : undefined}
                      aria-valuenow={index === activeIndex ? value : undefined}
                      className="h-1 flex-1 overflow-hidden rounded-full bg-foreground/20"
                    >
                      <span
                        className="block h-full bg-primary"
                        style={{ width: `${value}%` }}
                      />
                    </div>
                  );
                })}
              </div>
              <DialogHeader className="px-4 pt-5 text-left">
                <DialogTitle>{active.authorName}</DialogTitle>
                <DialogDescription>
                  @{active.authorHandle} ·{" "}
                  <time dateTime={active.createdAt}>
                    {new Date(active.createdAt).toLocaleString()}
                  </time>
                </DialogDescription>
              </DialogHeader>
              <div
                data-testid="story-media-surface"
                className="relative flex min-h-80 touch-none items-center justify-center bg-black"
                onPointerDown={() => setPointerPaused(true)}
                onPointerUp={() => setPointerPaused(false)}
                onPointerCancel={() => setPointerPaused(false)}
                onPointerLeave={() => setPointerPaused(false)}
              >
                {isVideo ? (
                  <video
                    ref={videoRef}
                    src={active.mediaUrl ?? undefined}
                    aria-label={`Story video by ${active.authorName}`}
                    autoPlay={!reducedMotion}
                    playsInline
                    onEnded={() => {
                      if (!reducedMotion) selectOffset(1);
                    }}
                    onTimeUpdate={(event) => {
                      const video = event.currentTarget;
                      setVideoProgress(
                        video.duration > 0
                          ? Math.min(100, Math.round((video.currentTime / video.duration) * 100))
                          : 0,
                      );
                    }}
                    className="max-h-[70vh] w-full object-contain"
                  />
                ) : active.mediaUrl ? (
                  <Image
                    src={active.mediaUrl}
                    alt={active.caption || `Story by ${active.authorName}`}
                    width={900}
                    height={1200}
                    unoptimized
                    className="max-h-[70vh] w-full object-contain"
                  />
                ) : null}
                <Button
                  type="button"
                  variant="secondary"
                  size="icon"
                  aria-label="Previous story"
                  disabled={activeIndex === 0}
                  onClick={() => selectOffset(-1)}
                  className="absolute left-3 top-1/2 -translate-y-1/2 rounded-full bg-background/80"
                >
                  <ChevronLeft className="size-5" />
                </Button>
                <Button
                  type="button"
                  variant="secondary"
                  size="icon"
                  aria-label="Next story"
                  onClick={() => selectOffset(1)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 rounded-full bg-background/80"
                >
                  <ChevronRight className="size-5" />
                </Button>
              </div>
              <div className="flex items-center gap-3 px-4 pb-4">
                <p className="min-w-0 flex-1 text-sm text-foreground">
                  {active.caption}
                </p>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => setManuallyPaused((current) => !current)}
                  aria-label={manuallyPaused ? "Play story" : "Pause story"}
                  aria-pressed={manuallyPaused}
                >
                  {manuallyPaused ? <Play className="size-4" /> : <Pause className="size-4" />}
                  {manuallyPaused ? "Play" : "Pause"}
                </Button>
                {active.isOwn ? (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    aria-label="Delete story"
                    onClick={() => {
                      setDeleteError(null);
                      setDeleteOpen(true);
                    }}
                  >
                    <Trash2 className="size-4" /> Delete
                  </Button>
                ) : null}
              </div>
              {reducedMotion ? (
                <p className="sr-only" aria-live="polite">
                  Automatic advance is off because reduced motion is enabled.
                </p>
              ) : null}
            </>
          ) : null}
        </DialogContent>
      </Dialog>

      <AlertDialog
        open={deleteOpen}
        onOpenChange={(open) => {
          if (!deletePending) setDeleteOpen(open);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete story?</AlertDialogTitle>
            <AlertDialogDescription>
              This removes the story before its scheduled expiration.
            </AlertDialogDescription>
          </AlertDialogHeader>
          {deleteError ? (
            <p role="alert" className="text-sm text-destructive">
              {deleteError}
            </p>
          ) : null}
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deletePending}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              disabled={deletePending}
              aria-label={deletePending ? "Deleting story" : "Confirm story deletion"}
              onClick={(event) => {
                event.preventDefault();
                void confirmDelete();
              }}
            >
              {deletePending ? "Deleting…" : "Delete"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
