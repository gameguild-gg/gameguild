import { fireEvent, render, screen, within } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getPublicTestingEventExperience: vi.fn(),
  getTestingProjectVersionOptions: vi.fn(),
  getLocalizationPreference: vi.fn(),
}));

vi.mock('@/lib/testing-lab/events-queries', () => ({
  getPublicTestingEventExperience: mocks.getPublicTestingEventExperience,
}));

vi.mock('@/lib/testing-lab/queries', () => ({
  getTestingProjectVersionOptions: mocks.getTestingProjectVersionOptions,
}));

vi.mock('@/lib/user-settings/queries', () => ({
  getLocalizationPreference: mocks.getLocalizationPreference,
}));

vi.mock('@/components/testing-lab/testing-project-application', () => ({
  TestingProjectApplication: ({ applications }: { applications?: Array<{ status?: string }> }) => (
    <div>Application states: {applications?.map((application) => application.status).join(', ') || 'new'}</div>
  ),
}));

vi.mock('@/components/testing-lab/testing-slot-registration', () => ({
  TestingSlotRegistration: ({ registration, registrationOpen, showClosedMessage, timeZoneId }: { registration?: { status?: string }; registrationOpen?: boolean; showClosedMessage?: boolean; timeZoneId?: string }) => (
    <div>
      <div>Tester state: {registration?.status ?? (registrationOpen ? 'open' : 'closed')}</div>
      <div>Timezone: {timeZoneId}</div>
      {!registrationOpen && showClosedMessage !== false ? (
        <p>This session is not accepting new registrations. Browse other playtests for an open seat.</p>
      ) : null}
    </div>
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
    mocks.getTestingProjectVersionOptions.mockResolvedValue([
      { id: 'version-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'published' },
      { id: 'version-3', projectId: 'project-3', projectTitle: 'Night Market', versionNumber: '1.2.0', status: 'published' },
    ]);
    mocks.getLocalizationPreference.mockResolvedValue({ language: 'en-US', timezone: 'America/Sao_Paulo', dateFormat: 'MM/dd/yyyy', timeFormat: '12h', currency: 'USD' });
  });

  it('composes applications, registrations, feedback, and approved game details for an authenticated visitor', async () => {
    mocks.getPublicTestingEventExperience.mockResolvedValue({
      event: {
        id: 'event-1',
        name: 'August campus playtest',
        description: 'Test community games with their creators.',
        mode: 'InPerson',
        status: 'ApplicationsOpen',
        approvalMode: 'ManagerOnly',
        requiresFeedback: true,
        games: [
          { projectId: 'project-1', title: 'Asterion', shortDescription: 'A short puzzle adventure.', description: 'Explore the clockwork city, solve environmental puzzles, and help its residents restore the old observatory.', imageUrl: '/images/asterion-cover.png' },
          { projectId: 'project-2', title: 'Starling', shortDescription: 'A small exploration game.' },
        ],
        slots: [{ id: 'slot-1', mode: 'InPerson', campusName: 'Downtown campus', roomName: 'Lab 4' }],
      },
      applications: [
        { id: 'application-1', projectId: 'project-1', status: 'Approved', decisionRationale: 'Approved for this playtest.' },
        { id: 'application-2', projectId: 'project-3', status: 'Pending' },
      ],
      registrations: [{ id: 'registration-1', slotId: 'slot-1', status: 'Waitlisted' }],
      feedbackObligations: [{ id: 'obligation-1', status: 'Pending' }],
      isAuthenticated: true,
      accessIssues: [],
    });

    render(await PublicTestingEventDetailPage({ params: Promise.resolve({ eventId: 'event-1' }) }));

    expect(screen.getByRole('heading', { name: 'August campus playtest' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Games in this playtest' })).not.toBeInTheDocument();
    const gameContent = screen.getByRole('region', { name: 'Game content' });
    const gameCarousel = within(gameContent).getByRole('region', { name: 'Featured game: Asterion' });
    expect(within(gameCarousel).getByRole('heading', { name: 'Asterion' })).toBeInTheDocument();
    expect(within(gameCarousel).getByRole('img', { name: 'Artwork for Asterion' })).toBeVisible();
    expect(within(gameCarousel).getByText(/clockwork city/)).toBeInTheDocument();
    const gameList = within(gameContent).getByRole('list', { name: 'Games and submissions' });
    expect(within(gameList).getByRole('button', { name: 'Show Starling in carousel' })).toBeInTheDocument();
    expect(within(gameList).getByText('Night Market')).toBeInTheDocument();
    expect(within(gameList).getByText('Your game submission')).toBeInTheDocument();
    expect(within(gameList).getByText('Pending')).toBeInTheDocument();
    expect(within(gameList).getByRole('link', { name: 'Review Night Market application' })).toHaveAttribute('href', '/testing-lab/events/event-1?submitGame=1&projectId=project-3#submit-game');
    expect(gameCarousel.compareDocumentPosition(gameList) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(within(gameContent).queryByRole('heading', { name: 'Schedule' })).not.toBeInTheDocument();
    expect(screen.queryByRole('group', { name: 'Project display' })).not.toBeInTheDocument();
    fireEvent.click(within(gameCarousel).getByRole('button', { name: 'Next game' }));
    expect(within(gameCarousel).getByRole('heading', { name: 'Starling' })).toBeInTheDocument();
    expect(within(gameCarousel).getByText('A small exploration game.')).toBeInTheDocument();
    expect(within(gameList).getByRole('button', { name: 'Show Starling in carousel' })).toHaveAttribute('aria-current', 'true');
    const eventInfo = screen.getByRole('complementary', { name: 'Playtest details' });
    expect(within(eventInfo).getByRole('heading', { name: 'Tester - Sign up' })).toBeInTheDocument();
    expect(within(eventInfo).getAllByText('This session is not accepting new registrations. Browse other playtests for an open seat.')).toHaveLength(1);
    expect(within(eventInfo).getByText('Timezone: America/Sao_Paulo')).toBeInTheDocument();
    expect(within(eventInfo).getByRole('heading', { name: 'Rules for testers' })).toBeInTheDocument();
    expect(within(eventInfo).getByRole('heading', { name: 'At a glance' })).toBeInTheDocument();
    expect(within(eventInfo).getByText('Tester spots')).toBeInTheDocument();
    expect(within(eventInfo).getByText('Games / game spots')).toBeInTheDocument();
    expect(within(eventInfo).getByText('2 / No limit')).toBeInTheDocument();
    expect(within(eventInfo).getByText('Feedback', { exact: true })).toBeInTheDocument();
    expect(within(eventInfo).getByText('Required', { exact: true })).toBeInTheDocument();
    expect(within(eventInfo).queryByText('Feedback required')).not.toBeInTheDocument();
    expect(within(gameList).queryByText('Decision details')).not.toBeInTheDocument();
    expect(within(eventInfo).queryByText('Your game submissions')).not.toBeInTheDocument();
    expect(within(eventInfo).queryByRole('link', { name: 'Submit a game' })).not.toBeInTheDocument();
    expect(within(gameList).getByText('Your game')).toBeInTheDocument();
    expect(within(gameList).getByText('Approved')).toBeInTheDocument();
    expect(within(gameList).getByRole('link', { name: 'Review Asterion application' })).toHaveAttribute('href', '/testing-lab/events/event-1?submitGame=1&projectId=project-1#submit-game');
    expect(within(gameContent).queryByRole('heading', { name: 'Your project' })).not.toBeInTheDocument();
    expect(within(gameContent).getByText('Submit a game')).toBeInTheDocument();
    expect(within(eventInfo).queryByText('Application states: Approved')).not.toBeInTheDocument();
    expect(within(eventInfo).getByText('Tester state: Waitlisted')).toBeInTheDocument();
    expect(within(eventInfo).getByText('Feedback obligations: 1')).toBeInTheDocument();
    expect(mocks.getTestingProjectVersionOptions).toHaveBeenCalledOnce();
    expect(mocks.getLocalizationPreference).toHaveBeenCalledOnce();
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

    expect(screen.queryByText(/Application states/)).not.toBeInTheDocument();
    expect(mocks.getTestingProjectVersionOptions).not.toHaveBeenCalled();
    expect(mocks.getLocalizationPreference).not.toHaveBeenCalled();
  });

  it('shows an approved submission with its game instead of repeating a project card when submissions are closed', async () => {
    mocks.getPublicTestingEventExperience.mockResolvedValue({
      event: {
        id: 'event-1',
        name: 'Closed playtest',
        mode: 'Online',
        status: 'ApplicationsClosed',
        approvalMode: 'ManagerOnly',
        requiresFeedback: false,
        games: [{ projectId: 'project-1', title: 'Asterion' }],
        slots: [],
      },
      applications: [{ id: 'application-1', projectId: 'project-1', status: 'Approved' }],
      registrations: [],
      feedbackObligations: [],
      isAuthenticated: true,
      accessIssues: [],
    });

    render(await PublicTestingEventDetailPage({ params: Promise.resolve({ eventId: 'event-1' }) }));

    const gameContent = screen.getByRole('region', { name: 'Game content' });
    const gameList = within(gameContent).getByRole('list', { name: 'Games and submissions' });
    expect(within(gameList).getByRole('button', { name: 'Show Asterion in carousel' })).toBeInTheDocument();
    expect(within(gameList).getByText('Your game')).toBeInTheDocument();
    expect(within(gameList).getByText('Approved')).toBeInTheDocument();
    expect(within(gameList).getByRole('link', { name: 'Review Asterion application' })).toHaveAttribute('href', '/testing-lab/events/event-1?submitGame=1&projectId=project-1#submit-game');
    expect(within(gameContent).queryByRole('heading', { name: 'Your project' })).not.toBeInTheDocument();
    expect(within(gameContent).queryByText('Review this game submission')).not.toBeInTheDocument();
    expect(mocks.getTestingProjectVersionOptions).not.toHaveBeenCalled();
  });

  it('clearly marks a past event closed while keeping its approved game briefing public', async () => {
    mocks.getPublicTestingEventExperience.mockResolvedValue({
      event: {
        id: 'event-1',
        name: 'Past community playtest',
        description: 'Community playtest of the latest game jam builds.',
        mode: 'Online',
        status: 'Scheduled',
        startsAt: '2020-08-12T13:00:00.000Z',
        endsAt: '2020-08-12T15:00:00.000Z',
        approvalMode: 'ManagerOnly',
        requiresFeedback: true,
        games: [{ projectId: 'project-1', title: 'Lantern Keeper', shortDescription: 'A small world of quiet puzzles.' }],
        slots: [],
      },
      applications: [],
      registrations: [],
      feedbackObligations: [],
      isAuthenticated: false,
      accessIssues: [],
    });

    render(await PublicTestingEventDetailPage({ params: Promise.resolve({ eventId: 'event-1' }) }));

    const eventInfo = screen.getByRole('complementary', { name: 'Playtest details' });
    expect(within(eventInfo).getByRole('heading', { name: 'Past community playtest' })).toBeInTheDocument();
    expect(within(eventInfo).getByText('Playtest ended')).toBeInTheDocument();
    expect(within(eventInfo).getByText('Online')).toBeInTheDocument();
    expect(within(eventInfo).queryByText('Feedback required')).not.toBeInTheDocument();
    expect(within(eventInfo).getByText('Feedback', { exact: true })).toBeInTheDocument();
    expect(within(eventInfo).getByText('Required', { exact: true })).toBeInTheDocument();
    expect(within(eventInfo).getByText('Community playtest of the latest game jam builds.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Lantern Keeper' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Sessions' })).not.toBeInTheDocument();
    const browseLink = within(eventInfo).getByRole('link', { name: /browse other playtests/i });
    expect(browseLink).toHaveAttribute('href', '/testing-lab');
    expect(browseLink).toHaveClass('text-primary');
    expect(browseLink).not.toHaveClass('border-border');
    expect(within(eventInfo).getByRole('button', { name: 'Registration closed' })).toBeDisabled();
    expect(within(eventInfo).getByRole('heading', { name: 'Tester - Sign up' })).toBeInTheDocument();
    expect(within(eventInfo).getByText('This session is not accepting new registrations. Browse other playtests for an open seat.')).toBeInTheDocument();
    expect(mocks.getTestingProjectVersionOptions).not.toHaveBeenCalled();
  });

  it('explains when an ended playtest has no published games', async () => {
    mocks.getPublicTestingEventExperience.mockResolvedValue({
      event: {
        id: 'event-1',
        name: 'Past playtest',
        mode: 'Online',
        status: 'Completed',
        endsAt: '2020-08-12T15:00:00.000Z',
        approvalMode: 'ManagerOnly',
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

    expect(screen.queryByRole('heading', { name: 'Games in this playtest' })).not.toBeInTheDocument();
    expect(screen.getByText('No games were published for this playtest.')).toBeInTheDocument();
    expect(screen.queryByText('Submit a game')).not.toBeInTheDocument();
    expect(screen.queryByText('Lineup in progress')).not.toBeInTheDocument();
    expect(mocks.getTestingProjectVersionOptions).not.toHaveBeenCalled();
  });

  it('keeps tester registration distinct from the game submission window', async () => {
    mocks.getPublicTestingEventExperience.mockResolvedValue({
      event: {
        id: 'event-1',
        name: 'Upcoming tester session',
        mode: 'Online',
        status: 'Scheduled',
        startsAt: '2030-08-12T13:00:00.000Z',
        endsAt: '2030-08-12T15:00:00.000Z',
        approvalMode: 'ManagerOnly',
        requiresFeedback: false,
        configuration: {
          frozenAt: '2030-08-01T12:00:00.000Z',
          testerRegistrationSchema: { questions: [] },
        },
        slots: [{ id: 'slot-1', mode: 'Online', endsAt: '2030-08-12T15:00:00.000Z' }],
      },
      applications: [],
      registrations: [],
      feedbackObligations: [],
      isAuthenticated: false,
      accessIssues: [],
    });

    render(await PublicTestingEventDetailPage({ params: Promise.resolve({ eventId: 'event-1' }) }));

    const eventInfo = screen.getByRole('complementary', { name: 'Playtest details' });
    expect(within(eventInfo).getByText('Tester state: open')).toBeInTheDocument();
    const signup = screen.getByRole('link', { name: /Sign up to test/ });
    expect(signup).toHaveAttribute('href', '#schedule');
    const browseLink = within(eventInfo).getByRole('link', { name: /Browse other playtests/ });
    expect(browseLink).toHaveClass('text-primary');
    expect(browseLink).not.toHaveClass('border-border');
    expect(within(eventInfo).getByRole('heading', { name: 'Tester - Sign up' })).toBeInTheDocument();
    expect(within(eventInfo).getByText('Timezone: UTC')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Submit a game' })).not.toBeInTheDocument();
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
    expect(screen.getByRole('link', { name: /browse other playtests/i })).toHaveAttribute('href', '/testing-lab');
    expect(consoleError).toHaveBeenCalledWith('[testing-lab] public event event-1 could not be loaded', ['Public event failed: response validation failed']);
    consoleError.mockRestore();
  });

  it('explains when a public playtest ID cannot be found', async () => {
    mocks.getPublicTestingEventExperience.mockResolvedValue({
      event: null,
      applications: [],
      registrations: [],
      feedbackObligations: [],
      isAuthenticated: false,
      accessIssues: ['Public event returned 404: TestingLab.PublicEventNotFound'],
    });

    render(await PublicTestingEventDetailPage({ params: Promise.resolve({ eventId: 'missing-event' }) }));

    expect(screen.getByRole('heading', { level: 1, name: 'Playtest not found' })).toBeInTheDocument();
    expect(screen.getByText(/URL may be incorrect/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /browse other playtests/i })).toHaveAttribute('href', '/testing-lab');
  });
});
