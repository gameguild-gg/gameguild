import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/testing-lab/events-actions', () => ({
  saveTestingProjectApplicationDraft: vi.fn(),
  withdrawTestingProjectApplication: vi.fn(),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...props }: { children: ReactNode; href: string }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

import { TestingProjectApplication } from './testing-project-application';

describe('TestingProjectApplication', () => {
  it('directs anonymous visitors to sign in', () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated={false}
        acceptsApplications
        projectVersions={[]}
      />,
    );

    expect(screen.getByRole('link', { name: /sign in or create a free account/i })).toHaveAttribute(
      'href',
      '/sign-in?redirectTo=%2Ftesting-lab%2Fevents%2Fevent-1%23join',
    );
  });

  it('returns an anonymous developer to the open project step after signing in', () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated={false}
        acceptsApplications
        projectVersions={[]}
        signInReturnUrl="/testing-lab/events/event-1?joinAs=developer#join"
      />,
    );

    expect(screen.getByRole('link', { name: /sign in or create a free account/i })).toHaveAttribute(
      'href',
      '/sign-in?redirectTo=%2Ftesting-lab%2Fevents%2Fevent-1%3FjoinAs%3Ddeveloper%23join',
    );
  });

  it('presents eligible builds as explicit game choices and requires a selection to continue', async () => {
    const user = userEvent.setup();
    const { container } = render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[{ id: 'version-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'ReadyForTesting' }]}
      />,
    );

    const build = screen.getByRole('radio', { name: 'Asterion · 1.0.0 · Ready for testing' });
    const continueButton = screen.getByRole('button', { name: /continue/i });
    expect(screen.getByRole('heading', { name: 'Choose your game' })).toBeInTheDocument();
    expect(continueButton).toBeDisabled();
    await user.click(build);
    expect(build).toBeChecked();
    expect(continueButton).toBeEnabled();
    expect(container.querySelector('p[aria-live="polite"]')).toHaveTextContent('Asterion · 1.0.0');
  });

  it('preselects the Project carried from its Distribution workspace', () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        initialProjectId="project-2"
        projectVersions={[
          { id: 'version-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'Released' },
          { id: 'version-2', projectId: 'project-2', projectTitle: 'Wayfinder', versionNumber: '2.0.0', status: 'ReadyForTesting' },
        ]}
      />,
    );

    expect(screen.getByRole('radio', { name: 'Wayfinder · 2.0.0 · Ready for testing' })).toBeChecked();
  });

  it('groups multiple eligible builds under one game and excludes drafts', () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[
          { id: 'version-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'ReadyForTesting' },
          { id: 'version-2', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.1.0', status: 'Released' },
          { id: 'version-draft', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.2.0', status: 'Draft' },
          { id: 'version-3', projectId: 'project-2', projectTitle: 'Mothlight', versionNumber: '0.8.0', status: 'ReadyForTesting' },
        ]}
      />,
    );

    expect(screen.getAllByRole('heading', { name: 'Asterion' })).toHaveLength(1);
    expect(screen.getByText('2 eligible builds')).toBeInTheDocument();
    expect(screen.getByRole('radio', { name: 'Asterion · 1.0.0 · Ready for testing' })).toBeInTheDocument();
    expect(screen.getByRole('radio', { name: 'Asterion · 1.1.0 · Released' })).toBeInTheDocument();
    expect(screen.queryByRole('radio', { name: /1\.2\.0/ })).not.toBeInTheDocument();
  });

  it('links users without projects to the real project directory', () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[]}
      />,
    );

    expect(screen.getByText('No test-ready builds available')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /open your projects/i })).toHaveAttribute('href', '/projects');
  });

  it('shows rejection rationale while still allowing another accessible Project to apply', () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[
          { id: 'version-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'Released' },
          { id: 'version-2', projectId: 'project-2', projectTitle: 'Wayfinder', versionNumber: '2.0.0', status: 'ReadyForTesting' },
        ]}
        initialProjectId="project-1"
        applications={[{
          id: 'application-1',
          projectId: 'project-1',
          status: 'Rejected',
          decisionRationale: 'The build is not playable yet.',
        }]}
      />,
    );

    expect(screen.getByText('Rejected')).toBeInTheDocument();
    expect(screen.getByText('The build is not playable yet.')).toBeInTheDocument();
    expect(screen.getByRole('radio', { name: 'Wayfinder · 2.0.0 · Ready for testing' })).toBeInTheDocument();
  });

  it('allows an authorized Project member to update an active application', () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[{ id: 'version-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'ReadyForTesting' }]}
        initialProjectId="project-1"
        applications={[{ id: 'application-1', projectId: 'project-1', projectVersionId: 'version-1', status: 'Pending' }]}
      />,
    );

    expect(screen.getByText('Pending')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^continue$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /withdraw application/i })).toBeInTheDocument();
  });

  it('keeps the mounted wizard available when a server action refresh omits private route data', () => {
    const { rerender } = render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[{ id: 'version-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'ReadyForTesting' }]}
      />,
    );

    rerender(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated={false}
        acceptsApplications
        projectVersions={[]}
      />,
    );

    expect(screen.getByRole('radio', { name: 'Asterion · 1.0.0 · Ready for testing' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /continue/i })).toBeInTheDocument();
  });
});
