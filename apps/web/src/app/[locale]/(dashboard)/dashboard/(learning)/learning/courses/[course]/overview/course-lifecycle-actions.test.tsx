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

  it('publishes draft courses without blocking on a dashboard refresh', async () => {
    const user = userEvent.setup();

    render(<CourseLifecycleActions courseId="course-1" status="draft" locale="en-US" />);

    expect(screen.getByText('Draft')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /publish/i }));

    await waitFor(() => {
      expect(publishCourse).toHaveBeenCalledWith('course-1');
    });
    expect(await screen.findByText('Published')).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /unpublish/i })).toBeEnabled();
    });
    expect(refreshMock).not.toHaveBeenCalled();
  });

  it('unpublishes published courses and updates the lifecycle controls', async () => {
    const user = userEvent.setup();

    render(<CourseLifecycleActions courseId="course-1" status="published" locale="en-US" />);

    await user.click(screen.getByRole('button', { name: /unpublish/i }));
    await waitFor(() => {
      expect(unpublishCourse).toHaveBeenCalledWith('course-1');
    });
    expect(await screen.findByText('Draft')).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /^publish$/i })).toBeEnabled();
    });
    expect(refreshMock).not.toHaveBeenCalled();
  });

  it('archives published courses and updates the lifecycle controls', async () => {
    const user = userEvent.setup();

    render(<CourseLifecycleActions courseId="course-1" status="published" locale="en-US" />);

    await user.click(screen.getByRole('button', { name: /^archive$/i }));
    await waitFor(() => {
      expect(archiveCourse).toHaveBeenCalledWith('course-1');
    });
    expect(await screen.findByText('Archived')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /re-publish/i })).toBeInTheDocument();
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

  it('keeps every lifecycle action available across a complete status sequence', async () => {
    const user = userEvent.setup();

    render(<CourseLifecycleActions courseId="course-1" status="published" locale="en-US" />);

    await user.click(screen.getByRole('button', { name: /unpublish/i }));
    const publishButton = await screen.findByRole('button', { name: /^publish$/i });
    await waitFor(() => expect(publishButton).toBeEnabled());

    await user.click(publishButton);
    const archiveButton = await screen.findByRole('button', { name: /^archive$/i });
    await waitFor(() => expect(archiveButton).toBeEnabled());

    await user.click(archiveButton);
    const republishButton = await screen.findByRole('button', { name: /re-publish/i });
    await waitFor(() => expect(republishButton).toBeEnabled());

    await user.click(republishButton);
    await waitFor(() => expect(screen.getByRole('button', { name: /unpublish/i })).toBeEnabled());

    expect(unpublishCourse).toHaveBeenCalledTimes(1);
    expect(publishCourse).toHaveBeenCalledTimes(2);
    expect(archiveCourse).toHaveBeenCalledTimes(1);
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
