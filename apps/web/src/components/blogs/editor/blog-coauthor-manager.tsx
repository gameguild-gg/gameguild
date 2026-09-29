"use client";

import { useState, useTransition } from "react";
import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
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
import { Loader2, Trash2, UserPlus } from "lucide-react";
import {
  coauthorAdd,
  coauthorRemove,
  resolveProfileByHandle,
  transferPrimary,
} from "@/lib/blogs/actions";
import type { BlogPostAuthorView } from "@/lib/blogs/queries";

export interface BlogCoauthor {
  userId: string;
  handle: string | null;
  displayName: string | null;
  isPrimary: boolean;
}

interface BlogCoauthorManagerProps {
  post: BlogPostAuthorView;
  viewerUserId: string;
  coauthors: BlogCoauthor[];
  onChanged: (post: BlogPostAuthorView) => void;
}

export function BlogCoauthorManager({
  post,
  viewerUserId,
  coauthors: initialCoauthors,
  onChanged,
}: BlogCoauthorManagerProps) {
  const [members, setMembers] = useState<BlogCoauthor[]>(initialCoauthors);
  const [handleInput, setHandleInput] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [transferTarget, setTransferTarget] = useState<BlogCoauthor | null>(
    null,
  );
  const [pending, startTransition] = useTransition();

  const viewerIsPrimary = post.primaryAuthorId === viewerUserId;

  const addByHandle = () => {
    if (!viewerIsPrimary || pending) return;
    const handle = handleInput.trim().replace(/^@/, "");
    if (!handle) return;
    startTransition(async () => {
      setError(null);
      const resolved = await resolveProfileByHandle(handle);
      if (!resolved.success) {
        setError(resolved.error);
        return;
      }
      if (members.some((c) => c.userId === resolved.userId) || post.primaryAuthorId === resolved.userId) {
        setError(`@${handle} is already an author.`);
        return;
      }
      const result = await coauthorAdd(post.id, resolved.userId);
      if (!result.success) {
        setError(result.error);
        return;
      }
      setHandleInput("");
      setMembers((current) => [
        ...current,
        { userId: resolved.userId, handle, displayName: null, isPrimary: false },
      ]);
    });
  };

  const remove = (member: BlogCoauthor) => {
    if (!viewerIsPrimary || pending || member.isPrimary) return;
    startTransition(async () => {
      setError(null);
      const result = await coauthorRemove(post.id, member.userId);
      if (!result.success) {
        setError(result.error);
        return;
      }
      setMembers((current) => current.filter((c) => c.userId !== member.userId));
    });
  };

  const confirmTransfer = () => {
    if (!transferTarget) return;
    const target = transferTarget;
    setTransferTarget(null);
    startTransition(async () => {
      setError(null);
      const result = await transferPrimary(post.id, target.userId);
      if (!result.success) {
        setError(result.error);
        return;
      }
      const saved = result.data as unknown as BlogPostAuthorView;
      setMembers((current) =>
        current.map((c) =>
          c.userId === target.userId
            ? { ...c, isPrimary: true }
            : { ...c, isPrimary: false },
        ),
      );
      onChanged(saved);
    });
  };

  return (
    <div className="space-y-3 p-4">
      <div className="space-y-2">
        <Label htmlFor="blog-coauthors">Authors</Label>
        <ul className="space-y-1.5">
          {members.map((member) => (
            <li
              key={member.userId}
              className="flex items-center justify-between gap-2 rounded-md border px-2.5 py-1.5 text-sm"
            >
              <span className="min-w-0 truncate">
                {member.displayName || member.handle || member.userId}
                {member.isPrimary ? (
                  <span className="ml-2 rounded bg-primary/10 px-1.5 py-0.5 text-xs text-primary">
                    Primary
                  </span>
                ) : null}
                {member.userId === viewerUserId ? (
                  <span className="ml-1 text-xs text-muted-foreground">(you)</span>
                ) : null}
              </span>
              {member.isPrimary ? null : viewerIsPrimary ? (
                <span className="flex shrink-0 items-center gap-1">
                  <Button
                    type="button"
                    variant="ghost"
                    size="xs"
                    disabled={pending}
                    onClick={() => setTransferTarget(member)}
                  >
                    Make primary
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon-sm"
                    aria-label={`Remove ${member.handle ?? member.userId}`}
                    disabled={pending}
                    onClick={() => remove(member)}
                  >
                    <Trash2 />
                  </Button>
                </span>
              ) : (
                <Tooltip>
                  <TooltipTrigger
                    render={
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon-sm"
                        aria-label={`Manage ${member.handle ?? member.userId}`}
                        disabled
                      />
                    }
                  >
                    <UserPlus className="text-muted-foreground" />
                  </TooltipTrigger>
                  <TooltipContent>
                    Only the primary author can manage co-authors.
                  </TooltipContent>
                </Tooltip>
              )}
            </li>
          ))}
        </ul>
      </div>

      {viewerIsPrimary ? (
        <div className="space-y-1.5">
          <Label htmlFor="blog-coauthor-add">Add co-author</Label>
          <div className="flex gap-2">
            <Input
              id="blog-coauthor-add"
              value={handleInput}
              placeholder="@handle"
              onChange={(event) => setHandleInput(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  addByHandle();
                }
              }}
            />
            <Button
              type="button"
              size="sm"
              disabled={pending || !handleInput.trim()}
              onClick={addByHandle}
            >
              {pending ? <Loader2 className="animate-spin" /> : null}
              Add
            </Button>
          </div>
        </div>
      ) : (
        <Tooltip>
          <TooltipTrigger
            render={
              <Input
                id="blog-coauthor-add"
                placeholder="@handle"
                disabled
                aria-label="Add co-author (primary author only)"
              />
            }
          >
            <span />
          </TooltipTrigger>
          <TooltipContent>
            Only the primary author can manage co-authors.
          </TooltipContent>
        </Tooltip>
      )}

      {error ? (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      ) : null}

      <AlertDialog
        open={transferTarget !== null}
        onOpenChange={(open) => !open && setTransferTarget(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Transfer primary authorship?</AlertDialogTitle>
            <AlertDialogDescription>
              {transferTarget?.handle
                ? `@${transferTarget.handle}`
                : "This co-author"}{" "}
              becomes the primary author. They gain publish and delete rights,
              and you cannot undo this yourself.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className={buttonVariants({ variant: "destructive" })}
              onClick={confirmTransfer}
            >
              Transfer primary
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
