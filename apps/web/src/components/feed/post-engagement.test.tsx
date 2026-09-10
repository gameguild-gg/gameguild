import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  followCreator: vi.fn(),
  savePost: vi.fn(),
  setPostReaction: vi.fn(),
  sharePost: vi.fn(),
}));

vi.mock("@/lib/feed/actions", () => ({
  followCreator: mocks.followCreator,
  savePost: mocks.savePost,
  setPostReaction: mocks.setPostReaction,
  sharePost: mocks.sharePost,
}));

import { PostAuthorFollow, PostEngagement } from "./post-engagement";

describe("PostEngagement", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.history.replaceState({}, "", "/pt-BR/social");
    mocks.setPostReaction.mockResolvedValue({ type: "Like", reactionsCount: 3 });
    mocks.savePost.mockResolvedValue({ postId: "post-1", isSaved: true });
    mocks.followCreator.mockResolvedValue({ userId: "author-1", isFollowing: true });
    mocks.sharePost.mockResolvedValue({ postId: "post-1", shared: true });
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText: vi.fn().mockResolvedValue(undefined) },
    });
    Object.defineProperty(navigator, "share", {
      configurable: true,
      value: undefined,
    });
  });

  afterEach(cleanup);

  it("reconciles reaction and save controls with authoritative responses", async () => {
    mocks.setPostReaction.mockResolvedValueOnce({ type: "Love", reactionsCount: 9 });
    mocks.savePost.mockResolvedValueOnce({ postId: "post-1", isSaved: false });
    render(
      <PostEngagement
        postId="post-1"
        authorName="Ada"
        initialReaction={null}
        initialReactionCount={2}
        initialSaved={false}
        commentCount={4}
        onOpenComments={vi.fn()}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "React to post" }));
    await waitFor(() => expect(screen.getByRole("button", { name: "Remove Love reaction" })).toHaveTextContent("9"));

    fireEvent.click(screen.getByRole("button", { name: "Save post" }));
    await waitFor(() => expect(screen.getByRole("button", { name: "Save post" })).toHaveAttribute("aria-pressed", "false"));
  });

  it("rolls a failed reaction back visibly and ignores only duplicate reaction clicks", async () => {
    let rejectReaction!: (reason: Error) => void;
    mocks.setPostReaction.mockImplementationOnce(
      () => new Promise((_resolve, reject) => { rejectReaction = reject; }),
    );
    render(
      <PostEngagement
        postId="post-1"
        authorName="Ada"
        initialReaction={null}
        initialReactionCount={2}
        initialSaved={false}
        commentCount={0}
        onOpenComments={vi.fn()}
      />,
    );

    const reaction = screen.getByRole("button", { name: "React to post" });
    fireEvent.click(reaction);
    fireEvent.click(reaction);
    fireEvent.click(screen.getByRole("button", { name: "Save post" }));

    expect(mocks.setPostReaction).toHaveBeenCalledTimes(1);
    await waitFor(() => expect(mocks.savePost).toHaveBeenCalledTimes(1));
    rejectReaction(new Error("Reaction is offline"));
    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("Reaction is offline"));
    expect(screen.getByRole("button", { name: "React to post" })).toHaveAttribute("aria-pressed", "false");
  });

  it("copies a locale-aware URL when native sharing rejects", async () => {
    Object.defineProperty(navigator, "share", {
      configurable: true,
      value: vi.fn().mockRejectedValue(new Error("dismissed")),
    });
    render(
      <PostEngagement
        postId="post-1"
        authorName="Ada"
        initialReaction={null}
        initialReactionCount={0}
        initialSaved={false}
        commentCount={0}
        onOpenComments={vi.fn()}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Share post" }));

    await waitFor(() => expect(navigator.clipboard.writeText).toHaveBeenCalledWith(
      `${window.location.origin}/pt-BR/social/posts/post-1`,
    ));
    expect(mocks.sharePost).toHaveBeenCalledWith("post-1");
  });

  it("keeps save retryable after a visible rollback", async () => {
    mocks.savePost.mockRejectedValueOnce(new Error("Save unavailable"));
    render(
      <PostEngagement
        postId="post-1"
        authorName="Ada"
        initialReaction={null}
        initialReactionCount={0}
        initialSaved={false}
        commentCount={0}
        onOpenComments={vi.fn()}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Save post" }));
    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("Save unavailable"));
    expect(screen.getByRole("button", { name: "Save post" })).toHaveAttribute("aria-pressed", "false");

    fireEvent.click(screen.getByRole("button", { name: "Save post" }));
    await waitFor(() => expect(mocks.savePost).toHaveBeenCalledTimes(2));
  });

  it("reconciles author follow, rolls failure back and hides self-follow", async () => {
    let releaseFollow!: (value: { userId: string; isFollowing: boolean }) => void;
    mocks.followCreator.mockImplementationOnce(() => new Promise((resolve) => { releaseFollow = resolve; }));
    const view = render(
      <PostAuthorFollow
        authorId="author-1"
        authorName="Ada"
        currentUserId="viewer-1"
        initialFollowing={false}
      />,
    );

    const follow = screen.getByRole("button", { name: "Follow Ada" });
    fireEvent.click(follow);
    fireEvent.click(follow);
    expect(mocks.followCreator).toHaveBeenCalledTimes(1);
    releaseFollow({ userId: "author-1", isFollowing: true });
    await waitFor(() => expect(screen.getByRole("button", { name: "Unfollow Ada" })).toHaveAttribute("aria-pressed", "true"));

    mocks.followCreator.mockResolvedValueOnce({ userId: "author-1", isFollowing: false });
    fireEvent.click(screen.getByRole("button", { name: "Unfollow Ada" }));
    await waitFor(() => expect(screen.getByRole("button", { name: "Follow Ada" })).toHaveAttribute("aria-pressed", "false"));

    mocks.followCreator.mockRejectedValueOnce(new Error("Follow unavailable"));
    fireEvent.click(screen.getByRole("button", { name: "Follow Ada" }));
    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("Follow unavailable"));
    expect(screen.getByRole("button", { name: "Follow Ada" })).toHaveAttribute("aria-pressed", "false");

    view.rerender(
      <PostAuthorFollow
        authorId="author-1"
        authorName="Ada"
        currentUserId="author-1"
        initialFollowing={false}
      />,
    );
    expect(screen.queryByRole("button", { name: /follow ada/i })).not.toBeInTheDocument();
  });
});
