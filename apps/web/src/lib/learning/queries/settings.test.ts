import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  getCourse: vi.fn(),
}));

vi.mock("./course", () => ({
  getCourse: mocks.getCourse,
}));

import {
  getCourseAccessSettings,
  getCourseIntegrationSettings,
  getCourseNotificationSettings,
} from "./settings";

const baseCourse = {
  id: "course-1",
  title: "Production Art",
  updatedAt: "2026-07-10T10:00:00.000Z",
  videoShowcaseUrl: null,
  enrollmentStatus: "Open",
  visibility: "public",
  features: {
    hasClasses: true,
    hasAssessments: true,
    hasCertificate: true,
    hasDiscussions: true,
  },
};

describe("course operational settings queries", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("hydrates persisted notification settings from course metadata", async () => {
    mocks.getCourse.mockResolvedValue({
      ...baseCourse,
      metadata: JSON.stringify({
        notificationSettings: {
          studentNotifications: {
            courseUpdates: false,
            classReminders: [120, 15],
          },
          instructorNotifications: {
            lowRating: true,
            lowRatingThreshold: 2,
          },
          templates: [
            {
              id: "custom-template",
              type: "course-update",
              subject: "Studio update",
              enabled: false,
            },
          ],
        },
      }),
    });

    const settings = await getCourseNotificationSettings("course-1");

    expect(settings?.studentNotifications.courseUpdates).toBe(false);
    expect(settings?.studentNotifications.enrollmentConfirmation).toBe(true);
    expect(settings?.studentNotifications.classReminders).toEqual([120, 15]);
    expect(settings?.instructorNotifications.lowRatingThreshold).toBe(2);
    expect(settings?.templates).toEqual([
      {
        id: "custom-template",
        type: "course-update",
        subject: "Studio update",
        enabled: false,
      },
    ]);
  });

  it("hydrates persisted integrations and webhooks from course metadata", async () => {
    mocks.getCourse.mockResolvedValue({
      ...baseCourse,
      metadata: JSON.stringify({
        integrationSettings: {
          integrations: [
            {
              id: "discord-community",
              type: "discord",
              name: "Class Discord",
              enabled: true,
              status: "connected",
              config: { inviteUrl: "https://discord.gg/gameguild" },
            },
          ],
          webhooks: [
            {
              id: "webhook-1",
              url: "https://example.com/course-events",
              events: ["enrollment.created"],
              enabled: true,
            },
          ],
        },
      }),
    });

    const settings = await getCourseIntegrationSettings("course-1");

    expect(settings?.integrations).toHaveLength(1);
    expect(settings?.integrations[0]).toMatchObject({
      id: "discord-community",
      type: "discord",
      enabled: true,
    });
    expect(settings?.webhooks).toEqual([
      {
        id: "webhook-1",
        url: "https://example.com/course-events",
        events: ["enrollment.created"],
        enabled: true,
      },
    ]);
  });

  it("falls back to course-derived defaults when metadata is absent or invalid", async () => {
    mocks.getCourse.mockResolvedValue({ ...baseCourse, metadata: "{invalid" });

    const [notifications, integrations] = await Promise.all([
      getCourseNotificationSettings("course-1"),
      getCourseIntegrationSettings("course-1"),
    ]);

    expect(notifications?.studentNotifications.upcomingClasses).toBe(true);
    expect(notifications?.templates).toHaveLength(3);
    expect(integrations?.integrations.map((item) => item.id)).toEqual([
      "course-1-video",
      "course-1-classes",
    ]);
  });

  it("maps access settings for each enrollment state and optional limit", async () => {
    mocks.getCourse
      .mockResolvedValueOnce({
        ...baseCourse,
        maxEnrollments: 25,
        enrollmentDeadline: "2026-08-01T00:00:00.000Z",
      })
      .mockResolvedValueOnce({
        ...baseCourse,
        enrollmentStatus: "Closed",
        maxEnrollments: null,
        enrollmentDeadline: null,
      })
      .mockResolvedValueOnce({
        ...baseCourse,
        enrollmentStatus: "ApprovalRequired",
      });

    await expect(getCourseAccessSettings("open-course")).resolves.toMatchObject(
      {
        courseId: "open-course",
        enrollmentType: "open",
        maxEnrollments: 25,
        enrollmentEnd: "2026-08-01T00:00:00.000Z",
      },
    );
    await expect(
      getCourseAccessSettings("closed-course"),
    ).resolves.toMatchObject({
      courseId: "closed-course",
      enrollmentType: "closed",
      maxEnrollments: undefined,
      enrollmentEnd: undefined,
    });
    await expect(
      getCourseAccessSettings("approval-course"),
    ).resolves.toMatchObject({
      enrollmentType: "approval",
    });
  });

  it("returns null settings when the course does not exist", async () => {
    mocks.getCourse.mockResolvedValue(null);

    await expect(getCourseAccessSettings("missing-access")).resolves.toBeNull();
    await expect(
      getCourseNotificationSettings("missing-notifications"),
    ).resolves.toBeNull();
    await expect(
      getCourseIntegrationSettings("missing-integrations"),
    ).resolves.toBeNull();
  });

  it("derives disabled notification and integration defaults from course features", async () => {
    mocks.getCourse.mockResolvedValue({
      ...baseCourse,
      metadata: undefined,
      videoShowcaseUrl: "https://media.example/showcase.mp4",
      features: {
        hasClasses: false,
        hasAssessments: false,
        hasCertificate: false,
        hasDiscussions: false,
      },
    });

    const notifications = await getCourseNotificationSettings("defaults-off");
    const integrations = await getCourseIntegrationSettings("defaults-off");

    expect(notifications?.studentNotifications).toMatchObject({
      upcomingClasses: false,
      classReminders: [],
      assessmentResults: false,
      certificateReady: false,
      discussionReplies: false,
    });
    expect(
      notifications?.templates.map((template) => template.enabled),
    ).toEqual([true, false, false]);
    expect(integrations?.integrations).toEqual([
      expect.objectContaining({
        id: "course-1-video",
        enabled: true,
        status: "connected",
      }),
      expect.objectContaining({
        id: "course-1-classes",
        enabled: false,
        status: "disconnected",
      }),
    ]);
  });

  it("falls back field-by-field when stored settings have unusable shapes", async () => {
    mocks.getCourse
      .mockResolvedValueOnce({
        ...baseCourse,
        metadata: JSON.stringify({
          notificationSettings: {
            studentNotifications: null,
            instructorNotifications: null,
            templates: "not-an-array",
          },
        }),
      })
      .mockResolvedValueOnce({
        ...baseCourse,
        metadata: JSON.stringify({
          integrationSettings: {
            integrations: null,
            webhooks: "not-an-array",
          },
        }),
      });

    const notifications = await getCourseNotificationSettings(
      "partial-notifications",
    );
    const integrations = await getCourseIntegrationSettings(
      "partial-integrations",
    );

    expect(notifications?.studentNotifications.enrollmentConfirmation).toBe(
      true,
    );
    expect(notifications?.instructorNotifications.newEnrollment).toBe(true);
    expect(notifications?.templates).toHaveLength(3);
    expect(integrations?.integrations).toHaveLength(2);
    expect(integrations?.webhooks).toEqual([]);
  });

  it("ignores metadata primitives, arrays, and null values", async () => {
    mocks.getCourse
      .mockResolvedValueOnce({ ...baseCourse, metadata: "null" })
      .mockResolvedValueOnce({ ...baseCourse, metadata: "[]" })
      .mockResolvedValueOnce({ ...baseCourse, metadata: '"metadata"' });

    await expect(
      getCourseNotificationSettings("null-metadata"),
    ).resolves.toMatchObject({
      courseId: "course-1",
    });
    await expect(
      getCourseIntegrationSettings("array-metadata"),
    ).resolves.toMatchObject({
      courseId: "course-1",
    });
    await expect(
      getCourseNotificationSettings("primitive-metadata"),
    ).resolves.toMatchObject({
      courseId: "course-1",
    });
  });
});
