import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { submitAssessment, submitContentActivity } from '@/lib/learner-activity-actions';
import { LearnerActivityForm } from './learner-activity-form';

vi.mock('@/lib/learner-activity-actions', () => ({ submitAssessment: vi.fn(), submitContentActivity: vi.fn() }));
vi.mock('next/navigation', () => ({ useRouter: () => ({ refresh: vi.fn() }) }));

describe('LearnerActivityForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(submitAssessment).mockResolvedValue({ success: true });
    vi.mocked(submitContentActivity).mockResolvedValue({ success: true });
  });

  it('submits a structured quiz answer through the assessment action', async () => {
    render(
      <LearnerActivityForm
        courseId="course-1"
        courseSlug="course"
        enrollmentId="enrollment-1"
        activity={{ kind: 'assessment', assessment: { id: 'quiz-1', title: 'Knowledge check', type: 'Quiz', submissionModalities: 'StructuredAnswer' } }}
      />,
    );
    await userEvent.type(screen.getByLabelText('Your answer'), 'A deterministic answer');
    await userEvent.click(screen.getByRole('button', { name: 'Submit assessment' }));
    expect(submitAssessment).toHaveBeenCalledOnce();
    const data = vi.mocked(submitAssessment).mock.calls[0]?.[0];
    expect(data?.get('modality')).toBe('StructuredAnswer');
    expect(data?.get('response')).toBe('A deterministic answer');
    expect(await screen.findByText('Submission received')).toBeInTheDocument();
  });

  it('submits a reflection through the typed content action', async () => {
    render(
      <LearnerActivityForm
        courseId="course-1"
        courseSlug="course"
        enrollmentId="enrollment-1"
        activity={{ kind: 'content', contentId: 'reflection-1', contentType: 'Reflection', title: 'Reflect' }}
      />,
    );
    await userEvent.type(screen.getByLabelText('Your reflection'), 'I changed my production process.');
    await userEvent.click(screen.getByRole('button', { name: 'Submit reflection' }));
    const data = vi.mocked(submitContentActivity).mock.calls[0]?.[0];
    expect(data?.get('kind')).toBe('reflection');
    expect(data?.get('response')).toBe('I changed my production process.');
  });
  it('blocks project submission until the learner owns a real project', () => {
    render(
      <LearnerActivityForm
        courseId="course-1"
        courseSlug="course"
        enrollmentId="enrollment-1"
        activity={{
          kind: 'assessment',
          assessment: { id: 'project-1', title: 'Portfolio project', type: 'Project', submissionModalities: 'Project' },
          projects: [],
        }}
      />,
    );

    expect(screen.getByText('Create a project before submitting')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Open projects' })).toHaveAttribute('href', 'http://localhost:3000/projects');
    expect(screen.getByRole('button', { name: 'Submit assessment' })).toBeDisabled();
    expect(submitAssessment).not.toHaveBeenCalled();
  });
});
