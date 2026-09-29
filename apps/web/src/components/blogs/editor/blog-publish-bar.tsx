"use client";

import { useState, useTransition } from "react";
import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
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
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@game-guild/ui/components/tooltip";
import { buttonVariants } from "@game-guild/ui/components/button";
import { Check, EyeOff, Eye, Loader2, Trash2 } from "lucide-react";
import { deletePost, publish, unpublish } from "@/lib/blogs/actions";
import type { BlogPostAuthorView } from "@/lib/blogs/queries";

interface BlogPublishBarProps {
  post: BlogPostAuthorView;
  viewerUserId: string;
  onChanged: (post: BlogPostAuthorView) => void;
  onDeleted: () => void;
}

export function BlogPublishBar({
  post,
  viewerUserId,
  onChanged,
  onDeleted,
}: BlogPublishBarProps) {
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  const viewerIsPrimary = post.primaryAuthorId === viewerUserId;
  const isPublished = post.status === "Published";

  const runPublish = () => {
    if (!viewerIsPrimary || pending) return;
    startTransition(async () => {
      setError(null);
      const result = isPublished ? await unpublish(post.id) : await publish(post.id);
      if (!result.success) {
        setError(result.error);
        return;
      }
      onChanged(result.data as unknown as BlogPostAuthorView);
    });
  };

  const runDelete = () => {
    setConfirming(false);
    if (!viewerIsPrimary || pending) return;
    startTransition(async () => {
      setError(null);
      const result = await deletePost(post.id);
      if (!result.success) {
        setError(result.error);
        return;
      }
      onDeleted();
    });
  };

  const statusBadge = (
    <Badge variant={isPublished ? "default" : "outline"} data-testid="blog-status">
      {isPublished ? "Published" : "Draft"}
    </Badge>
  );

  return (
    <div className="flex items-center gap-2">
      {statusBadge}
      {isPublished && post.publishedAt ? (
        <span className="hidden text-xs text-muted-foreground sm:inline">
          {new Date(post.publishedAt).toLocaleDateString()}
        </span>
      ) : null}

      {error ? (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      ) : null}

      {viewerIsPrimary ? (
        <>
          <Button
            size="sm"
            disabled={pending}
            aria-label={isPublished ? "Unpublish post" : "Publish post"}
            onClick={runPublish}
          >
            {pending ? <Loader2 className="animate-spin" /> : isPublished ? <EyeOff /> : <Eye />}
            {isPublished ? "Unpublish" : "Publish"}
          </Button>
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label="Delete post"
            disabled={pending}
            onClick={() => setConfirming(true)}
          >
            <Trash2 />
          </Button>
        </>
      ) : (
        <Tooltip>
          <TooltipTrigger
            render={
              <Button size="sm" disabled aria-label="Publish (primary author only)">
                <Check />
                {isPublished ? "Unpublish" : "Publish"}
              </Button>
            }
          >
            <span />
          </TooltipTrigger>
          <TooltipContent>
            Only the primary author can publish, unpublish, or delete.
          </TooltipContent>
        </Tooltip>
      )}

      <AlertDialog open={confirming} onOpenChange={setConfirming}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this post?</AlertDialogTitle>
            <AlertDialogDescription>
              The post and its public URL are removed permanently. This cannot
              be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className={buttonVariants({ variant: "destructive" })}
              onClick={runDelete}
            >
              Delete post
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
