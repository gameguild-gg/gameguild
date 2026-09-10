import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({ followCreator: vi.fn() }));
vi.mock("@/lib/feed/actions", () => mocks);
vi.mock("next/image", () => ({ default: () => <span data-testid="next-image" /> }));
vi.mock("@/i18n/navigation", () => ({ Link: ({ children, ...props }: React.ComponentProps<"a">) => <a {...props}>{children}</a> }));

import { SocialProfileView } from "./social-profile";
import type { SocialProfile, SocialProfilePost, SocialProfileProject } from "@/lib/feed/contracts";

const profile: SocialProfile = {
  id: "profile-1",
  userId: "user-2",
  handle: "lin",
  displayName: "Lin Creator",
  avatarUrl: null,
  bannerUrl: null,
  bio: "Building thoughtful games.",
  headline: "Independent designer",
  location: "Toronto",
  timeZone: "America/Toronto",
  websiteUrl: "https://example.com",
  availabilityStatus: "AvailableForCollaboration",
  isVerified: true,
  projectCount: 3,
  postCount: 12,
  followerCount: 42,
  followingCount: 8,
  isFollowing: false,
};

const posts: SocialProfilePost[] = [
  {
    id: "post-1",
    content: "A persisted launch update",
    mediaUrl: null,
    mediaType: null,
    createdAt: "2026-09-10T00:00:00Z",
  },
];

const projects: SocialProfileProject[] = [
  {
    id: "project-1",
    title: "Skybound",
    slug: "skybound",
    shortDescription: "A persisted public project",
    imageUrl: null,
    publishedAt: "2026-09-09T00:00:00Z",
  },
];

describe("SocialProfileView", () => {
  afterEach(cleanup);

  it("renders real profile metrics and persists follow state", async () => {
    mocks.followCreator.mockResolvedValue({ userId: "user-2", isFollowing: true });
    render(<SocialProfileView profile={profile} currentUserId="user-1" posts={posts} projects={projects} />);

    expect(screen.getByText("42")).toBeInTheDocument();
    expect(screen.getByText("Building thoughtful games.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Follow Lin Creator" }));

    await waitFor(() => expect(mocks.followCreator).toHaveBeenCalledWith("user-2", true));
    expect(screen.getByRole("button", { name: "Unfollow Lin Creator" })).toBeInTheDocument();
    expect(screen.getByText("43")).toBeInTheDocument();
  });

  it("does not render a self-follow action", () => {
    render(<SocialProfileView profile={profile} currentUserId="user-2" posts={posts} projects={projects} />);
    expect(screen.queryByRole("button", { name: /follow lin creator/i })).not.toBeInTheDocument();
  });

  it("renders persisted posts and projects with real locale-aware routes", () => {
    render(<SocialProfileView profile={profile} currentUserId="user-1" posts={posts} projects={projects} />);

    expect(screen.getByText("A persisted launch update")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /skybound/i })).toHaveAttribute(
      "href",
      "/projects/skybound",
    );
    expect(screen.getByRole("link", { name: /open post/i })).toHaveAttribute(
      "href",
      "/social/posts/post-1",
    );
  });

  it("renders truthful collection empty states", () => {
    render(<SocialProfileView profile={profile} currentUserId="user-1" posts={[]} projects={[]} />);

    expect(screen.getByText("No public posts yet.")).toBeInTheDocument();
    expect(screen.getByText("No public projects yet.")).toBeInTheDocument();
  });

  it("renders a recoverable collection error without fake cards", () => {
    render(
      <SocialProfileView
        profile={profile}
        currentUserId="user-1"
        posts={[]}
        projects={[]}
        collectionsError="Public work could not be loaded."
      />,
    );

    expect(screen.getByRole("alert")).toHaveTextContent("Public work could not be loaded.");
    expect(screen.queryByText("Demo project")).not.toBeInTheDocument();
  });

  it("reconciles follow state with the authoritative response and rolls back failures", async () => {
    mocks.followCreator.mockResolvedValueOnce({ userId: "user-2", isFollowing: false });
    render(<SocialProfileView profile={profile} currentUserId="user-1" posts={[]} projects={[]} />);

    fireEvent.click(screen.getByRole("button", { name: "Follow Lin Creator" }));
    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Follow Lin Creator" })).toHaveAttribute(
        "aria-pressed",
        "false",
      ),
    );
    expect(screen.getByText("42")).toBeInTheDocument();

    mocks.followCreator.mockRejectedValueOnce(new Error("Follow unavailable"));
    fireEvent.click(screen.getByRole("button", { name: "Follow Lin Creator" }));
    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Follow Lin Creator" })).toHaveAttribute(
        "aria-pressed",
        "false",
      ),
    );
    expect(screen.getByText("42")).toBeInTheDocument();
  });
});
