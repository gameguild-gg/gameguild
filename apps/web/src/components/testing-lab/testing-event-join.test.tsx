import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ComponentProps, ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, ...props }: ComponentProps<'a'>) => <a {...props}>{children}</a>,
}));

vi.mock('@game-guild/ui/components/dialog', () => ({
  Dialog: ({ open, children }: { open: boolean; children: ReactNode }) => <div>{open ? children : null}</div>,
  DialogContent: ({ children }: { children: ReactNode }) => <div role="dialog">{children}</div>,
  DialogDescription: ({ children }: { children: ReactNode }) => <p>{children}</p>,
  DialogHeader: ({ children }: { children: ReactNode }) => <header>{children}</header>,
  DialogTitle: ({ children }: { children: ReactNode }) => <h2>{children}</h2>,
}));

vi.mock('./testing-slot-registration', () => ({
  TestingSlotRegistration: ({ slot, onBack, signInReturnUrl }: {
    slot: { id?: string; mode?: string | null };
    onBack?: () => void;
    signInReturnUrl?: string;
  }) => (
    <div data-testid="tester-enrollment" data-slot-id={slot.id}>
      <span>{slot.mode ?? 'Online'}</span>
      {signInReturnUrl ? <span>{signInReturnUrl}</span> : null}
      {onBack ? <button type="button" onClick={onBack}>Back from tester form</button> : null}
    </div>
  ),
}));

vi.mock('./testing-project-application', () => ({
  TestingProjectApplication: ({ projectVersions, initialProjectId, selectedApplicationId, signInReturnUrl }: {
    projectVersions: Array<{ id: string; projectTitle: string }>;
    initialProjectId?: string;
    selectedApplicationId?: string;
    signInReturnUrl?: string;
  }) => (
    <div data-testid="developer-enrollment" data-project-id={initialProjectId} data-application-id={selectedApplicationId}>
      {projectVersions.map((version) => <span key={version.id}>{version.projectTitle}</span>)}
      {signInReturnUrl ? <span>{signInReturnUrl}</span> : null}
    </div>
  ),
}));

import { TestingEventJoin } from './testing-event-join';

type JoinProps = ComponentProps<typeof TestingEventJoin>;

function session(id: string, label: string, registrationOpen = true): JoinProps['testerSessions'][number] {
  return {
    id,
    label,
    slot: { id, mode: 'Online' },
    registrationOpen,
  };
}

const defaults: JoinProps = {
  eventId: 'event-1',
  isAuthenticated: true,
  testerSessions: [session('slot-1', 'Online · Oct 2, 6:00 PM')],
  testerUnavailableReason: 'Tester sign-up is closed for this playtest.',
  generalRules: 'Be respectful and share useful feedback.',
  testerInstructions: 'Join on time.',
  timeZoneId: 'UTC',
  locale: 'en-US',
  hour12: true,
  projectApplication: {
    acceptsApplications: true,
    projectVersions: [{ id: 'build-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'ReadyForTesting' }],
    applications: [],
    requiresFeedback: true,
  },
};

function renderJoin(overrides: Partial<JoinProps> = {}) {
  return render(<TestingEventJoin {...defaults} {...overrides} />);
}

describe('TestingEventJoin', () => {
  it('continues directly to tester enrollment inside the dialog', async () => {
    const user = userEvent.setup();
    renderJoin();

    await user.click(screen.getByRole('button', { name: 'Join' }));
    expect(screen.getByRole('heading', { name: 'How would you like to join?' })).toBeInTheDocument();
    expect(screen.queryByText('Play the games and share feedback with their creators.')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'As a tester' }));

    expect(screen.getByRole('heading', { name: 'Join as a tester' })).toBeInTheDocument();
    expect(screen.getByTestId('tester-enrollment')).toHaveAttribute('data-slot-id', 'slot-1');
    expect(screen.getByText('/testing-lab/events/event-1?joinAs=tester&slotId=slot-1#join')).toBeInTheDocument();
  });

  it('lets testers choose among open sessions without leaving the dialog', async () => {
    const user = userEvent.setup();
    renderJoin({
      testerSessions: [
        session('slot-1', 'Online · Oct 2'),
        session('slot-2', 'In person · Oct 3'),
        session('slot-closed', 'Online · Oct 4', false),
      ],
      projectApplication: { ...defaults.projectApplication, acceptsApplications: false },
    });

    await user.click(screen.getByRole('button', { name: 'Join' }));
    expect(screen.getByRole('button', { name: 'As a developer, Closed' })).toBeDisabled();
    await user.click(screen.getByRole('button', { name: 'As a tester' }));
    expect(screen.getByRole('heading', { name: 'Choose a session' })).toBeInTheDocument();
    expect(screen.queryByText('Online · Oct 4')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'In person · Oct 3' }));

    expect(screen.getByRole('heading', { name: 'Join as a tester' })).toBeInTheDocument();
    expect(screen.getByTestId('tester-enrollment')).toHaveAttribute('data-slot-id', 'slot-2');
    await user.click(screen.getByRole('button', { name: 'Back from tester form' }));
    expect(screen.getByRole('heading', { name: 'Choose a session' })).toBeInTheDocument();
  });

  it('opens the developer game-selection and application flow inside the dialog', async () => {
    const user = userEvent.setup();
    renderJoin();

    await user.click(screen.getByRole('button', { name: 'Join' }));
    await user.click(screen.getByRole('button', { name: 'As a developer' }));

    expect(screen.getByRole('heading', { name: 'Submit a game' })).toBeInTheDocument();
    expect(screen.getByTestId('developer-enrollment')).toHaveTextContent('Asterion');
    expect(screen.getByText('/testing-lab/events/event-1?joinAs=developer#join')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Back' }));
    expect(screen.getByRole('heading', { name: 'How would you like to join?' })).toBeInTheDocument();
  });

  it('reopens the selected enrollment step after sign-in', () => {
    renderJoin({ initialMode: 'tester', initialSlotId: 'slot-1' });

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Join as a tester' })).toBeInTheDocument();
    expect(screen.getByTestId('tester-enrollment')).toHaveAttribute('data-slot-id', 'slot-1');
  });

  it('lets a tester join the waitlist when a session has no seats left', async () => {
    const user = userEvent.setup();
    renderJoin({
      testerSessions: [{
        ...session('slot-full', 'Online · Oct 2'),
        slot: { id: 'slot-full', mode: 'Online', availableTesterCount: 0, maxTesters: 1 },
      }],
      projectApplication: { ...defaults.projectApplication, acceptsApplications: false },
    });

    await user.click(screen.getByRole('button', { name: 'Join' }));
    expect(screen.getByRole('button', { name: 'As a tester' })).toBeEnabled();
    await user.click(screen.getByRole('button', { name: 'As a tester' }));
    expect(screen.getByRole('heading', { name: 'Join as a tester' })).toBeInTheDocument();
    expect(screen.getByTestId('tester-enrollment')).toHaveAttribute('data-slot-id', 'slot-full');
  });

  it('reopens an existing developer submission even after applications close', () => {
    renderJoin({
      testerSessions: [session('slot-closed', 'Online · Oct 2', false)],
      projectApplication: {
        ...defaults.projectApplication,
        acceptsApplications: false,
        applications: [{ id: 'application-1', projectId: 'project-1', status: 'Approved' }],
      },
      initialMode: 'developer',
      initialProjectId: 'project-1',
      selectedApplicationId: 'application-1',
    });

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Submit a game' })).toBeInTheDocument();
    expect(screen.getByTestId('developer-enrollment')).toHaveAttribute('data-project-id', 'project-1');
    expect(screen.getByTestId('developer-enrollment')).toHaveAttribute('data-application-id', 'application-1');
  });

  it('disables the single Join action when both participation paths are closed', () => {
    renderJoin({
      testerSessions: [session('slot-closed', 'Online · Oct 2', false)],
      projectApplication: { ...defaults.projectApplication, acceptsApplications: false },
    });

    expect(screen.getByRole('button', { name: 'Sign-up closed' })).toBeDisabled();
    expect(screen.queryByRole('button', { name: 'Submit a game' })).not.toBeInTheDocument();
  });

  it('routes anonymous sign-in back to the selected developer flow', async () => {
    const user = userEvent.setup();
    renderJoin({ isAuthenticated: false });

    await user.click(screen.getByRole('button', { name: 'Join' }));
    await user.click(screen.getByRole('button', { name: 'As a developer' }));

    expect(screen.getByText('/testing-lab/events/event-1?joinAs=developer#join')).toBeInTheDocument();
  });
});
