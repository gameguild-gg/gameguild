import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  countAvailableTesterSlots: vi.fn(),
  getTestingLabDashboard: vi.fn(),
  getTestingProjectOptions: vi.fn(),
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
  getTestingProjectOptions: mocks.getTestingProjectOptions,
  normalizeTestingLocationStatus: mocks.normalizeTestingLocationStatus,
  normalizeTestingRequestStatus: mocks.normalizeTestingRequestStatus,
  normalizeTestingSessionStatus: mocks.normalizeTestingSessionStatus,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...rest }: { children: ReactNode; href: string }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
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
          projectVersion: {
            id: 'version-1',
            projectId: 'project-1',
            versionNumber: '0.3.0',
            project: {
              id: 'project-1',
              title: 'Arena Tactics',
              slug: 'arena-tactics',
            },
          },
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
    mocks.getTestingProjectOptions.mockResolvedValue([
      {
        id: 'project-1',
        title: 'Arena Tactics',
        slug: 'arena-tactics',
        status: 'Published',
      },
    ]);

    render(await TestingLabPage());

    expect(screen.getByRole('heading', { name: 'Testing Lab' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /public lab/i })).toHaveAttribute('href', '/testing-lab');
    expect(screen.getByRole('link', { name: /project showcase/i })).toHaveAttribute('href', '/projects');
    expect(screen.getByRole('heading', { name: /operational workflow/i })).toBeInTheDocument();
    expect(screen.getByText('Combat prototype playtest')).toBeInTheDocument();
    expect(screen.getByText(/Arena Tactics · 0\.3\.0/)).toBeInTheDocument();
    expect(screen.getByRole('radio', { name: /Arena Tactics/ })).toBeInTheDocument();
    expect(screen.getByText('Friday feedback lab')).toBeInTheDocument();
    expect(screen.getByText('Remote lab')).toBeInTheDocument();
    expect(screen.getByText('8')).toBeInTheDocument();
    expect(screen.getByLabelText('Title')).toBeRequired();
    expect(screen.getByRole('radio', { name: /Arena Tactics/ })).toBeRequired();
    expect(screen.getByLabelText('Version')).toBeRequired();
    expect(screen.getByRole('button', { name: 'Submit to Testing Lab' })).toBeEnabled();
  });
});
