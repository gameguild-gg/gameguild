import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { CourseContent, CourseDetails } from "@/lib/learning/types";
import {
  deriveCourseLaunchSummary,
  formatDurationLabel,
  getCourseStructureMetrics,
} from "./course-launch";

const course = {
  title: "Production Game AI",
  description: "Build and ship a complete game AI system.",
  slug: "production-game-ai",
  thumbnail: "cover.png",
  status: "draft",
  visibility: "private",
  enrollmentStatus: "Open",
  enrollmentDeadline: null,
  isEnrollmentOpen: true,
} as CourseDetails;

const completeContent = {
  total: 3,
  items: [
    { id: "module", parentId: null, duration: null },
    { id: "lesson-1", parentId: "module", duration: 50 },
    { id: "lesson-2", parentId: "module", duration: 25 },
  ],
} as CourseContent;

const emptyContent = { total: 0, items: [] } as CourseContent;

describe("course launch readiness", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-09-15T12:00:00.000Z"));
  });

  afterEach(() => vi.useRealTimers());

  it("computes modules, lessons, and nullable duration values", () => {
    expect(getCourseStructureMetrics(completeContent)).toEqual({
      modules: 1,
      lessons: 2,
      totalDurationMinutes: 75,
    });
  });

  it("keeps an incomplete draft hidden and ignores an invalid deadline", () => {
    const summary = deriveCourseLaunchSummary(
      {
        ...course,
        title: "",
        description: "",
        slug: "",
        thumbnail: null,
        enrollmentDeadline: "not-a-date",
      },
      emptyContent,
    );

    expect(summary).toMatchObject({
      storefrontState: "hidden",
      academyState: "hidden",
      readinessState: "incomplete",
      enrollmentDeadlinePassed: false,
    });
    expect(summary.blockers).toEqual([
      "Set a clear course title",
      "Add a course description",
      "Set the course slug",
      "Upload a cover image",
      "Create at least one module",
      "Add at least one lesson",
    ]);
  });

  it("treats missing status and visibility values as unpublished defaults", () => {
    const summary = deriveCourseLaunchSummary(
      {
        ...course,
        status: undefined,
        visibility: undefined,
      } as unknown as CourseDetails,
      completeContent,
    );

    expect(summary).toMatchObject({
      storefrontState: "hidden",
      academyState: "hidden",
      readinessState: "academy-ready",
    });
  });

  it("publishes an unlisted complete course as a live teaser", () => {
    expect(
      deriveCourseLaunchSummary(
        { ...course, status: " Published ", visibility: " Unlisted " },
        completeContent,
      ),
    ).toMatchObject({
      storefrontState: "teaser",
      academyState: "live",
      readinessState: "live",
      blockers: [],
    });
  });

  it("opens enrollment only while every enrollment gate is open", () => {
    expect(
      deriveCourseLaunchSummary(
        {
          ...course,
          status: "published",
          visibility: "public",
          enrollmentStatus: "Open",
          isEnrollmentOpen: true,
          enrollmentDeadline: "2026-09-16T12:00:00.000Z",
        },
        completeContent,
      ).storefrontState,
    ).toBe("enrollment-open");

    for (const overrides of [
      { enrollmentStatus: "closed" },
      { isEnrollmentOpen: false },
      { enrollmentDeadline: "2026-09-15T12:00:00.000Z" },
    ]) {
      const summary = deriveCourseLaunchSummary(
        {
          ...course,
          status: "published",
          visibility: "public",
          ...overrides,
        },
        completeContent,
      );
      expect(summary.storefrontState).toBe("enrollment-closed");
    }
  });

  it("keeps a private published academy live without exposing storefront access", () => {
    expect(
      deriveCourseLaunchSummary(
        { ...course, status: "published", visibility: "private" },
        completeContent,
      ),
    ).toMatchObject({
      storefrontState: "hidden",
      academyState: "live",
      readinessState: "academy-ready",
    });
  });

  it("distinguishes storefront readiness from a scheduled published academy", () => {
    const storefrontOnly = {
      total: 1,
      items: [{ id: "module", parentId: null, duration: 10 }],
    } as CourseContent;
    expect(deriveCourseLaunchSummary(course, storefrontOnly)).toMatchObject({
      readinessState: "storefront-ready",
      academyState: "hidden",
    });
    expect(
      deriveCourseLaunchSummary(
        { ...course, status: "published", visibility: "public" },
        storefrontOnly,
      ),
    ).toMatchObject({
      readinessState: "storefront-ready",
      academyState: "scheduled",
    });
  });

  it("formats minute and hour durations without redundant zero minutes", () => {
    expect(formatDurationLabel(0)).toBe("0m");
    expect(formatDurationLabel(59)).toBe("59m");
    expect(formatDurationLabel(60)).toBe("1h");
    expect(formatDurationLabel(125)).toBe("2h 5m");
  });
});
