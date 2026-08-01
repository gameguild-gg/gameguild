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
        projects={[]}
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
        projects={[{ id: 'project-1', title: 'Asterion' }]}
      />,
    );

    expect(screen.getByRole('option', { name: 'Asterion' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /submit project application/i })).toBeInTheDocument();
    expect(screen.getByText(/capacity is reserved only after approval/i)).toBeInTheDocument();
  });

  it('shows rejection rationale and does not render a second application form', () => {
    render(
      <TestingProjectApplication
        eventId="event-1"
        isAuthenticated
        acceptsApplications
        projects={[{ id: 'project-1', title: 'Asterion' }]}
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
