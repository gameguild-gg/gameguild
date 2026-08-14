import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/testing-lab/events-actions', () => ({
  submitTestingProjectApplication: vi.fn(),
  withdrawTestingProjectApplication: vi.fn(),
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

    expect(screen.getByRole('link', { name: /sign in to apply/i })).toHaveAttribute('href', '/sign-in');
  });

  it('submits an existing project without claiming capacity', () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[{ id: 'version-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'published' }]}
      />,
    );

    expect(screen.getByRole('option', { name: 'Asterion · 1.0.0 (published)' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /submit project application/i })).toBeInTheDocument();
    expect(screen.getByText(/capacity is reserved only after approval/i)).toBeInTheDocument();
  });

  it('preselects the Project carried from its Distribution workspace', () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        initialProjectId="project-2"
        projectVersions={[
          { id: 'version-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'published' },
          { id: 'version-2', projectId: 'project-2', projectTitle: 'Wayfinder', versionNumber: '2.0.0', status: 'testing' },
        ]}
      />,
    );

    expect(screen.getByRole('combobox', { name: /project version/i })).toHaveValue('version-2');
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

    expect(screen.getByRole('link', { name: /browse projects/i })).toHaveAttribute('href', '/projects');
  });

  it('shows rejection rationale and does not render a second application form', () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projectVersions={[{ id: 'version-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'published' }]}
        application={{
          id: 'application-1',
          status: 'Rejected',
          decisionRationale: 'The build is not playable yet.',
        }}
      />,
    );

    expect(screen.getByText('Rejected')).toBeInTheDocument();
    expect(screen.getByText('The build is not playable yet.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /submit project application/i })).not.toBeInTheDocument();
  });
});
