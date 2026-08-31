import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/testing-lab/events-actions', () => ({
  submitTestingEventFeedback: vi.fn(),
}));

import { TestingFeedbackSubmission } from './testing-feedback-submission';

describe('TestingFeedbackSubmission', () => {
  it('renders one feedback form for each pending obligation', () => {
    render(
      <TestingFeedbackSubmission
        eventId="event-1"
        isAuthenticated
        obligations={[
          {
            id: 'obligation-1',
            applicationId: 'application-1',
            questionnaireRevisionId: '11111111-1111-1111-1111-111111111111',
            status: 'Pending',
            reviewPackage: {
              feedbackQuestionnaire: {
                title: 'Playtest feedback',
                questions: [{ id: 'clarity', prompt: 'What was clear?', type: 'FreeText', required: true, options: [] }],
              },
            },
          },
        ]}
      />,
    );

    expect(screen.getByLabelText(/what was clear/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/overall rating/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /submit required feedback/i })).toBeInTheDocument();
  });

  it('shows a completed state when no obligation remains pending', () => {
    render(
      <TestingFeedbackSubmission
        eventId="event-1"
        isAuthenticated
        obligations={[
          {
            id: 'obligation-1',
            applicationId: 'application-1',
            status: 'Fulfilled',
          },
        ]}
      />,
    );

    expect(screen.getByText(/all assigned feedback is complete/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /submit required feedback/i })).not.toBeInTheDocument();
  });
});
