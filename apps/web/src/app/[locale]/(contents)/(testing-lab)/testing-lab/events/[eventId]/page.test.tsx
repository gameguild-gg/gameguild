import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getPublicTestingEventExperience: vi.fn(),
  getTestingProjectVersionOptions: vi.fn(),
}));

vi.mock('@/lib/testing-lab/events-queries', () => ({
  getPublicTestingEventExperience: mocks.getPublicTestingEventExperience,
}));

vi.mock('@/lib/testing-lab/queries', () => ({
  getTestingProjectVersionOptions: mocks.getTestingProjectVersionOptions,
}));

vi.mock('@/components/testing-lab/testing-project-application', () => ({
  TestingProjectApplication: ({ application }: { application?: { status?: string } }) => (
    <div>Application state: {application?.status ?? 'new'}</div>
  ),
}));

vi.mock('@/components/testing-lab/testing-slot-registration', () => ({
  TestingSlotRegistration: ({ registration }: { registration?: { status?: string } }) => (
    <div>Tester state: {registration?.status ?? 'available'}</div>
  ),
}));

vi.mock('@/components/testing-lab/testing-feedback-submission', () => ({
  TestingFeedbackSubmission: ({ obligations }: { obligations: unknown[] }) => (
    <div>Feedback obligations: {obligations.length}</div>
  ),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...rest }: { children: ReactNode; href: string }) => (
    <a href={href} {...rest}>{children}</a>
  ),
}));

import PublicTestingEventDetailPage from './page';

describe('Public Testing Event detail page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getTestingProjectVersionOptions.mockResolvedValue([{ id: 'version-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'published' }]);
  });

  it('composes candidacy, tester registration, and feedback from actor-scoped API data', async () => {
    mocks.getPublicTestingEventExperience.mockResolvedValue({
      event: {
        id: 'event-1',
        name: 'August campus playtest',
        description: 'Test community games with their creators.',
        mode: 'InPerson',
        status: 'ApplicationsOpen',
        approvalMode: 'ManagerOnly',
        requiresFeedback: true,
        slots: [{ id: 'slot-1', mode: 'InPerson', campusName: 'Downtown campus', roomName: 'Lab 4' }],
      },
      applications: [{ id: 'application-1', status: 'Pending' }],
      registrations: [{ id: 'registration-1', slotId: 'slot-1', status: 'Waitlisted' }],
      feedbackObligations: [{ id: 'obligation-1', status: 'Pending' }],
      isAuthenticated: true,
      accessIssues: [],
    });

    render(await PublicTestingEventDetailPage({ params: Promise.resolve({ eventId: 'event-1' }) }));

    expect(screen.getByRole('heading', { name: 'August campus playtest' })).toBeInTheDocument();
    expect(screen.getByText('Application state: Pending')).toBeInTheDocument();
    expect(screen.getByText('Tester state: Waitlisted')).toBeInTheDocument();
    expect(screen.getByText('Feedback obligations: 1')).toBeInTheDocument();
    expect(mocks.getTestingProjectVersionOptions).toHaveBeenCalledOnce();
  });

  it('does not query private project choices for anonymous visitors', async () => {
    mocks.getPublicTestingEventExperience.mockResolvedValue({
      event: {
        id: 'event-1',
        name: 'Online project clinic',
        mode: 'Online',
        status: 'Scheduled',
        approvalMode: 'Committee',
        requiresFeedback: false,
        slots: [],
      },
      applications: [],
      registrations: [],
      feedbackObligations: [],
      isAuthenticated: false,
      accessIssues: [],
    });

    render(await PublicTestingEventDetailPage({ params: Promise.resolve({ eventId: 'event-1' }) }));

    expect(screen.getByText('Application state: new')).toBeInTheDocument();
    expect(mocks.getTestingProjectVersionOptions).not.toHaveBeenCalled();
  });

  it('renders an accessible retry state and records the public contract failure', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    mocks.getPublicTestingEventExperience.mockResolvedValue({
      event: null,
      applications: [],
      registrations: [],
      feedbackObligations: [],
      isAuthenticated: false,
      accessIssues: ['Public event failed: response validation failed'],
    });

    render(await PublicTestingEventDetailPage({ params: Promise.resolve({ eventId: 'event-1' }) }));

    expect(screen.getByRole('heading', { level: 1, name: 'Event temporarily unavailable' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Back to Testing Lab events' })).toHaveAttribute('href', '/testing-lab/events');
    expect(consoleError).toHaveBeenCalledWith('[testing-lab] public event event-1 could not be loaded', ['Public event failed: response validation failed']);
    consoleError.mockRestore();
  });
});
