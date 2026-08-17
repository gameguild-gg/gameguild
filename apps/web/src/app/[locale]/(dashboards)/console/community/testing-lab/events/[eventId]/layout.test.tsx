import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getTestingEventWorkspaceData: vi.fn(),
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/workspace/learning',
  useRouter: () => ({ refresh: vi.fn() }),
}));

vi.mock('@/lib/testing-lab/events-queries', () => ({
  getTestingEventWorkspaceData: mocks.getTestingEventWorkspaceData,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...rest }: { children: ReactNode; href: string }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
  usePathname: () => '/console/community/testing-lab/events/event-1/overview',
}));

import TestingEventWorkspaceLayout from './layout';

describe('Testing Event workspace layout', () => {
  it('renders event identity, lifecycle, and contextual navigation', async () => {
    mocks.getTestingEventWorkspaceData.mockResolvedValue({
      accessIssues: [],
      event: {
        id: 'event-1',
        name: 'August campus playtest',
        description: 'Moderated testing for community projects.',
        mode: 'InPerson',
        status: 'ApplicationsOpen',
        approvalMode: 'Committee',
        requiresFeedback: true,
      },
      slots: [],
      applications: [],
      committee: [],
      registrationsBySlot: {},
    });

    render(
      await TestingEventWorkspaceLayout({
        params: Promise.resolve({ eventId: 'event-1' }),
        children: <p>Workspace content</p>,
      }),
    );

    expect(screen.getByRole('heading', { name: 'August campus playtest', level: 1 })).toBeInTheDocument();
    expect(screen.getByRole('navigation', { name: 'Testing event workspace' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Overview' })).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('link', { name: 'Applications' })).toHaveAttribute(
      'href',
      '/console/community/testing-lab/events/event-1/applications',
    );
    expect(screen.getByText('Workspace content')).toBeInTheDocument();
  });

  it('marks completed events read-only and removes edit controls', async () => {
    mocks.getTestingEventWorkspaceData.mockResolvedValue({
      accessIssues: [],
      event: {
        id: 'event-1',
        name: 'Completed playtest',
        status: 'Completed',
        mode: 'Online',
        approvalMode: 'ManagerOnly',
      },
      slots: [],
      applications: [],
      committee: [],
      registrationsBySlot: {},
    });

    render(
      await TestingEventWorkspaceLayout({
        params: Promise.resolve({ eventId: 'event-1' }),
        children: <p>Workspace content</p>,
      }),
    );

    expect(screen.getByText(/read-only/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /edit/i })).not.toBeInTheDocument();
  });
});
