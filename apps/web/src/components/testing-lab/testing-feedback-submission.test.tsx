import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ submit: vi.fn() }));

vi.mock('@/lib/testing-lab/events-actions', () => ({
  submitTestingEventFeedback: mocks.submit,
}));

import { TestingFeedbackSubmission } from './testing-feedback-submission';

describe('TestingFeedbackSubmission', () => {
  beforeEach(() => vi.clearAllMocks());

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

  it('explains when no feedback has been assigned', () => {
    render(<TestingFeedbackSubmission eventId="event-1" isAuthenticated obligations={[]} />);
    expect(screen.getByText(/No project feedback is assigned/)).toBeInTheDocument();
  });

  it('requires authentication when pending feedback exists', () => {
    render(
      <TestingFeedbackSubmission
        eventId="event-1"
        isAuthenticated={false}
        obligations={[{ id: 'obligation-1', status: 'Pending' }]}
      />,
    );
    expect(screen.getByRole('link', { name: 'Sign in to submit feedback' })).toHaveAttribute('href', '/sign-in');
  });

  it('renders plural pending feedback count and stable application fallback keys', () => {
    render(
      <TestingFeedbackSubmission
        eventId="event-1"
        isAuthenticated
        obligations={[
          { applicationId: 'application-1', status: 'Pending' },
          { applicationId: 'application-2', status: 'Pending' },
        ]}
      />,
    );
    expect(screen.getByText('2 required feedback submissions remain')).toBeInTheDocument();
    expect(screen.getAllByText('Assigned project feedback')).toHaveLength(2);
    expect(screen.getAllByRole('button', { name: 'Submit required feedback' }).every((button) => button.hasAttribute('disabled'))).toBe(true);
  });

  it('presents the immutable project brief and only accessible assets', () => {
    render(
      <TestingFeedbackSubmission
        eventId="event-1"
        isAuthenticated
        obligations={[
          {
            id: 'obligation-1',
            questionnaireRevisionId: 'abcdefgh-1111',
            status: 'Pending',
            reviewPackage: {
              versionNumber: '2.0.0',
              brief: {
                testObjective: 'Verify onboarding.',
                installationAndAccess: 'Install the signed build.',
                testTasks: ['Create a profile', 'Finish the tutorial'],
                controls: 'Keyboard and mouse',
                knownLimitations: 'Audio is incomplete',
              },
              assets: [
                { assetReferenceId: 'asset-1', accessUrl: 'https://example.com/build', displayName: 'Windows build' },
                { assetReferenceId: 'asset-2', accessUrl: 'https://example.com/notes', displayName: '' },
                { assetReferenceId: 'asset-3', accessUrl: null, displayName: 'Unavailable' },
              ],
            },
          },
        ]}
      />,
    );
    expect(screen.getByText('Version 2.0.0')).toBeInTheDocument();
    expect(screen.getByText('Verify onboarding.')).toBeInTheDocument();
    expect(screen.getByRole('list')).toHaveTextContent('Create a profile');
    expect(screen.getByRole('link', { name: 'Windows build' })).toHaveAttribute('href', 'https://example.com/build');
    expect(screen.getByRole('link', { name: 'Open asset' })).toHaveAttribute('href', 'https://example.com/notes');
    expect(screen.queryByText('Unavailable')).not.toBeInTheDocument();
    expect(screen.getByText(/revision abcdefgh/)).toBeInTheDocument();
  });

  it('supports a brief without tasks', () => {
    render(
      <TestingFeedbackSubmission
        eventId="event-1"
        isAuthenticated
        obligations={[{
          id: 'obligation-1',
          status: 'Pending',
          reviewPackage: {
            brief: {
              testObjective: 'Explore.',
              installationAndAccess: 'Open browser.',
              testTasks: null,
              controls: 'Mouse',
              knownLimitations: 'None',
            },
          },
        }]}
      />,
    );
    expect(screen.getByRole('list')).toBeEmptyDOMElement();
  });

  it('submits questionnaire, rating, recommendation, and notes', async () => {
    const user = userEvent.setup();
    mocks.submit.mockResolvedValueOnce({ success: true, message: 'Feedback submitted.' });
    render(
      <TestingFeedbackSubmission
        eventId="event-1"
        isAuthenticated
        obligations={[{
          id: 'obligation-1',
          questionnaireRevisionId: 'revision-1',
          status: 'Pending',
          reviewPackage: {
            feedbackQuestionnaire: {
              title: 'Feedback',
              questions: [{ id: 'clarity', prompt: 'What was clear?', type: 'FreeText', required: true, options: [] }],
            },
          },
        }]}
      />,
    );
    const submit = screen.getByRole('button', { name: 'Submit required feedback' });
    expect(submit).toBeDisabled();
    await user.type(screen.getByLabelText('What was clear?'), 'The tutorial');
    await user.click(screen.getByRole('button', { name: 'Confirm questionnaire answers' }));
    await user.type(screen.getByLabelText(/Overall rating/), '9');
    await user.selectOptions(screen.getByLabelText('Would you recommend it?'), 'yes');
    await user.type(screen.getByLabelText(/Additional observations/), 'Great onboarding.');
    expect(submit).toBeEnabled();
    await user.click(submit);

    await waitFor(() => expect(mocks.submit).toHaveBeenCalledOnce());
    expect(Object.fromEntries((mocks.submit.mock.calls[0]![0] as FormData).entries())).toEqual({
      eventId: 'event-1',
      obligationId: 'obligation-1',
      questionnaireRevisionId: 'revision-1',
      responsesJson: '{"answers":[{"questionId":"clarity","textValue":"The tutorial","selectedOptionIds":[]}]}',
      overallRating: '9',
      wouldRecommend: 'true',
      additionalNotes: 'Great onboarding.',
    });
    expect(await screen.findByText('Feedback submitted.')).toBeInTheDocument();
  });

  it('submits a negative recommendation and renders server validation', async () => {
    const user = userEvent.setup();
    mocks.submit.mockResolvedValueOnce({ success: false, error: 'Rating rejected.' });
    render(
      <TestingFeedbackSubmission
        eventId="event-1"
        isAuthenticated
        obligations={[{ id: 'obligation-1', status: 'Pending' }]}
      />,
    );
    await user.click(screen.getByRole('button', { name: 'Confirm questionnaire answers' }));
    await user.type(screen.getByLabelText(/Overall rating/), '3');
    await user.selectOptions(screen.getByLabelText('Would you recommend it?'), 'no');
    await user.click(screen.getByRole('button', { name: 'Submit required feedback' }));
    expect(await screen.findByText('Rating rejected.')).toBeInTheDocument();
    const submitted = mocks.submit.mock.calls[0]![0] as FormData;
    expect(submitted.get('wouldRecommend')).toBe('false');
    expect(submitted.get('questionnaireRevisionId')).toBe('');
  });

  it.each([
    [new Error('Feedback service offline'), 'Feedback service offline'],
    ['failure', 'The Testing Lab operation failed.'],
  ])('turns thrown feedback actions into visible errors', async (failure, message) => {
    const user = userEvent.setup();
    mocks.submit.mockRejectedValueOnce(failure);
    render(
      <TestingFeedbackSubmission
        eventId="event-1"
        isAuthenticated
        obligations={[{ id: 'obligation-1', status: 'Pending' }]}
      />,
    );
    await user.click(screen.getByRole('button', { name: 'Confirm questionnaire answers' }));
    await user.type(screen.getByLabelText(/Overall rating/), '5');
    await user.selectOptions(screen.getByLabelText('Would you recommend it?'), 'yes');
    await user.click(screen.getByRole('button', { name: 'Submit required feedback' }));
    expect(await screen.findByText(message)).toBeInTheDocument();
  });
});
