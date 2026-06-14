import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ContentReportService } from '@/lib/courses/services/content-report.service';
import { CourseContentViewer } from './course-content-viewer';

const mocks = vi.hoisted(() => ({
  getCourseLearningData: vi.fn(),
  markContentComplete: vi.fn(),
  submitActivity: vi.fn(),
}));

vi.mock('@/lib/courses/server-actions', () => ({
  getCourseLearningData: mocks.getCourseLearningData,
  markContentComplete: mocks.markContentComplete,
  submitActivity: mocks.submitActivity,
}));

const lessonCourseData = {
  id: 'course-1',
  title: 'Launch Production',
  description: 'Production course',
  overallProgress: 0,
  totalItems: 2,
  completedItems: 0,
  estimatedTimeToComplete: 2,
  currentItem: {
    id: 'lesson-1',
    title: 'Release Checklist',
    type: 'lesson',
    status: 'available',
    order: 1,
    isRequired: true,
    description: 'Prepare the launch checklist.',
    content: '<p>Checklist content.</p>',
  },
  modules: [
    {
      id: 'module-1',
      title: 'Module 1',
      description: 'Basics',
      order: 1,
      isLocked: false,
      progress: 0,
      items: [
        {
          id: 'lesson-1',
          title: 'Release Checklist',
          type: 'lesson',
          status: 'available',
          order: 1,
          isRequired: true,
          description: 'Prepare the launch checklist.',
          content: '<p>Checklist content.</p>',
        },
      ],
    },
  ],
};

const peerReviewCourseData = {
    id: 'course-1',
    title: 'Launch Production',
    description: 'Production course',
    overallProgress: 0,
    totalItems: 1,
    completedItems: 0,
    estimatedTimeToComplete: 2,
    currentItem: {
      id: 'peer-review-1',
      title: 'Portfolio Critique',
      type: 'peer-review',
      status: 'available',
      order: 1,
      isRequired: true,
      activityType: 'discussion',
      description: 'Review a classmate portfolio against the rubric.',
      content: JSON.stringify({
        prompt: 'Give actionable feedback on presentation, clarity, and production readiness.',
        criteria: [
          { name: 'clarity', description: 'Is the feedback clear and specific?' },
          { name: 'usefulness', description: 'Will the feedback help the creator improve?' },
        ],
      }),
    },
    modules: [
      {
        id: 'module-1',
        title: 'Module 1',
        description: 'Basics',
        order: 1,
        isLocked: false,
        progress: 0,
        items: [
          {
            id: 'peer-review-1',
            title: 'Portfolio Critique',
            type: 'peer-review',
            status: 'available',
            order: 1,
            isRequired: true,
            activityType: 'discussion',
            description: 'Review a classmate portfolio against the rubric.',
            content: JSON.stringify({
              prompt: 'Give actionable feedback on presentation, clarity, and production readiness.',
              criteria: [
                { name: 'clarity', description: 'Is the feedback clear and specific?' },
                { name: 'usefulness', description: 'Will the feedback help the creator improve?' },
              ],
            }),
          },
          {
            id: 'follow-up-lesson',
            title: 'Revise from critique',
            type: 'lesson',
            status: 'locked',
            order: 2,
            isRequired: true,
            description: 'Apply the critique before the next milestone.',
          },
        ],
      },
    ],
  };

vi.mock('@/lib/courses/services/content-report.service', () => ({
  ContentReportService: {
    createReport: vi.fn(),
  },
}));

describe('CourseContentViewer', () => {
  beforeEach(() => {
    mocks.getCourseLearningData.mockReset();
    mocks.markContentComplete.mockReset();
    mocks.submitActivity.mockReset();
    mocks.getCourseLearningData.mockResolvedValue(lessonCourseData);
    mocks.submitActivity.mockResolvedValue({ success: true, message: 'Activity submitted successfully.' });
    vi.mocked(ContentReportService.createReport).mockReset();
  });

  it('shows learner feedback after submitting a content report', async () => {
    vi.mocked(ContentReportService.createReport).mockResolvedValueOnce({
      success: true,
      message: 'Report submitted for moderation.',
      reportId: 'report-1',
    });

    render(<CourseContentViewer courseSlug="launch-production" />);

    await screen.findByRole('heading', { name: /release checklist/i });
    await userEvent.click(screen.getByRole('button', { name: /content actions/i }));
    await userEvent.click(screen.getByRole('menuitem', { name: /report content/i }));
    await userEvent.click(screen.getByLabelText(/technical issue/i));
    await userEvent.click(screen.getByRole('button', { name: /submit report/i }));

    await waitFor(() => {
      expect(ContentReportService.createReport).toHaveBeenCalledWith(
        expect.objectContaining({
          contentId: 'lesson-1',
          reason: 'technical',
        }),
      );
    });

    expect(await screen.findByRole('status')).toHaveTextContent(/report submitted/i);
  });

  it('renders peer-review content from the selected course item and submits it through the API action', async () => {
    mocks.getCourseLearningData.mockResolvedValueOnce(peerReviewCourseData);

    render(<CourseContentViewer courseSlug="launch-production" />);

    await screen.findByRole('heading', { name: /portfolio critique/i });
    expect(screen.getByText(/give actionable feedback on presentation/i)).toBeInTheDocument();
    expect(screen.queryByText(/game design document - rpg adventure/i)).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /start peer review/i }));
    await userEvent.click(screen.getByRole('button', { name: /rate clarity 4/i }));
    await userEvent.click(screen.getByRole('button', { name: /rate usefulness 5/i }));
    fireEvent.change(screen.getByLabelText(/written feedback/i), {
      target: {
        value: 'The portfolio has strong production value. Tighten the project captions and make the final role clearer.',
      },
    });
    await userEvent.click(screen.getByRole('button', { name: /submit peer review/i }));

    await waitFor(() => {
      expect(mocks.submitActivity).toHaveBeenCalledWith(
        expect.objectContaining({
          activityId: 'peer-review-1',
          courseId: 'course-1',
          activityType: 'discussion',
          isGraded: true,
        }),
      );
    });
    expect(await screen.findByRole('status')).toHaveTextContent(/peer review submitted/i);
  });
});
