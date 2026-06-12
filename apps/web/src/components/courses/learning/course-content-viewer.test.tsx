import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ContentReportService } from '@/lib/courses/services/content-report.service';
import { CourseContentViewer } from './course-content-viewer';

vi.mock('@/lib/courses/server-actions', () => ({
  getCourseLearningData: vi.fn(async () => ({
    id: 'course-1',
    title: 'Launch Production',
    description: 'Production course',
    overallProgress: 0,
    totalItems: 1,
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
  })),
  markContentComplete: vi.fn(),
}));

vi.mock('@/lib/courses/services/content-report.service', () => ({
  ContentReportService: {
    createReport: vi.fn(),
  },
}));

describe('CourseContentViewer', () => {
  beforeEach(() => {
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
});
