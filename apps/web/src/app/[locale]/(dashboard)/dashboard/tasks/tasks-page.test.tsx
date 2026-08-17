import '@testing-library/jest-dom/vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getMyTasks: vi.fn(),
}));

vi.mock('@/lib/learning/queries/tasks', () => ({
  getMyTasks: mocks.getMyTasks,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({
    href,
    children,
    ...props
  }: {
    href: string;
    children: React.ReactNode;
  }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

import { getMyTasks } from '@/lib/learning/queries/tasks';
import TasksPage from './page';

async function renderPage() {
  render(await TasksPage());
}

const gradeTask = {
  type: 'grade',
  courseId: 'course-guid-1',
  courseTitle: 'Game AI Course',
  courseSlug: 'game-ai-course',
  assessmentId: 'assessment-1',
  assessmentTitle: 'Midterm Project',
  dueAt: '2026-09-01T12:00:00.000Z',
  countSubmitted: 3,
  reviewsCompleted: null,
  reviewsRequired: null,
};

const doTask = {
  type: 'do',
  courseId: 'course-guid-2',
  courseTitle: 'Level Design',
  courseSlug: 'level-design',
  assessmentId: 'assessment-2',
  assessmentTitle: 'Quiz 3',
  dueAt: '2026-08-20T12:00:00.000Z',
  countSubmitted: null,
  reviewsCompleted: null,
  reviewsRequired: null,
};

const reviewTask = {
  type: 'review',
  courseId: 'course-guid-3',
  courseTitle: 'Sound Design',
  courseSlug: 'sound-design',
  assessmentId: 'assessment-3',
  assessmentTitle: 'Peer Review Essay',
  dueAt: '2026-08-25T12:00:00.000Z',
  countSubmitted: null,
  reviewsCompleted: 1,
  reviewsRequired: 3,
};

describe('TasksPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders grade cards with submission counts and submissions links', async () => {
    getMyTasks.mockResolvedValue({
      ok: true,
      tasks: [gradeTask, doTask, reviewTask],
    });

    await renderPage();

    expect(getMyTasks).toHaveBeenCalledTimes(1);

    const gradeTab = screen.getByRole('tab', { name: 'To grade' });
    expect(gradeTab).toBeInTheDocument();

    const link = screen.getByRole('link', { name: /Midterm Project/ });
    expect(link).toHaveAttribute(
      'href',
      '/dashboard/learning/courses/game-ai-course/assessments/assessment-1/submissions',
    );
    expect(screen.getByText('Game AI Course')).toBeInTheDocument();
    expect(screen.getByText('3 submissions awaiting grading')).toBeInTheDocument();
  });

  it('shows review progress and links to /learn/reviews', async () => {
    getMyTasks.mockResolvedValue({
      ok: true,
      tasks: [doTask, reviewTask],
    });

    await renderPage();
    await userEvent.click(screen.getByRole('tab', { name: 'To review' }));

    const section = screen.getByRole('link', { name: /Peer Review Essay/ });
    expect(section).toHaveAttribute('href', '/learn/reviews');
    expect(screen.getByText('1 / 3 reviews completed')).toBeInTheDocument();
    expect(screen.getByText('Sound Design')).toBeInTheDocument();
  });

  it('links do tasks to the learn course page', async () => {
    getMyTasks.mockResolvedValue({
      ok: true,
      tasks: [doTask],
    });

    await renderPage();
    await userEvent.click(screen.getByRole('tab', { name: 'To do' }));

    const link = screen.getByRole('link', { name: /Quiz 3/ });
    expect(link).toHaveAttribute('href', '/learn/courses/level-design');
  });

  it('renders course title without a link when the slug is unresolved', async () => {
    getMyTasks.mockResolvedValue({
      ok: true,
      tasks: [{ ...gradeTask, courseSlug: undefined }],
    });

    await renderPage();

    expect(screen.getByText('Game AI Course')).toBeInTheDocument();
    const gradeSection = screen.getByText('Midterm Project');
    expect(
      gradeSection.closest('a'),
    ).toBeNull();
  });

  it('renders empty states per tab', async () => {
    getMyTasks.mockResolvedValue({ ok: true, tasks: [] });

    await renderPage();

    expect(screen.queryByRole('tab', { name: 'To grade' })).toBeNull();
    await userEvent.click(screen.getByRole('tab', { name: 'To do' }));
    expect(screen.getByText(/Nothing to do/)).toBeInTheDocument();
    await userEvent.click(screen.getByRole('tab', { name: 'To review' }));
    expect(screen.getByText(/No peer reviews/)).toBeInTheDocument();
  });

  it('renders an error card when the tasks fetch fails', async () => {
    getMyTasks.mockResolvedValue({
      ok: false,
      error: 'Tasks unavailable',
    });

    await renderPage();

    expect(screen.getByText('Tasks unavailable')).toBeInTheDocument();
    expect(screen.queryByRole('tab', { name: 'To do' })).toBeNull();
  });
});
