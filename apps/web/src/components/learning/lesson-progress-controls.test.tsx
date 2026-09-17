import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ComponentProps, ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ begin: vi.fn(), complete: vi.fn(), refresh: vi.fn() }));
vi.mock('@/lib/learner/progress-actions', () => ({
  beginCourseContent: mocks.begin,
  completeCourseContent: mocks.complete,
}));
vi.mock('next/navigation', () => ({ useRouter: () => ({ refresh: mocks.refresh }) }));
vi.mock('@game-guild/ui/components/button', () => ({
  Button: ({ children, ...props }: ComponentProps<'button'> & { children: ReactNode }) => <button {...props}>{children}</button>,
}));

import { LessonProgressControls } from './lesson-progress-controls';

describe('LessonProgressControls', () => {
  beforeEach(() => vi.clearAllMocks());

  it('starts an available lesson and refreshes persisted progress', async () => {
    mocks.begin.mockResolvedValue({ success: true });
    render(<LessonProgressControls courseId="course-1" contentId="content-1" status="available" />);
    fireEvent.click(screen.getByRole('button', { name: /Start lesson/ }));
    await waitFor(() => expect(mocks.refresh).toHaveBeenCalledOnce());
    expect(mocks.begin).toHaveBeenCalledWith('course-1', 'content-1');
  });

  it('keeps both actions disabled while a completion is pending', async () => {
    const pending = Promise.withResolvers<{ success: boolean }>();
    mocks.complete.mockReturnValue(pending.promise);
    render(<LessonProgressControls courseId="course-1" contentId="content-1" status="available" />);
    fireEvent.click(screen.getByRole('button', { name: /Mark complete/ }));

    expect(await screen.findByRole('button', { name: /Starting/ })).toBeDisabled();
    expect(screen.getByRole('button', { name: /Saving/ })).toBeDisabled();
    pending.resolve({ success: true });
    await waitFor(() => expect(mocks.refresh).toHaveBeenCalledOnce());
  });

  it('shows action failures and allows another attempt', async () => {
    mocks.complete.mockResolvedValue({ success: false, error: 'Completion requirements are not met.' });
    render(<LessonProgressControls courseId="course-1" contentId="content-1" status="in-progress" />);
    fireEvent.click(screen.getByRole('button', { name: /Mark complete/ }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Completion requirements are not met.');
    expect(screen.getByRole('button', { name: /Mark complete/ })).toBeEnabled();
    expect(screen.queryByRole('button', { name: /Start lesson/ })).not.toBeInTheDocument();
  });

  it.each([
    [new Error('Progress API unavailable.'), 'Progress API unavailable.'],
    ['offline', 'Unable to update lesson progress.'],
  ])('normalizes unexpected progress failures', async (error, expected) => {
    mocks.begin.mockRejectedValue(error);
    render(<LessonProgressControls courseId="course-1" contentId="content-1" status="available" />);
    fireEvent.click(screen.getByRole('button', { name: /Start lesson/ }));
    expect(await screen.findByRole('alert')).toHaveTextContent(expected);
    expect(screen.getByRole('button', { name: /Start lesson/ })).toBeEnabled();
  });

  it('renders terminal and locked states without mutations', () => {
    const { rerender } = render(<LessonProgressControls courseId="course-1" contentId="content-1" status="completed" />);
    expect(screen.getByText('Completed')).toBeInTheDocument();
    expect(screen.queryByRole('button')).not.toBeInTheDocument();

    rerender(<LessonProgressControls courseId="course-1" contentId="content-1" status="locked" />);
    expect(screen.queryByText('Completed')).not.toBeInTheDocument();
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
