import "@testing-library/jest-dom/vitest";
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("@/i18n/navigation", () => ({
  Link: ({ children, ...props }: React.ComponentProps<"a">) => <a {...props}>{children}</a>,
  usePathname: () => "/",
}));

import { SocialFeedTabs } from "./social-feed-tabs";
import { SocialRail } from "./social-rail";
import { SOCIAL_NAVIGATION } from "./social-sidebar";

describe("social navigation", () => {
  afterEach(cleanup);

  it("does not expose Messages and keeps Saved on the real saved stream", () => {
    expect(SOCIAL_NAVIGATION.some((item) => item.label === "Messages")).toBe(false);
    expect(SOCIAL_NAVIGATION.find((item) => item.label === "Saved")?.href).toBe(
      "/?tab=saved",
    );
  });

  it("gives For You, Following, Community, and Saved distinct locale-aware destinations", () => {
    render(<SocialFeedTabs active="community" />);
    const destinations = [
      ["For you", "/"],
      ["Following", "/?tab=following"],
      ["Community", "/?tab=community"],
      ["Saved", "/?tab=saved"],
    ];

    for (const [name, href] of destinations) {
      expect(screen.getByRole("link", { name })).toHaveAttribute("href", href);
    }
    expect(new Set(destinations.map(([, href]) => href)).size).toBe(4);
  });

  it("builds the real public creator profile destination", () => {
    render(
      <SocialRail
        currentProfile={{
          id: "profile-1",
          userId: "user-1",
          handle: "lin",
          displayName: "Lin",
          avatarUrl: null,
          bannerUrl: null,
          bio: null,
          headline: null,
          location: null,
          timeZone: null,
          websiteUrl: null,
          availabilityStatus: null,
          isVerified: false,
          projectCount: 0,
          postCount: 0,
          followerCount: 0,
          followingCount: 0,
          isFollowing: false,
        }}
        sessions={[]}
        creators={[]}
        tags={[]}
        activeTab="foryou"
      />,
    );
    expect(screen.getByRole("link", { name: "View profile" })).toHaveAttribute(
      "href",
      "/social/profiles/lin",
    );
  });
});
