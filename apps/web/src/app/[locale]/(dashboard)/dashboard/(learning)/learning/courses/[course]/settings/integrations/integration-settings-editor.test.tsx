import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ updateCourseIntegrationSettings: vi.fn() }));

vi.mock('@/lib/learning/actions', () => ({
  updateCourseIntegrationSettings: mocks.updateCourseIntegrationSettings,
}));

import { IntegrationSettingsEditor } from './integration-settings-editor';

describe('IntegrationSettingsEditor', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.updateCourseIntegrationSettings.mockResolvedValue({ success: true, data: null });
  });

  it('toggles an integration and adds an outbound webhook through a dialog', async () => {
    const user = userEvent.setup();
    render(
      <IntegrationSettingsEditor
        settings={{
          courseId: 'course-1',
          integrations: [{ id: 'discord', type: 'discord', name: 'Class Discord', enabled: false, config: {}, status: 'disconnected' }],
          webhooks: [],
          updatedAt: '2026-07-10T00:00:00.000Z',
        }}
      />,
    );

    await user.click(screen.getByRole('switch', { name: 'Enable Class Discord' }));
    await user.click(screen.getByRole('button', { name: 'Add webhook' }));
    await user.type(screen.getByLabelText('Webhook URL'), 'https://example.com/course-events');
    await user.clear(screen.getByLabelText('Events'));
    await user.type(screen.getByLabelText('Events'), 'course.updated, enrollment.created');
    await user.click(screen.getByRole('button', { name: 'Add to course' }));
    await user.click(screen.getByRole('button', { name: 'Save integration settings' }));

    expect(mocks.updateCourseIntegrationSettings).toHaveBeenCalledWith(
      'course-1',
      expect.objectContaining({
        integrations: [expect.objectContaining({ id: 'discord', enabled: true, status: 'connected' })],
        webhooks: [expect.objectContaining({ url: 'https://example.com/course-events', events: ['course.updated', 'enrollment.created'] })],
      }),
    );
    expect(await screen.findByRole('status')).toHaveTextContent('Integration settings saved.');
  });
});
