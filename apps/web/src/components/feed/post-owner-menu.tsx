"use client";

import { deleteSocialPost, updateSocialPost } from "@/lib/feed/actions";
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
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@game-guild/ui/components/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@game-guild/ui/components/dropdown-menu";
import { Textarea } from "@game-guild/ui/components/textarea";
import { MoreHorizontal, Pencil, Trash2 } from "lucide-react";
import * as React from "react";
import { toast } from "sonner";

export function PostOwnerMenu({
  postId,
  content,
  canEdit,
  canDelete,
  onContentChange,
  onDeleted,
}: {
  postId: string;
  content: string;
  canEdit: boolean;
  canDelete: boolean;
  onContentChange: (content: string) => void;
  onDeleted: () => void;
}): React.JSX.Element | null {
  const [editOpen, setEditOpen] = React.useState(false);
  const [deleteOpen, setDeleteOpen] = React.useState(false);
  const [editContent, setEditContent] = React.useState(content);
  const [editPending, setEditPending] = React.useState(false);
  const [deletePending, setDeletePending] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const editPendingRef = React.useRef(false);
  const deletePendingRef = React.useRef(false);

  if (!canEdit && !canDelete) return null;

  async function submitEdit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const next = editContent.trim();
    if (!next || editPendingRef.current) return;
    editPendingRef.current = true;
    setEditPending(true);
    setError(null);
    try {
      const state = await updateSocialPost(postId, next);
      onContentChange(state.content);
      setEditOpen(false);
      toast.success("Post updated.");
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : "Post could not be updated.";
      setError(message);
      toast.error(message);
    } finally {
      editPendingRef.current = false;
      setEditPending(false);
    }
  }

  async function removePost() {
    if (deletePendingRef.current) return;
    deletePendingRef.current = true;
    setDeletePending(true);
    setError(null);
    try {
      await deleteSocialPost(postId);
      setDeleteOpen(false);
      onDeleted();
      toast.success("Post deleted.");
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : "Post could not be deleted.";
      setError(message);
      toast.error(message);
    } finally {
      deletePendingRef.current = false;
      setDeletePending(false);
    }
  }

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger
          render={<Button variant="ghost" size="icon-sm" aria-label="Post options" />}
        >
          <MoreHorizontal className="size-5" />
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          {canEdit ? (
            <DropdownMenuItem
              onSelect={() => {
                setError(null);
                setEditContent(content);
                setEditOpen(true);
              }}
            >
              <Pencil /> Edit post
            </DropdownMenuItem>
          ) : null}
          {canEdit && canDelete ? <DropdownMenuSeparator /> : null}
          {canDelete ? (
            <DropdownMenuItem
              variant="destructive"
              onSelect={() => {
                setError(null);
                setDeleteOpen(true);
              }}
            >
              <Trash2 /> Delete post
            </DropdownMenuItem>
          ) : null}
        </DropdownMenuContent>
      </DropdownMenu>

      <Dialog open={editOpen} onOpenChange={(next) => !editPending && setEditOpen(next)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit post</DialogTitle>
            <DialogDescription>Update the text shown in this post.</DialogDescription>
          </DialogHeader>
          <form onSubmit={submitEdit} className="space-y-4">
            <Textarea
              aria-label="Edit post"
              value={editContent}
              maxLength={5000}
              onChange={(event) => setEditContent(event.target.value)}
            />
            {error ? <p role="alert" className="text-sm text-destructive">{error}</p> : null}
            <DialogFooter>
              <Button type="button" variant="outline" disabled={editPending} onClick={() => setEditOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={editPending || !editContent.trim()} aria-label="Save post changes">
                {editPending ? "Saving…" : "Save"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <AlertDialog open={deleteOpen} onOpenChange={(next) => !deletePending && setDeleteOpen(next)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete post?</AlertDialogTitle>
            <AlertDialogDescription>This permanently removes the post and its discussion.</AlertDialogDescription>
          </AlertDialogHeader>
          {error ? <p role="alert" className="text-sm text-destructive">{error}</p> : null}
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deletePending}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              disabled={deletePending}
              aria-label="Delete post"
              onClick={(event) => {
                event.preventDefault();
                void removePost();
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
