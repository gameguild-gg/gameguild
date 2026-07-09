import '@testing-library/jest-dom/vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CourseLifecycleActions } from './course-lifecycle-actions';
import { archiveCourse, deleteCourse, publishCourse, unpublishCourse } from '@/lib/learning/actions';

const refreshMock = vi.fn();
const pushMock = vi.fn();

vi.mock('next/navigation', () => ({
  useRouter: () => ({
    refresh: refreshMock,
    push: pushMock,
  }),
}));

vi.mock('@/lib/learning/actions', () => ({
  archiveCourse: vi.fn(),
  deleteCourse: vi.fn(),
  publishCourse: vi.fn(),
  unpublishCourse: vi.fn(),
}));

describe('CourseLifecycleActions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(archiveCourse).mockResolvedValue({ success: true, data: null });
    vi.mocked(deleteCourse).mockResolvedValue({ success: true, data: null });
    vi.mocked(publishCourse).mockResolvedValue({ success: true, data: null });
    vi.mocked(unpublishCourse).mockResolvedValue({ success: true, data: null });
  });

  it('publishes draft courses and refreshes the dashboard', async () => {
    const user = userEvent.setup();

    render(<CourseLifecycleActions courseId="course-1" status="draft" locale="en-US" />);

    expect(screen.getByText('Draft')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /publish/i }));

    await waitFor(() => {
      expect(publishCourse).toHaveBeenCalledWith('course-1');
    });
    expect(refreshMock).toHaveBeenCalled();
  });

  it('unpublishes and archives published courses', async () => {
    const user = userEvent.setup();

    render(<CourseLifecycleActions courseId="course-1" status="published" locale="en-US" />);

    await user.click(screen.getByRole('button', { name: /unpublish/i }));
    await waitFor(() => {
      expect(unpublishCourse).toHaveBeenCalledWith('course-1');
    });

    await user.click(screen.getByRole('button', { name: /^archive$/i }));
    await waitFor(() => {
      expect(archiveCourse).toHaveBeenCalledWith('course-1');
    });
  });

  it('republishes archived courses', async () => {
    const user = userEvent.setup();

    render(<CourseLifecycleActions courseId="course-1" status="archived" locale="en-US" />);

    expect(screen.getByText('Archived')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /re-publish/i }));

    await waitFor(() => {
      expect(publishCourse).toHaveBeenCalledWith('course-1');
    });
  });

  it('requires a second click before deleting and then routes back to courses', async () => {
    const user = userEvent.setup();

    render(<CourseLifecycleActions courseId="course-1" status="draft" locale="en-US" />);

    await user.click(screen.getByRole('button', { name: '' }));
    await user.click(screen.getByRole('button', { name: /confirm delete/i }));

    await waitFor(() => {
      expect(deleteCourse).toHaveBeenCalledWith('course-1');
    });
    expect(pushMock).toHaveBeenCalledWith('/en-US/dashboard/learning/courses');
  });

  it('shows action errors without navigating away', async () => {
    const user = userEvent.setup();
    vi.mocked(publishCourse).mockResolvedValueOnce({ success: false, error: 'Course is missing lessons.' });

    render(<CourseLifecycleActions courseId="course-1" status="draft" locale="en-US" />);

    await user.click(screen.getByRole('button', { name: /publish/i }));

    expect(await screen.findByText('Course is missing lessons.')).toBeInTheDocument();
    expect(pushMock).not.toHaveBeenCalled();
  });
});
