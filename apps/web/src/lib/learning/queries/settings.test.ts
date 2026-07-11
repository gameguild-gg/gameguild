import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getCourse: vi.fn(),
}));

vi.mock('./course', () => ({
  getCourse: mocks.getCourse,
}));

import { getCourseIntegrationSettings, getCourseNotificationSettings } from './settings';

const baseCourse = {
  id: 'course-1',
  title: 'Production Art',
  updatedAt: '2026-07-10T10:00:00.000Z',
  videoShowcaseUrl: null,
  enrollmentStatus: 'Open',
  visibility: 'public',
  features: {
    hasClasses: true,
    hasAssessments: true,
    hasCertificate: true,
    hasDiscussions: true,
  },
};

describe('course operational settings queries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('hydrates persisted notification settings from course metadata', async () => {
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
            { id: 'custom-template', type: 'course-update', subject: 'Studio update', enabled: false },
          ],
        },
      }),
    });

    const settings = await getCourseNotificationSettings('course-1');

    expect(settings?.studentNotifications.courseUpdates).toBe(false);
    expect(settings?.studentNotifications.enrollmentConfirmation).toBe(true);
    expect(settings?.studentNotifications.classReminders).toEqual([120, 15]);
    expect(settings?.instructorNotifications.lowRatingThreshold).toBe(2);
    expect(settings?.templates).toEqual([
      { id: 'custom-template', type: 'course-update', subject: 'Studio update', enabled: false },
    ]);
  });

  it('hydrates persisted integrations and webhooks from course metadata', async () => {
    mocks.getCourse.mockResolvedValue({
      ...baseCourse,
      metadata: JSON.stringify({
        integrationSettings: {
          integrations: [
            {
              id: 'discord-community',
              type: 'discord',
              name: 'Class Discord',
              enabled: true,
              status: 'connected',
              config: { inviteUrl: 'https://discord.gg/gameguild' },
            },
          ],
          webhooks: [
            { id: 'webhook-1', url: 'https://example.com/course-events', events: ['enrollment.created'], enabled: true },
          ],
        },
      }),
    });

    const settings = await getCourseIntegrationSettings('course-1');

    expect(settings?.integrations).toHaveLength(1);
    expect(settings?.integrations[0]).toMatchObject({ id: 'discord-community', type: 'discord', enabled: true });
    expect(settings?.webhooks).toEqual([
      { id: 'webhook-1', url: 'https://example.com/course-events', events: ['enrollment.created'], enabled: true },
    ]);
  });

  it('falls back to course-derived defaults when metadata is absent or invalid', async () => {
    mocks.getCourse.mockResolvedValue({ ...baseCourse, metadata: '{invalid' });

    const [notifications, integrations] = await Promise.all([
      getCourseNotificationSettings('course-1'),
      getCourseIntegrationSettings('course-1'),
    ]);

    expect(notifications?.studentNotifications.upcomingClasses).toBe(true);
    expect(notifications?.templates).toHaveLength(3);
    expect(integrations?.integrations.map((item) => item.id)).toEqual(['course-1-video', 'course-1-classes']);
  });
});
