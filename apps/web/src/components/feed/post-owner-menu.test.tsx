import "@testing-library/jest-dom/vitest";
import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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
    const user = userEvent.setup();
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

    await user.click(screen.getByRole("button", { name: "Post options" }));
    await user.click(await screen.findByRole("menuitem", { name: "Edit post" }));
    const editor = screen.getByRole("textbox", { name: "Edit post" });
    await user.clear(editor);
    await user.type(editor, "Draft");
    const save = screen.getByRole("button", { name: "Save post changes" });
    await user.click(save);
    await user.click(save);

    expect(mocks.updateSocialPost).toHaveBeenCalledTimes(1);
    expect(mocks.updateSocialPost).toHaveBeenCalledWith("post-1", "Draft");
    release({ id: "post-1", content: "Canonical content" });
    await waitFor(() => expect(onContentChange).toHaveBeenCalledWith("Canonical content"));
  });

  it("requires confirmation and guards duplicate post deletion", async () => {
    const user = userEvent.setup();
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

    await user.click(screen.getByRole("button", { name: "Post options" }));
    await user.click(await screen.findByRole("menuitem", { name: "Delete post" }));
    const confirmation = await screen.findByRole("alertdialog");
    const remove = within(confirmation).getByRole("button", { name: "Delete post" });
    await user.click(remove);
    await user.click(remove);

    expect(mocks.deleteSocialPost).toHaveBeenCalledTimes(1);
    expect(remove).toBeDisabled();
    release();
    await waitFor(() => expect(onDeleted).toHaveBeenCalledTimes(1));
  });

  it("keeps a failed edit visible and retryable", async () => {
    const user = userEvent.setup();
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

    await user.click(screen.getByRole("button", { name: "Post options" }));
    await user.click(await screen.findByRole("menuitem", { name: "Edit post" }));
    const editor = screen.getByRole("textbox", { name: "Edit post" });
    await user.clear(editor);
    await user.type(editor, "Draft");
    await user.click(screen.getByRole("button", { name: "Save post changes" }));

    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("Edit unavailable"));
    expect(screen.getByRole("dialog", { name: "Edit post" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Edit post" })).toHaveValue("Draft");
  });
});
