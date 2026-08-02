import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getTestingEventsDirectory: vi.fn(),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ refresh: vi.fn() }),
}));

vi.mock('@/lib/testing-lab/events-queries', () => ({
  getTestingEventsDirectory: mocks.getTestingEventsDirectory,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...rest }: { children: ReactNode; href: string }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

import TestingEventsPage from './page';

describe('Testing Events page', () => {
  it('renders API-backed events and the creation workflow', async () => {
    mocks.getTestingEventsDirectory.mockResolvedValue({
      accessIssues: [],
      events: [
        {
          id: 'event-1',
          name: 'August campus playtest',
          description: 'Moderated testing for community projects.',
          mode: 'InPerson',
          status: 'ApplicationsOpen',
          approvalMode: 'Committee',
          startsAt: '2026-08-12T18:00:00.000Z',
          endsAt: '2026-08-12T22:00:00.000Z',
          slotCount: 2,
          applicationCount: 5,
        },
      ],
    });

    render(await TestingEventsPage({ searchParams: Promise.resolve({ status: 'ApplicationsOpen' }) }));

    expect(screen.getByRole('heading', { name: 'Testing events' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /new event/i })).toBeInTheDocument();
    expect(screen.getByText('August campus playtest')).toBeInTheDocument();
    expect(screen.getByText('Applications Open')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /manage event/i })).toHaveAttribute(
      'href',
      '/dashboard/testing-lab/events/event-1',
    );
    expect(mocks.getTestingEventsDirectory).toHaveBeenCalledWith({
      status: 'ApplicationsOpen',
      skip: 0,
      take: 100,
    });
  });
});
