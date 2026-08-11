import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getTestingLabAnalytics: vi.fn(),
  getTestingLabDashboard: vi.fn(),
  getTestingEventsDirectory: vi.fn(),
  normalizeTestingRequestStatus: vi.fn(),
  normalizeTestingSessionStatus: vi.fn(),
}));

vi.mock('@/lib/testing-lab', () => ({
  getTestingLabAnalytics: mocks.getTestingLabAnalytics,
  getTestingLabDashboard: mocks.getTestingLabDashboard,
  normalizeTestingRequestStatus: mocks.normalizeTestingRequestStatus,
  normalizeTestingSessionStatus: mocks.normalizeTestingSessionStatus,
}));

vi.mock('@/lib/testing-lab/events-queries', () => ({
  getTestingEventsDirectory: mocks.getTestingEventsDirectory,
}));

vi.mock('@/components/testing-lab/testing-lab-calendar', () => ({
  TestingLabCalendar: ({ events }: { events: Array<{ name?: string }> }) => (
    <section aria-label="Testing Lab calendar">{events.map((event) => <span key={event.name}>{event.name}</span>)}</section>
  ),
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
  it('renders live Testing Lab metrics and the Testing Lab calendar', async () => {
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
      current: {
        events: 1,
        completedEvents: 0,
        applications: 1,
        approvedProjects: 1,
        registeredTesters: 2,
        attendedTesters: 0,
        feedback: 0,
        averageRating: null,
        recommendationRate: null,
        capacity: 10,
        fillRate: 20,
      },
      previous: null,
      locations: { total: 1, active: 1 },
      trend: [],
      events: [],
    });
    mocks.getTestingEventsDirectory.mockResolvedValue({
      accessIssues: [],
      events: [{ id: 'event-1', name: 'Campus playtest', startsAt: '2026-08-10T18:00:00.000Z' }],
    });

    render(await TestingLabPage());

    expect(screen.getByRole('heading', { name: 'Testing Lab' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /public lab/i })).toHaveAttribute('href', '/testing-lab');
    expect(screen.getByRole('region', { name: 'Testing Lab calendar' })).toBeInTheDocument();
    expect(screen.getByText('Campus playtest')).toBeInTheDocument();
    expect(screen.getByText('20%')).toBeInTheDocument();
    expect(screen.getByText('Combat prototype playtest')).toBeInTheDocument();
    expect(screen.getByText('Friday feedback lab')).toBeInTheDocument();
    expect(screen.getByText('Remote lab')).toBeInTheDocument();
  });
});
