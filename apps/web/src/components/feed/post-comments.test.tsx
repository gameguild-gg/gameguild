import "@testing-library/jest-dom/vitest";
import { act, cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  createPostComment: vi.fn(),
  deletePostComment: vi.fn(),
  loadPostCommentRepliesPageAction: vi.fn(),
  loadPostCommentsPageAction: vi.fn(),
  updatePostComment: vi.fn(),
}));

vi.mock("@/lib/feed/actions", () => mocks);
vi.mock("next/image", () => ({ default: () => <span data-testid="next-image" /> }));

const widgetProps = vi.fn();
vi.mock("@/components/antibot/turnstile-widget", () => ({
  TurnstileWidget: (props: { onToken: (token: string | null) => void; id?: string }) => {
    widgetProps(props);
    return null;
  },
}));

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
    mocks.loadPostCommentRepliesPageAction.mockResolvedValue({ items: [], nextSkip: null });
  });
  afterEach(cleanup);

  it("paginates comments and requests the next parent-scoped reply page with merge dedupe", async () => {
    const root = comment("root-1", "Root");
    root.replies = [comment("reply-1", "Reply one", root.id), comment("reply-2", "Reply two", root.id)];
    mocks.loadPostCommentRepliesPageAction.mockResolvedValueOnce({
      items: [comment("reply-2", "Reply two", root.id), comment("reply-3", "Reply three", root.id)],
      nextSkip: null,
    });
    mocks.loadPostCommentsPageAction
      .mockResolvedValueOnce({ items: [root, comment("root-2", "Second")], nextSkip: 2 })
      .mockResolvedValueOnce({ items: [comment("root-3", "Third")], nextSkip: null });
    render(<PostComments postId="post-1" currentUserId="viewer-1" open onOpenChange={vi.fn()} />);

    expect(await screen.findByText("Reply one")).toBeInTheDocument();
    expect(screen.queryByText("Reply three")).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Load more replies to Author root-1" }));
    await waitFor(() => expect(mocks.loadPostCommentRepliesPageAction).toHaveBeenCalledWith("post-1", "root-1", 2, 2));
    expect(await screen.findByText("Reply three")).toBeInTheDocument();
    expect(screen.getAllByText("Reply two")).toHaveLength(1);

    fireEvent.click(screen.getByRole("button", { name: "Load more comments" }));
    await waitFor(() => expect(mocks.loadPostCommentsPageAction).toHaveBeenLastCalledWith("post-1", 2, 10));
    expect(await screen.findByText("Third")).toBeInTheDocument();
  });

  it("renders comments inline on desktop and only uses a dialog surface on mobile", async () => {
    const desktop = render(<PostComments postId="post-1" currentUserId="viewer-1" open onOpenChange={vi.fn()} />);

    expect(await screen.findByRole("region", { name: "Comments" })).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

    desktop.unmount();
    setViewport(true);
    render(<PostComments postId="post-1" currentUserId="viewer-1" open onOpenChange={vi.fn()} />);

    expect(await screen.findByRole("dialog", { name: "Comments" })).toBeInTheDocument();
    expect(screen.queryByRole("region", { name: "Comments" })).not.toBeInTheDocument();
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

    await waitFor(() => expect(mocks.createPostComment).toHaveBeenCalledWith("post-1", expect.objectContaining({
      content: "My draft",
      parentCommentId: "root-1",
    })));
    expect(await screen.findByText("Canonical reply")).toBeInTheDocument();
  });

  it("sends the turnstile token and resets it to null after a failed submit", async () => {
    mocks.loadPostCommentsPageAction.mockResolvedValueOnce({ items: [], nextSkip: null });
    mocks.createPostComment
      .mockRejectedValueOnce(new Error("Bot check failed. Please refresh and try again."))
      .mockResolvedValueOnce(comment("c-new", "Retry"));
    render(<PostComments postId="post-1" currentUserId="viewer-1" open onOpenChange={vi.fn()} />);

    await screen.findByPlaceholderText("Add a comment…");
    await waitFor(() => expect(widgetProps).toHaveBeenCalled());
    const onToken = (widgetProps.mock.calls.at(-1)?.[0] as { onToken: (token: string | null) => void }).onToken;
    act(() => onToken("tok-1"));

    fireEvent.change(screen.getByPlaceholderText("Add a comment…"), { target: { value: "First try" } });
    fireEvent.click(screen.getByRole("button", { name: "Publish comment" }));
    await waitFor(() => expect(mocks.createPostComment).toHaveBeenCalledTimes(1));
    expect(mocks.createPostComment.mock.calls[0]?.[1]).toMatchObject({ turnstileToken: "tok-1" });
    expect(await screen.findByRole("alert")).toHaveTextContent(/bot check failed/i);

    // Single-use token: the failed submit consumed it, so the retry must send null.
    fireEvent.change(screen.getByPlaceholderText("Add a comment…"), { target: { value: "First try " } });
    fireEvent.click(screen.getByRole("button", { name: "Publish comment" }));
    await waitFor(() => expect(mocks.createPostComment).toHaveBeenCalledTimes(2));
    expect(mocks.createPostComment.mock.calls[1]?.[1]).toMatchObject({ turnstileToken: null });
  });

  it("clears the single-use turnstile token after a successful submit", async () => {
    mocks.loadPostCommentsPageAction.mockResolvedValueOnce({ items: [], nextSkip: null });
    mocks.createPostComment
      .mockResolvedValueOnce(comment("c-1", "First"))
      .mockResolvedValueOnce(comment("c-2", "Second"));
    render(<PostComments postId="post-1" currentUserId="viewer-1" open onOpenChange={vi.fn()} />);

    await screen.findByPlaceholderText("Add a comment…");
    await waitFor(() => expect(widgetProps).toHaveBeenCalled());
    const onToken = (widgetProps.mock.calls.at(-1)?.[0] as { onToken: (token: string | null) => void }).onToken;
    act(() => onToken("tok-1"));

    fireEvent.change(screen.getByPlaceholderText("Add a comment…"), { target: { value: "First" } });
    fireEvent.click(screen.getByRole("button", { name: "Publish comment" }));
    await waitFor(() => expect(mocks.createPostComment).toHaveBeenCalledTimes(1));
    expect(mocks.createPostComment.mock.calls[0]?.[1]).toMatchObject({ turnstileToken: "tok-1" });
    expect(await screen.findByText("First")).toBeInTheDocument();

    // Single-use token: the successful submit consumed it, so the next submit sends null.
    fireEvent.change(screen.getByPlaceholderText("Add a comment…"), { target: { value: "Second" } });
    fireEvent.click(screen.getByRole("button", { name: "Publish comment" }));
    await waitFor(() => expect(mocks.createPostComment).toHaveBeenCalledTimes(2));
    expect(mocks.createPostComment.mock.calls[1]?.[1]).toMatchObject({ turnstileToken: null });
  });

  it("keeps a token re-issued by the widget during a pending submit", async () => {
    mocks.loadPostCommentsPageAction.mockResolvedValueOnce({ items: [], nextSkip: null });
    let releaseFirst!: (value: ReturnType<typeof comment>) => void;
    mocks.createPostComment.mockImplementationOnce(
      () => new Promise((resolve) => { releaseFirst = resolve; }),
    );
    mocks.createPostComment.mockResolvedValueOnce(comment("c-new", "Retry"));
    render(<PostComments postId="post-1" currentUserId="viewer-1" open onOpenChange={vi.fn()} />);

    await screen.findByPlaceholderText("Add a comment…");
    await waitFor(() => expect(widgetProps).toHaveBeenCalled());
    const onToken = (widgetProps.mock.calls.at(-1)?.[0] as { onToken: (token: string | null) => void }).onToken;
    act(() => onToken("tok-1"));

    fireEvent.change(screen.getByPlaceholderText("Add a comment…"), { target: { value: "First try" } });
    fireEvent.click(screen.getByRole("button", { name: "Publish comment" }));
    await waitFor(() => expect(mocks.createPostComment).toHaveBeenCalledTimes(1));

    // The widget re-issues a fresh token while the submit is still in flight.
    act(() => onToken("tok-2"));
    act(() => releaseFirst(comment("c-1", "First")));
    await screen.findByText("First");

    fireEvent.change(screen.getByPlaceholderText("Add a comment…"), { target: { value: "Retry" } });
    fireEvent.click(screen.getByRole("button", { name: "Publish comment" }));
    await waitFor(() => expect(mocks.createPostComment).toHaveBeenCalledTimes(2));
    expect(mocks.createPostComment.mock.calls[1]?.[1]).toMatchObject({ turnstileToken: "tok-2" });
  });

  it("edits authoritatively and confirms a guarded delete", async () => {
    const own = comment("comment-1", "Original");
    mocks.loadPostCommentsPageAction.mockResolvedValueOnce({ items: [own], nextSkip: null });
    mocks.updatePostComment.mockResolvedValueOnce({ ...own, content: "Canonical edit", isEdited: true });
    let releaseDelete!: () => void;
    const onCommentCountReconciled = vi.fn();
    mocks.deletePostComment.mockImplementationOnce(() => new Promise((resolve) => {
      releaseDelete = () => resolve({ kind: "confirmed", postId: "post-1", commentId: own.id, commentsCount: 17 });
    }));
    render(<PostComments postId="post-1" currentUserId="viewer-1" open onOpenChange={vi.fn()} onCommentCountReconciled={onCommentCountReconciled} />);

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
    expect(onCommentCountReconciled).toHaveBeenCalledWith(17);
  });

  it("uses a bottom drawer on mobile", async () => {
    setViewport(true);
    render(<PostComments postId="post-1" currentUserId="viewer-1" open onOpenChange={vi.fn()} />);

    await waitFor(() => expect(document.querySelector('[data-slot="drawer-content"]')).toBeInTheDocument());
  });
});
