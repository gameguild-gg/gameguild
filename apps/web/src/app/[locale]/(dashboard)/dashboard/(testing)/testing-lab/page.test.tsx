import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getTestingLabAnalytics: vi.fn(),
  getTestingLabDashboard: vi.fn(),
  normalizeTestingRequestStatus: vi.fn(),
  normalizeTestingSessionStatus: vi.fn(),
}));

vi.mock('@/lib/testing-lab', () => ({
  getTestingLabAnalytics: mocks.getTestingLabAnalytics,
  getTestingLabDashboard: mocks.getTestingLabDashboard,
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
  it('renders live Testing Lab metrics and routed operations', async () => {
    mocks.normalizeTestingRequestStatus.mockReturnValue('Open');
    mocks.normalizeTestingSessionStatus.mockReturnValue('Scheduled');
    mocks.getTestingLabDashboard.mockResolvedValue({
      accessIssues: [],
      requests: [
        {
          id: 'request-1',
          title: 'Combat prototype playtest',
          description: 'Validate onboarding and first combat loop.',
          status: 'Open',
        },
      ],
      sessions: [
        {
          id: 'session-1',
          sessionName: 'Friday feedback lab',
          location: { id: 'location-1', name: 'Remote lab', status: 'Active' },
          status: 'Scheduled',
        },
      ],
      locations: [],
      publicSessions: [],
    });
    mocks.getTestingLabAnalytics.mockResolvedValue({
      accessIssues: [],
      requests: { total: 1, open: 1, active: 0, completed: 0 },
      sessions: { total: 1, scheduled: 1, active: 0, completed: 0 },
      capacity: { total: 10, registered: 2, available: 8, waitlisted: 0, fillRate: 20 },
      feedback: { total: 0, averageRating: null, recommended: 0, recommendationRate: null },
      attendance: { registered: 2, attended: 0, attendanceRate: 0 },
      locations: {
        total: 1,
        active: 1,
        virtual: 1,
        rows: [{ id: 'location-1', name: 'Remote lab', sessions: 1, registered: 2, capacity: 10 }],
      },
    });

    render(await TestingLabPage());

    expect(screen.getByRole('heading', { name: 'Testing Lab' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /public lab/i })).toHaveAttribute('href', '/testing-lab');
    expect(screen.getByRole('heading', { name: 'Operations' })).toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: /projects/i }).some((link) => link.getAttribute('href') === '/dashboard/testing-lab/projects')).toBe(true);
    expect(screen.getAllByRole('link').some((link) => link.getAttribute('href') === '/dashboard/testing-lab/analytics')).toBe(true);
    expect(screen.getByText('20%')).toBeInTheDocument();
    expect(screen.getByText('Combat prototype playtest')).toBeInTheDocument();
    expect(screen.getByText('Friday feedback lab')).toBeInTheDocument();
    expect(screen.getByText('Remote lab')).toBeInTheDocument();
  });
});
