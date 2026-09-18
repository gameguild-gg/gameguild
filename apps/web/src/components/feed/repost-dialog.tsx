"use client";

import { repostPost } from "@/lib/feed/actions";
import { Button } from "@game-guild/ui/components/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@game-guild/ui/components/dialog";
import {
  Drawer,
  DrawerContent,
  DrawerDescription,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
} from "@game-guild/ui/components/drawer";
import { Textarea } from "@game-guild/ui/components/textarea";
import { Repeat2 } from "lucide-react";
import * as React from "react";
import { toast } from "sonner";

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

function responseBoolean(value: unknown, key: string, fallback: boolean) {
  if (!value || typeof value !== "object") return fallback;
  const candidate = (value as Record<string, unknown>)[key];
  return typeof candidate === "boolean" ? candidate : fallback;
}

function responseCount(value: unknown, key: string, fallback: number) {
  if (!value || typeof value !== "object") return fallback;
  const candidate = (value as Record<string, unknown>)[key];
  return typeof candidate === "number" && Number.isFinite(candidate) ? candidate : fallback;
}

function RepostForm({
  commentary,
  error,
  pending,
  onCommentaryChange,
  onCancel,
  onSubmit,
}: {
  commentary: string;
  error: string | null;
  pending: boolean;
  onCommentaryChange: (value: string) => void;
  onCancel: () => void;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
}) {
  return (
    <form onSubmit={onSubmit} className="space-y-4 px-4 pb-4 sm:px-0 sm:pb-0">
      <Textarea
        aria-label="Repost commentary"
        placeholder="Add a thought (optional)"
        maxLength={1000}
        value={commentary}
        onChange={(event) => onCommentaryChange(event.target.value)}
      />
      {error ? <p role="alert" className="text-sm text-destructive">{error}</p> : null}
      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={pending}>Cancel</Button>
        <Button type="submit" disabled={pending} aria-label="Publish repost">{pending ? "Publishing…" : "Repost"}</Button>
      </div>
    </form>
  );
}

export function RepostDialog({
  postId,
  initialReposted,
  initialCount,
}: {
  postId: string;
  initialReposted: boolean;
  initialCount: number;
}): React.JSX.Element {
  const isMobile = useResponsiveMobile();
  const [open, setOpen] = React.useState(false);
  const [commentary, setCommentary] = React.useState("");
  const [reposted, setReposted] = React.useState(initialReposted);
  const [count, setCount] = React.useState(initialCount);
  const [pending, setPending] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const pendingRef = React.useRef(false);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pendingRef.current || reposted) return;
    pendingRef.current = true;
    const previousReposted = reposted;
    const previousCount = count;
    setReposted(true);
    setCount((value) => value + 1);
    setPending(true);
    setError(null);
    try {
      const state = await repostPost(postId, commentary.trim());
      setReposted(responseBoolean(state, "hasReposted", true));
      setCount(responseCount(state, "repostsCount", previousCount + 1));
      setCommentary("");
      setOpen(false);
      toast.success("Reposted.");
    } catch (reason) {
      setReposted(previousReposted);
      setCount(previousCount);
      const message = reason instanceof Error ? reason.message : "Post could not be reposted.";
      setError(message);
      toast.error(message);
    } finally {
      pendingRef.current = false;
      setPending(false);
    }
  }

  const form = (
    <RepostForm
      commentary={commentary}
      error={error}
      pending={pending}
      onCommentaryChange={setCommentary}
      onCancel={() => setOpen(false)}
      onSubmit={submit}
    />
  );

  return (
    <>
      <button
        type="button"
        onClick={() => {
          setError(null);
          setOpen(true);
        }}
        disabled={reposted || pending}
        aria-label={reposted ? "Reposted" : "Repost"}
        aria-pressed={reposted}
        className={`inline-flex min-h-9 items-center gap-2 rounded-lg px-2 text-sm hover:bg-accent disabled:opacity-70 ${reposted ? "text-success" : "text-muted-foreground hover:text-success"}`}
      >
        <Repeat2 className="size-[19px]" />
        {count > 0 ? count : null}
      </button>
      {isMobile ? (
        <Drawer open={open} onOpenChange={setOpen}>
          <DrawerContent>
            <DrawerHeader>
              <DrawerTitle>Repost this post</DrawerTitle>
              <DrawerDescription>Add optional commentary before sharing it with your followers.</DrawerDescription>
            </DrawerHeader>
            {form}
            <DrawerFooter className="hidden" />
          </DrawerContent>
        </Drawer>
      ) : (
        <Dialog open={open} onOpenChange={setOpen}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Repost this post</DialogTitle>
              <DialogDescription>Add optional commentary before sharing it with your followers.</DialogDescription>
            </DialogHeader>
            {form}
            <DialogFooter className="hidden" />
          </DialogContent>
        </Dialog>
      )}
    </>
  );
}
