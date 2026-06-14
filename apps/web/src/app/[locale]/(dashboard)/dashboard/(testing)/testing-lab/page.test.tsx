import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  countAvailableTesterSlots: vi.fn(),
  getTestingLabDashboard: vi.fn(),
  normalizeTestingLocationStatus: vi.fn(),
  normalizeTestingRequestStatus: vi.fn(),
  normalizeTestingSessionStatus: vi.fn(),
  submitTestingBuild: vi.fn(),
}));

vi.mock('@/lib/testing-lab/actions', () => ({
  submitTestingBuild: mocks.submitTestingBuild,
}));

vi.mock('@/lib/testing-lab', () => ({
  countAvailableTesterSlots: mocks.countAvailableTesterSlots,
  getTestingLabDashboard: mocks.getTestingLabDashboard,
  normalizeTestingLocationStatus: mocks.normalizeTestingLocationStatus,
  normalizeTestingRequestStatus: mocks.normalizeTestingRequestStatus,
  normalizeTestingSessionStatus: mocks.normalizeTestingSessionStatus,
}));

import TestingLabPage from './page';

describe('testing lab dashboard page', () => {
  it('renders live Testing Lab datasets and the submit form', async () => {
    mocks.normalizeTestingRequestStatus.mockReturnValue('Open');
    mocks.normalizeTestingSessionStatus.mockReturnValue('Scheduled');
    mocks.normalizeTestingLocationStatus.mockReturnValue('Active');
    mocks.countAvailableTesterSlots.mockReturnValue(8);
    mocks.getTestingLabDashboard.mockResolvedValue({
      accessIssues: [],
      requests: [
        {
          id: 'request-1',
          title: 'Combat prototype playtest',
          description: 'Validate onboarding and first combat loop.',
          downloadUrl: 'https://example.com/build.zip',
          status: 'Open',
          maxTesters: 12,
          currentTesterCount: 4,
          startDate: '2026-07-01T12:00:00.000Z',
          endDate: '2026-07-14T12:00:00.000Z',
        },
      ],
      sessions: [
        {
          id: 'session-1',
          sessionName: 'Friday feedback lab',
          sessionDate: '2026-07-03T12:00:00.000Z',
          location: { id: 'location-1', name: 'Remote lab', status: 'Active' },
          maxTesters: 10,
          registeredTesterCount: 2,
          status: 'Scheduled',
        },
      ],
      locations: [
        {
          id: 'location-1',
          name: 'Remote lab',
          isVirtual: true,
          capacity: 20,
          maxProjectsCapacity: 5,
          status: 'Active',
        },
      ],
      publicSessions: [{ id: 'session-1', sessionName: 'Friday feedback lab', status: 'Scheduled' }],
    });

    render(await TestingLabPage());

    expect(screen.getByRole('heading', { name: 'Testing Lab' })).toBeInTheDocument();
    expect(screen.getByText('Combat prototype playtest')).toBeInTheDocument();
    expect(screen.getByText('Friday feedback lab')).toBeInTheDocument();
    expect(screen.getByText('Remote lab')).toBeInTheDocument();
    expect(screen.getByText('8')).toBeInTheDocument();
    expect(screen.getByLabelText('Title')).toBeRequired();
    expect(screen.getByLabelText('Team identifier')).toBeRequired();
    expect(screen.getByLabelText('Version')).toBeRequired();
    expect(screen.getByRole('button', { name: 'Submit to Testing Lab' })).toBeEnabled();
  });
});
