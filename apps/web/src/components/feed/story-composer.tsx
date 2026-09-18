"use client";

import { createStory, getSocialMediaStatus } from "@/lib/feed/actions";
import type { SocialMediaAsset, SocialStory } from "@/lib/feed/contracts";
import { uploadSocialMediaWithProgress } from "@/lib/feed/social-media-upload";
import { Button } from "@game-guild/ui/components/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@game-guild/ui/components/dialog";
import { Textarea } from "@game-guild/ui/components/textarea";
import { Loader2, Plus } from "lucide-react";
import * as React from "react";
import { SocialMediaPicker, type SelectedSocialMedia } from "./social-media-picker";

const STORY_CAPTION_LIMIT = 500;
const STORY_PROCESSING_ATTEMPTS = 20;

function initials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toUpperCase();
}

function processingDelay(attempt: number) {
  return Math.min(500 + attempt * 250, 2_000);
}

async function waitForStoryMedia(
  asset: SocialMediaAsset,
  signal: AbortSignal,
  onProcessing: () => void,
) {
  let current = asset;
  if (current.state === "Processing") onProcessing();
  for (
    let attempt = 0;
    current.state === "Processing" && attempt < STORY_PROCESSING_ATTEMPTS;
    attempt += 1
  ) {
    if (signal.aborted) throw new Error("Media upload cancelled.");
    await new Promise((resolve) => window.setTimeout(resolve, processingDelay(attempt)));
    if (signal.aborted) throw new Error("Media upload cancelled.");
    current = await getSocialMediaStatus(current.assetReferenceId);
  }
  if (current.state === "Rejected") {
    throw new Error("The story media could not be processed.");
  }
  if (current.state !== "Ready") {
    throw new Error("Story media processing timed out. Please try again.");
  }
  return current;
}

export function StoryComposer({
  userName,
  onPublished,
}: {
  userName: string;
  onPublished: (story: SocialStory) => void;
}): React.JSX.Element {
  const [open, setOpen] = React.useState(false);
  const [media, setMedia] = React.useState<SelectedSocialMedia | null>(null);
  const [caption, setCaption] = React.useState("");
  const [pending, setPending] = React.useState(false);
  const [phase, setPhase] = React.useState<"idle" | "uploading" | "processing" | "failed">("idle");
  const [progress, setProgress] = React.useState<{
    loaded: number;
    total: number;
    percent: number;
  } | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const pendingRef = React.useRef(false);
  const controllerRef = React.useRef<AbortController | null>(null);
  const readyAssetRef = React.useRef<SocialMediaAsset | null>(null);
  const publishedIdsRef = React.useRef(new Set<string>());

  React.useEffect(() => () => controllerRef.current?.abort(), []);

  function resetDraft() {
    setMedia(null);
    setCaption("");
    setProgress(null);
    setError(null);
    setPhase("idle");
    readyAssetRef.current = null;
  }

  function changeMedia(next: SelectedSocialMedia | null) {
    readyAssetRef.current = null;
    setProgress(null);
    setError(null);
    setPhase("idle");
    setMedia(next);
  }

  async function publish(event?: React.FormEvent<HTMLFormElement>) {
    event?.preventDefault();
    if (!media || pendingRef.current) return;
    pendingRef.current = true;
    setPending(true);
    setError(null);
    const controller = new AbortController();
    controllerRef.current = controller;

    try {
      let asset = readyAssetRef.current;
      if (!asset) {
        setPhase("uploading");
        asset = await uploadSocialMediaWithProgress(media.file, {
          signal: controller.signal,
          onProgress: setProgress,
        });
        asset = await waitForStoryMedia(asset, controller.signal, () =>
          setPhase("processing"),
        );
        readyAssetRef.current = asset;
      }
      const story = await createStory(asset.assetReferenceId, caption.trim() || null);
      if (!publishedIdsRef.current.has(story.id)) {
        publishedIdsRef.current.add(story.id);
        onPublished(story);
      }
      resetDraft();
      setOpen(false);
    } catch (reason) {
      const message =
        reason instanceof Error ? reason.message : "The story could not be published.";
      if (phase === "processing" || /process|timed out/i.test(message)) {
        readyAssetRef.current = null;
      }
      setError(message);
      setPhase("failed");
    } finally {
      if (controllerRef.current === controller) controllerRef.current = null;
      pendingRef.current = false;
      setPending(false);
    }
  }

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="group flex w-[4.5rem] shrink-0 flex-col items-center gap-2 text-center"
        aria-label="Add story"
      >
        <span className="relative flex size-14 items-center justify-center rounded-full bg-muted text-sm font-bold text-foreground transition-transform group-hover:scale-[1.03]">
          {initials(userName)}
          <span className="absolute -bottom-0.5 -right-0.5 flex size-5 items-center justify-center rounded-full border-2 border-background bg-primary text-primary-foreground">
            <Plus className="size-3" aria-hidden="true" />
          </span>
        </span>
        <span className="w-full truncate text-[11px] font-medium text-muted-foreground">
          Your story
        </span>
      </button>

      <Dialog
        open={open}
        onOpenChange={(next) => {
          if (!pending) setOpen(next);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create a story</DialogTitle>
            <DialogDescription>
              Share one image or video for the next 24 hours.
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={publish} className="space-y-4">
            <SocialMediaPicker value={media} onChange={changeMedia} disabled={pending} />
            <div>
              <Textarea
                aria-label="Story caption"
                value={caption}
                maxLength={STORY_CAPTION_LIMIT}
                disabled={pending}
                onChange={(event) => setCaption(event.target.value)}
                placeholder="Add a caption (optional)"
              />
              <p className="mt-1 text-right text-xs text-muted-foreground">
                {caption.length}/{STORY_CAPTION_LIMIT}
              </p>
            </div>

            {progress ? (
              <div aria-live="polite">
                <div
                  role="progressbar"
                  aria-label="Story upload progress"
                  aria-valuemin={0}
                  aria-valuemax={100}
                  aria-valuenow={progress.percent}
                  className="h-2 overflow-hidden rounded-full bg-primary/20"
                >
                  <div
                    className="h-full bg-primary"
                    style={{ width: `${progress.percent}%` }}
                  />
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  Uploading {progress.loaded} of {progress.total} bytes ({progress.percent}%)
                </p>
              </div>
            ) : null}
            {phase === "processing" ? (
              <p className="text-sm text-muted-foreground" aria-live="polite">
                Processing story media…
              </p>
            ) : null}
            {error ? (
              <div role="alert" className="flex items-center justify-between gap-3">
                <p className="text-sm text-destructive">{error}</p>
                {media ? (
                  <Button type="button" variant="ghost" size="sm" onClick={() => void publish()}>
                    Retry story upload
                  </Button>
                ) : null}
              </div>
            ) : null}

            <DialogFooter>
              {pending ? (
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => controllerRef.current?.abort()}
                  aria-label="Cancel upload"
                >
                  Cancel upload
                </Button>
              ) : (
                <Button type="button" variant="outline" onClick={() => setOpen(false)}>
                  Cancel
                </Button>
              )}
              <Button
                type="submit"
                disabled={!media || pending}
                aria-label={pending ? "Publishing story" : "Publish story"}
              >
                {pending ? <Loader2 className="size-4 animate-spin" /> : null}
                {pending ? "Publishing…" : "Publish story"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  );
}
