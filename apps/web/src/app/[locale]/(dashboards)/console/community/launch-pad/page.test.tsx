import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createLaunchPadEventForm: vi.fn(),
  createLaunchPadSlotForm: vi.fn(),
  transitionLaunchPadEventForm: vi.fn(),
  getManagedLaunchPadEvents: vi.fn(),
  getManagedLaunchPadEvent: vi.fn(),
  getDashboardContexts: vi.fn(),
}));

vi.mock('@/lib/launch-pad/actions', () => ({
  createLaunchPadEventForm: mocks.createLaunchPadEventForm,
  createLaunchPadSlotForm: mocks.createLaunchPadSlotForm,
  transitionLaunchPadEventForm: mocks.transitionLaunchPadEventForm,
}));

vi.mock('@/lib/launch-pad/queries', () => ({
  getManagedLaunchPadEvents: mocks.getManagedLaunchPadEvents,
  getManagedLaunchPadEvent: mocks.getManagedLaunchPadEvent,
}));

vi.mock('@/lib/dashboard-contexts', () => ({
  getDashboardContexts: mocks.getDashboardContexts,
  hasAnyDashboardCapability: (capabilities: string[], capability: string) => capabilities.includes(capability),
}));

vi.mock('@/components/ui/date-time-picker', () => ({
  DateTimePicker: ({ id, name, required }: { id: string; name: string; required?: boolean }) => <input id={id} name={name} required={required} />,
}));

import LaunchPadManagementPage from './page';

describe('launch pad management page', () => {
  it('renders event lifecycle management without exposing personal participation', async () => {
    mocks.getDashboardContexts.mockResolvedValue({ capabilities: ['LaunchPad.ManageEvents'] });
    mocks.getManagedLaunchPadEvents.mockResolvedValue([{
      id: 'event-1', name: 'Community launch', description: 'Present approved releases',
      startsAt: '2026-09-01T18:00:00.000Z', endsAt: '2026-09-01T21:00:00.000Z', status: 'ApplicationsOpen',
    }]);
    mocks.getManagedLaunchPadEvent.mockResolvedValue({
      event: {
        id: 'event-1', name: 'Community launch', description: 'Present approved releases',
        startsAt: '2026-09-01T18:00:00.000Z', endsAt: '2026-09-01T21:00:00.000Z', status: 'ApplicationsOpen',
      },
      slots: [],
    });

    render(await LaunchPadManagementPage());

    expect(screen.getByRole('heading', { name: /Launch Pad management/i })).toBeInTheDocument();
    expect(screen.getByText('Community launch')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Move to ApplicationsClosed' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create draft event' })).toBeInTheDocument();
    expect(screen.getByLabelText('Event starts')).toBeRequired();
    expect(screen.queryByText(/your participation/i)).not.toBeInTheDocument();
  });
});
