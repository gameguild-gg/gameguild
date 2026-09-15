import { describe, expect, it } from "vitest";
import {
  buildDashboardCoursePath,
  getCourseAuthorHandle,
  getCourseLookupSlug,
  getCourseRouteParam,
  slugifyRoutePart,
} from "./course-route";

describe("course route helpers", () => {
  it("builds dashboard course params as course-slug-by-author", () => {
    expect(
      getCourseRouteParam({
        id: "course-1",
        slug: "ai-for-boss-encounters",
        creatorName: "Ada Lovelace",
      }),
    ).toBe("ai-for-boss-encounters-by-ada-lovelace");
  });

  it("extracts the canonical API slug from a slug-by-author route param", () => {
    expect(getCourseLookupSlug("ai-for-boss-encounters-by-ada-lovelace")).toBe(
      "ai-for-boss-encounters",
    );
  });

  it("keeps legacy plain slug params resolvable", () => {
    expect(getCourseLookupSlug("ai-for-boss-encounters")).toBe(
      "ai-for-boss-encounters",
    );
    expect(getCourseLookupSlug("  -by-orphan  ")).toBe("-by-orphan");
  });

  it("normalizes and caps route segments", () => {
    expect(slugifyRoutePart("  Ada & Grace!  ")).toBe("ada-grace");
    expect(slugifyRoutePart("A".repeat(100))).toHaveLength(80);
  });

  it("selects the first usable author identity with safe fallbacks", () => {
    expect(getCourseAuthorHandle({ creatorHandle: "@Ada_Dev" })).toBe(
      "ada-dev",
    );
    expect(
      getCourseAuthorHandle({
        creatorHandle: "@@@",
        creatorName: "Grace Hopper",
      }),
    ).toBe("grace-hopper");
    expect(
      getCourseAuthorHandle({
        creatorName: "@@@",
        creatorEmail: "builder@example.com",
      }),
    ).toBe("builder");
    expect(
      getCourseAuthorHandle({
        creatorEmail: "@example.com",
        creatorId: " Creator 123 ",
      }),
    ).toBe("creator-123");
    expect(getCourseAuthorHandle({ creatorId: "@@@" })).toBe("gameguild");
    expect(getCourseAuthorHandle({})).toBe("gameguild");
  });

  it("uses a legacy id when a course has no slug", () => {
    expect(getCourseRouteParam({ id: " course-id ", slug: " " })).toBe(
      "course-id",
    );
    expect(getCourseRouteParam({ id: null })).toBe("");
  });

  it("builds workspace and console course paths with normalized segments", () => {
    expect(buildDashboardCoursePath("course by id")).toBe(
      "/workspace/learning/courses/course%20by%20id",
    );
    expect(
      buildDashboardCoursePath(
        { slug: "game-ai", creatorHandle: "ada" },
        "/settings/access",
        "console",
      ),
    ).toBe("/console/learning/courses/game-ai-by-ada/settings/access");
  });
});
