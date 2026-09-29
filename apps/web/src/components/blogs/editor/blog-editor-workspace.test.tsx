import "@testing-library/jest-dom/vitest";
import userEvent from "@testing-library/user-event";
import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { BlogEditorWorkspace } from "./blog-editor-workspace";
import type { BlogPostAuthorView } from "@/lib/blogs/queries";

const mocks = vi.hoisted(() => ({
  updateDraft: vi.fn(),
  changeSlug: vi.fn(),
}));

vi.mock("@/lib/blogs/actions", () => ({
  updateDraft: mocks.updateDraft,
  changeSlug: mocks.changeSlug,
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), refresh: vi.fn() }),
}));

vi.mock("./blog-body-editor", () => ({
  BlogBodyEditor: ({ format, onMarkdownChange }: { format: string; onMarkdownChange: (value: string) => void }) => (
    <div data-testid="body-editor" data-format={format}>
      <button type="button" onClick={() => onMarkdownChange("Updated content")}>Edit body</button>
    </div>
  ),
}));
vi.mock("./blog-publish-bar", () => ({
  BlogPublishBar: () => <div data-testid="publish-bar" />,
}));
vi.mock("./blog-settings-panel", () => ({
  BlogSettingsPanel: () => <div data-testid="settings-panel" />,
}));
vi.mock("./blog-coauthor-manager", () => ({
  BlogCoauthorManager: () => <div data-testid="coauthor-manager" />,
}));
vi.mock("./blog-copilot-panel", () => ({
  BlogCopilotPanel: ({ postRevision }: { postRevision: number }) => (
    <div data-testid="copilot-panel" data-revision={postRevision} />
  ),
}));

const post = (overrides: Partial<BlogPostAuthorView> = {}): BlogPostAuthorView => ({
  id: "post-1",
  primaryAuthorId: "user-primary",
  title: "Hello",
  slug: "hello",
  excerpt: null,
  content: "",
  jsonBody: null,
  format: "Lexical",
  tags: null,
  metaTitle: null,
  metaDescription: null,
  ogImageUrl: null,
  canonicalUrlOverride: null,
  twitterCard: null,
  structuredDataOverride: null,
  allowComments: true,
  status: "Draft",
  publishedAt: null,
  revision: 7,
  updatedAt: "2026-01-01T00:00:00Z",
  ...overrides,
});

describe("BlogEditorWorkspace format lock", () => {
  it("renders no format switcher on the edit page and keeps the stored format", () => {
    render(
      <BlogEditorWorkspace
        post={post()}
        viewerUserId="user-primary"
        primaryAuthorHandle="alice"
        coauthors={[
          { userId: "user-primary", handle: "alice", displayName: null, isPrimary: true },
        ]}
      />,
    );

    expect(screen.queryByRole("radiogroup", { name: "Post format" })).not.toBeInTheDocument();
    expect(screen.queryByRole("radio", { name: /Markdown/ })).not.toBeInTheDocument();
    expect(screen.getByTestId("body-editor")).toHaveAttribute("data-format", "Lexical");
    expect(screen.getByText("Lexical")).toBeInTheDocument();
  });
});

describe("BlogEditorWorkspace slug transform", () => {
  it("slugifies the URL slug input live like lesson authoring", async () => {
    const user = userEvent.setup();
    mocks.changeSlug.mockResolvedValue({
      success: true,
      data: { ...post(), slug: "xpto-with-spaces" },
    });
    render(
      <BlogEditorWorkspace
        post={post()}
        viewerUserId="user-primary"
        primaryAuthorHandle="alice"
        coauthors={[
          { userId: "user-primary", handle: "alice", displayName: null, isPrimary: true },
        ]}
      />,
    );

    await user.click(screen.getByRole("button", { name: "hello" }));
    const slugInput = screen.getByRole("textbox", { name: "URL slug" });
    await user.clear(slugInput);
    await user.type(slugInput, "Xpto With Spaces");

    expect(slugInput).toHaveValue("xpto-with-spaces");
  });
});

describe("BlogEditorWorkspace copilot mount", () => {
  it("shows a Copilot tab and renders the copilot panel with the current revision", async () => {
    const user = userEvent.setup();
    render(
      <BlogEditorWorkspace
        post={post()}
        viewerUserId="user-primary"
        primaryAuthorHandle="alice"
        coauthors={[
          { userId: "user-primary", handle: "alice", displayName: null, isPrimary: true },
        ]}
      />,
    );

    expect(screen.getByRole("button", { name: /Copilot/ })).toBeInTheDocument();
    expect(screen.queryByTestId("copilot-panel")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /Copilot/ }));

    expect(screen.getByTestId("copilot-panel")).toHaveAttribute("data-revision", "7");
  });
});

describe("BlogEditorWorkspace autosave", () => {
  it("persists the current draft once and advances the visible revision", async () => {
    const user = userEvent.setup();
    mocks.updateDraft.mockReset();
    mocks.updateDraft.mockResolvedValue({
      success: true,
      data: post({ content: "Updated content", format: "Markdown", revision: 8 }),
    });

    render(
      <BlogEditorWorkspace
        post={post({ format: "Markdown" })}
        viewerUserId="user-primary"
        primaryAuthorHandle="alice"
        coauthors={[
          { userId: "user-primary", handle: "alice", displayName: null, isPrimary: true },
        ]}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Edit body" }));

    await waitFor(() => expect(mocks.updateDraft).toHaveBeenCalledTimes(1), { timeout: 3500 });
    expect(mocks.updateDraft).toHaveBeenCalledWith("post-1", expect.objectContaining({
      content: "Updated content",
      revision: 7,
    }));
    expect(await screen.findByText("Rev 8")).toBeInTheDocument();
  });
});
