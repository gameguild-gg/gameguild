import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  createPostComment: vi.fn(),
  deletePostComment: vi.fn(),
  loadPostCommentsPageAction: vi.fn(),
  updatePostComment: vi.fn(),
}));

vi.mock("@/lib/feed/actions", () => mocks);
vi.mock("next/image", () => ({ default: () => <span data-testid="next-image" /> }));

import { PostComments } from "./post-comments";
import type { PostComment } from "@/lib/feed/contracts";

function comment(id: string, content: string, parentCommentId: string | null = null): PostComment {
  return {
    id,
    postId: "post-1",
    authorId: id === "comment-1" ? "viewer-1" : `author-${id}`,
    authorName: id === "comment-1" ? "Viewer" : `Author ${id}`,
    authorHandle: id,
    authorAvatarUrl: null,
    parentCommentId,
    content,
    likesCount: 0,
    isEdited: false,
    createdAt: "2026-09-10T00:00:00Z",
    updatedAt: null,
    replies: [],
  };
}

function setViewport(mobile: boolean) {
  Object.defineProperty(window, "innerWidth", { configurable: true, value: mobile ? 500 : 1024 });
  Object.defineProperty(window, "matchMedia", {
    configurable: true,
    value: vi.fn().mockImplementation(() => ({
      matches: mobile,
      media: "(max-width: 767px)",
      onchange: null,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })),
  });
}

describe("PostComments", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setViewport(false);
    mocks.loadPostCommentsPageAction.mockResolvedValue({ items: [], nextSkip: null });
  });
  afterEach(cleanup);

  it("paginates comments and progressively reveals replies", async () => {
    const root = comment("root-1", "Root");
    root.replies = [comment("reply-1", "Reply one", root.id), comment("reply-2", "Reply two", root.id), comment("reply-3", "Reply three", root.id)];
    mocks.loadPostCommentsPageAction
      .mockResolvedValueOnce({ items: [root, comment("root-2", "Second")], nextSkip: 2 })
      .mockResolvedValueOnce({ items: [comment("root-3", "Third")], nextSkip: null });
    render(<PostComments postId="post-1" currentUserId="viewer-1" open onOpenChange={vi.fn()} />);

    expect(await screen.findByText("Reply one")).toBeInTheDocument();
    expect(screen.queryByText("Reply three")).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Load more replies to Author root-1" }));
    expect(screen.getByText("Reply three")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Load more comments" }));
    await waitFor(() => expect(mocks.loadPostCommentsPageAction).toHaveBeenLastCalledWith("post-1", 2, 10));
    expect(await screen.findByText("Third")).toBeInTheDocument();
  });

  it("creates a reply from minimal input and renders the authoritative comment", async () => {
    const root = comment("root-1", "Root");
    mocks.loadPostCommentsPageAction.mockResolvedValueOnce({ items: [root], nextSkip: null });
    const authoritative = comment("reply-new", "Canonical reply", root.id);
    mocks.createPostComment.mockResolvedValueOnce(authoritative);
    render(<PostComments postId="post-1" currentUserId="viewer-1" open onOpenChange={vi.fn()} />);

    await screen.findByText("Root");
    fireEvent.click(screen.getByRole("button", { name: "Reply to Author root-1" }));
    fireEvent.change(screen.getByPlaceholderText("Add a reply…"), { target: { value: "My draft" } });
    fireEvent.click(screen.getByRole("button", { name: "Publish reply" }));

    await waitFor(() => expect(mocks.createPostComment).toHaveBeenCalledWith("post-1", {
      content: "My draft",
      parentCommentId: "root-1",
    }));
    expect(await screen.findByText("Canonical reply")).toBeInTheDocument();
  });

  it("edits authoritatively and confirms a guarded delete", async () => {
    const own = comment("comment-1", "Original");
    mocks.loadPostCommentsPageAction.mockResolvedValueOnce({ items: [own], nextSkip: null });
    mocks.updatePostComment.mockResolvedValueOnce({ ...own, content: "Canonical edit", isEdited: true });
    let releaseDelete!: () => void;
    mocks.deletePostComment.mockImplementationOnce(() => new Promise((resolve) => {
      releaseDelete = () => resolve({ postId: "post-1", commentId: own.id, deleted: true });
    }));
    render(<PostComments postId="post-1" currentUserId="viewer-1" open onOpenChange={vi.fn()} />);

    await screen.findByText("Original");
    fireEvent.click(screen.getByRole("button", { name: "Edit comment by Viewer" }));
    fireEvent.change(screen.getByLabelText("Edit comment"), { target: { value: "Draft edit" } });
    fireEvent.click(screen.getByRole("button", { name: "Save comment" }));
    expect(await screen.findByText("Canonical edit")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Delete comment by Viewer" }));
    const confirmation = await screen.findByRole("alertdialog");
    const remove = within(confirmation).getByRole("button", { name: "Delete comment" });
    fireEvent.click(remove);
    fireEvent.click(remove);
    expect(mocks.deletePostComment).toHaveBeenCalledTimes(1);
    expect(remove).toBeDisabled();
    releaseDelete();
    await waitFor(() => expect(screen.queryByText("Canonical edit")).not.toBeInTheDocument());
  });

  it("uses a bottom drawer on mobile", async () => {
    setViewport(true);
    render(<PostComments postId="post-1" currentUserId="viewer-1" open onOpenChange={vi.fn()} />);

    await waitFor(() => expect(document.querySelector('[data-slot="drawer-content"]')).toBeInTheDocument());
  });
});
