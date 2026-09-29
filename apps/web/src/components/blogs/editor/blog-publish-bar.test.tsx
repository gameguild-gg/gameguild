import "@testing-library/jest-dom/vitest";
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { BlogPublishBar } from "./blog-publish-bar";
import type { BlogPostAuthorView } from "@/lib/blogs/queries";

vi.mock("@/lib/blogs/actions", () => ({
  publish: vi.fn(),
  unpublish: vi.fn(),
  deletePost: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), refresh: vi.fn() }),
}));

const post = (overrides: Partial<BlogPostAuthorView> = {}): BlogPostAuthorView => ({
  id: "post-1",
  primaryAuthorId: "user-primary",
  title: "Hello",
  slug: "hello",
  excerpt: null,
  content: "",
  jsonBody: null,
  format: "Markdown",
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
  revision: 1,
  updatedAt: "2026-01-01T00:00:00Z",
  ...overrides,
});

describe("BlogPublishBar role gating", () => {
  it("enables publish/unpublish/delete for the primary author", () => {
    render(
      <BlogPublishBar
        post={post()}
        viewerUserId="user-primary"
        onChanged={() => undefined}
        onDeleted={() => undefined}
      />,
    );

    expect(screen.getByRole("button", { name: "Publish post" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Delete post" })).toBeEnabled();
  });

  it("renders publish disabled with tooltip for a non-primary co-author", () => {
    render(
      <BlogPublishBar
        post={post({ status: "Published" })}
        viewerUserId="user-coauthor"
        onChanged={() => undefined}
        onDeleted={() => undefined}
      />,
    );

    const publish = screen.getByRole("button", { name: "Publish (primary author only)" });
    expect(publish).toBeDisabled();
    expect(screen.getByText("Unpublish")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete post" })).not.toBeInTheDocument();
    expect(screen.getByTestId("blog-status")).toHaveTextContent("Published");
  });

  it("shows the draft status badge", () => {
    render(
      <BlogPublishBar
        post={post()}
        viewerUserId="user-primary"
        onChanged={() => undefined}
        onDeleted={() => undefined}
      />,
    );

    expect(screen.getByTestId("blog-status")).toHaveTextContent("Draft");
  });
});
