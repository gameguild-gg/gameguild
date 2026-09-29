import "@testing-library/jest-dom/vitest";
import { render, screen } from "@testing-library/react";
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
  BlogBodyEditor: ({ format }: { format: string }) => (
    <div data-testid="body-editor" data-format={format} />
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
