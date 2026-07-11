import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ updateCourseNotificationSettings: vi.fn() }));

vi.mock('@/lib/learning/actions', () => ({
  updateCourseNotificationSettings: mocks.updateCourseNotificationSettings,
}));

import { NotificationSettingsEditor } from './notification-settings-editor';

describe('NotificationSettingsEditor', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.updateCourseNotificationSettings.mockResolvedValue({ success: true, data: null });
  });

  it('updates channels, reminders, threshold, and template subjects', async () => {
    const user = userEvent.setup();
    render(
      <NotificationSettingsEditor
        settings={{
          courseId: 'course-1',
          studentNotifications: {
            enrollmentConfirmation: true,
            courseUpdates: true,
            newContent: true,
            upcomingClasses: true,
            classReminders: [60],
            assignmentDue: true,
            assessmentResults: true,
            certificateReady: true,
            discussionReplies: true,
          },
          instructorNotifications: {
            newEnrollment: true,
            newReview: true,
            supportTicket: true,
            discussionMention: true,
            lowRating: true,
            lowRatingThreshold: 3,
          },
          templates: [{ id: 'welcome', type: 'enrollment', subject: 'Welcome', enabled: true }],
          updatedAt: '2026-07-10T00:00:00.000Z',
        }}
      />,
    );

    await user.click(screen.getByRole('switch', { name: 'Course updates' }));
    await user.clear(screen.getByLabelText('Class reminder minutes'));
    await user.type(screen.getByLabelText('Class reminder minutes'), '120, 15');
    await user.clear(screen.getByLabelText('Low rating threshold'));
    await user.type(screen.getByLabelText('Low rating threshold'), '2');
    await user.clear(screen.getByLabelText('Subject for enrollment'));
    await user.type(screen.getByLabelText('Subject for enrollment'), 'Welcome to production');
    await user.click(screen.getByRole('button', { name: 'Save notification settings' }));

    expect(mocks.updateCourseNotificationSettings).toHaveBeenCalledWith(
      'course-1',
      expect.objectContaining({
        studentNotifications: expect.objectContaining({ courseUpdates: false, classReminders: [120, 15] }),
        instructorNotifications: expect.objectContaining({ lowRatingThreshold: 2 }),
        templates: [expect.objectContaining({ subject: 'Welcome to production' })],
      }),
    );
    expect(await screen.findByRole('status')).toHaveTextContent('Notification settings saved.');
  });
});
