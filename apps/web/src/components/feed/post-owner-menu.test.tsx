import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  deleteSocialPost: vi.fn(),
  updateSocialPost: vi.fn(),
}));
vi.mock("@/lib/feed/actions", () => mocks);

import { PostOwnerMenu } from "./post-owner-menu";

describe("PostOwnerMenu", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.updateSocialPost.mockResolvedValue({ id: "post-1", content: "Canonical content" });
    mocks.deleteSocialPost.mockResolvedValue({ postId: "post-1", deleted: true });
  });
  afterEach(cleanup);

  it("hides owner actions without permissions", () => {
    render(
      <PostOwnerMenu
        postId="post-1"
        content="Original"
        canEdit={false}
        canDelete={false}
        onContentChange={vi.fn()}
        onDeleted={vi.fn()}
      />,
    );
    expect(screen.queryByRole("button", { name: "Post options" })).not.toBeInTheDocument();
  });

  it("edits once and applies the authoritative content", async () => {
    const onContentChange = vi.fn();
    let release!: (value: { id: string; content: string }) => void;
    mocks.updateSocialPost.mockImplementationOnce(() => new Promise((resolve) => { release = resolve; }));
    render(
      <PostOwnerMenu
        postId="post-1"
        content="Original"
        canEdit
        canDelete
        onContentChange={onContentChange}
        onDeleted={vi.fn()}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Post options" }));
    fireEvent.click(await screen.findByRole("menuitem", { name: "Edit post" }));
    fireEvent.change(screen.getByRole("textbox", { name: "Edit post" }), { target: { value: "Draft" } });
    const save = screen.getByRole("button", { name: "Save post changes" });
    fireEvent.click(save);
    fireEvent.click(save);

    expect(mocks.updateSocialPost).toHaveBeenCalledTimes(1);
    expect(mocks.updateSocialPost).toHaveBeenCalledWith("post-1", "Draft");
    release({ id: "post-1", content: "Canonical content" });
    await waitFor(() => expect(onContentChange).toHaveBeenCalledWith("Canonical content"));
  });

  it("requires confirmation and guards duplicate post deletion", async () => {
    const onDeleted = vi.fn();
    let release!: () => void;
    mocks.deleteSocialPost.mockImplementationOnce(() => new Promise((resolve) => {
      release = () => resolve({ postId: "post-1", deleted: true });
    }));
    render(
      <PostOwnerMenu
        postId="post-1"
        content="Original"
        canEdit
        canDelete
        onContentChange={vi.fn()}
        onDeleted={onDeleted}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Post options" }));
    fireEvent.click(await screen.findByRole("menuitem", { name: "Delete post" }));
    const confirmation = await screen.findByRole("alertdialog");
    const remove = within(confirmation).getByRole("button", { name: "Delete post" });
    fireEvent.click(remove);
    fireEvent.click(remove);

    expect(mocks.deleteSocialPost).toHaveBeenCalledTimes(1);
    expect(remove).toBeDisabled();
    release();
    await waitFor(() => expect(onDeleted).toHaveBeenCalledTimes(1));
  });

  it("keeps a failed edit visible and retryable", async () => {
    mocks.updateSocialPost.mockRejectedValueOnce(new Error("Edit unavailable"));
    render(
      <PostOwnerMenu
        postId="post-1"
        content="Original"
        canEdit
        canDelete={false}
        onContentChange={vi.fn()}
        onDeleted={vi.fn()}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Post options" }));
    fireEvent.click(await screen.findByRole("menuitem", { name: "Edit post" }));
    fireEvent.change(screen.getByRole("textbox", { name: "Edit post" }), { target: { value: "Draft" } });
    fireEvent.click(screen.getByRole("button", { name: "Save post changes" }));

    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("Edit unavailable"));
    expect(screen.getByRole("dialog", { name: "Edit post" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Edit post" })).toHaveValue("Draft");
  });
});
