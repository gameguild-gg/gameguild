import '@testing-library/jest-dom/vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import AccessSettingsPage from '@/app/[locale]/(private)/workspace/learning/courses/[course]/listing/access/page';
import DangerPage from '@/app/[locale]/(private)/workspace/learning/courses/[course]/settings/danger/page';
import { archiveCourse, deleteCourse, fetchCourse, transferCourseOwnership, updateCourse } from '@/lib/learning/actions';

const refreshMock = vi.fn();
const pushMock = vi.fn();

Object.defineProperties(HTMLElement.prototype, {
  hasPointerCapture: { value: vi.fn(() => false) },
  setPointerCapture: { value: vi.fn() },
  releasePointerCapture: { value: vi.fn() },
  scrollIntoView: { value: vi.fn() },
});

beforeAll(() => {
  global.ResizeObserver = class ResizeObserver {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
});

vi.mock('next/navigation', () => ({
  useRouter: () => ({
    refresh: refreshMock,
    push: pushMock,
  }),
}));

vi.mock('@/lib/learning/actions', () => ({
  archiveCourse: vi.fn(),
  deleteCourse: vi.fn(),
  fetchCourse: vi.fn(),
  transferCourseOwnership: vi.fn(),
  updateCourse: vi.fn(),
}));

const course = {
  id: 'course-1',
  title: 'Boss AI',
  description: 'Boss AI course',
  status: 'published',
  visibility: 'public',
  enrollmentStatus: 'Open',
  currentEnrollments: 4,
  maxEnrollments: 12,
  enrollmentDeadline: '2026-08-15T00:00:00.000Z',
  isEnrollmentOpen: true,
};

const params = Promise.resolve({ locale: 'en-US', course: 'boss-ai-by-instructor-one' });

describe('course settings client pages', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchCourse).mockResolvedValue(course as never);
    vi.mocked(updateCourse).mockResolvedValue({ success: true, data: null });
    vi.mocked(archiveCourse).mockResolvedValue({ success: true, data: null });
    vi.mocked(deleteCourse).mockResolvedValue({ success: true, data: null });
    vi.mocked(transferCourseOwnership).mockResolvedValue({ success: true, data: null });
  });

  it('saves access settings with unlimited enrollment cap semantics', async () => {
    const user = userEvent.setup();

    render(<AccessSettingsPage params={params} />);

    expect(await screen.findByText('Listing visibility')).toBeInTheDocument();
    await user.clear(screen.getByLabelText(/maximum enrollments/i));
    await user.type(screen.getByLabelText(/maximum enrollments/i), '0');
    await user.clear(screen.getByLabelText(/enrollment deadline/i));
    await user.type(screen.getByLabelText(/enrollment deadline/i), '2026-09-10');
    await user.click(screen.getByRole('button', { name: /save listing access/i }));

    await waitFor(() => {
      expect(updateCourse).toHaveBeenCalledWith({
        courseId: 'course-1',
        visibility: 'Public',
        enrollmentStatus: 'Open',
        maxEnrollments: null,
        enrollmentDeadline: '2026-09-10',
      });
    });
    expect(refreshMock).toHaveBeenCalled();
    expect(screen.getByText('Listing access settings saved successfully.')).toBeInTheDocument();
  });

  it('renders access API errors and course-not-found state', async () => {
    const user = userEvent.setup();
    vi.mocked(updateCourse).mockResolvedValueOnce({ success: false, error: 'Enrollment is closed.' });

    render(<AccessSettingsPage params={params} />);
    expect(await screen.findByText('Listing visibility')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /save listing access/i }));
    expect(await screen.findByText('Enrollment is closed.')).toBeInTheDocument();

    vi.mocked(fetchCourse).mockResolvedValueOnce(null);
    render(<AccessSettingsPage params={Promise.resolve({ locale: 'en-US', course: 'missing' })} />);
    expect(await screen.findByText('Course not found.')).toBeInTheDocument();
  });

  it('archives a course from the danger zone', async () => {
    const user = userEvent.setup();

    render(<DangerPage params={params} />);

    expect(await screen.findByText('Settings')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /archive course/i }));

    await waitFor(() => {
      expect(archiveCourse).toHaveBeenCalledWith('course-1');
    });
    expect(await screen.findByText('Archived successfully.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /archive course/i })).not.toBeInTheDocument();
    expect(refreshMock).toHaveBeenCalled();
  });

  it('requires exact title confirmation before deleting a course', async () => {
    const user = userEvent.setup();

    render(<DangerPage params={params} />);

    expect(await screen.findByText('Settings')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /^delete course$/i }));

    const deleteButton = screen.getByRole('button', { name: /permanently delete/i });
    expect(deleteButton).toBeDisabled();

    await user.type(screen.getByLabelText(/type boss ai to confirm deletion/i), 'Boss AI');
    expect(deleteButton).toBeEnabled();
    await user.click(deleteButton);

    await waitFor(() => {
      expect(deleteCourse).toHaveBeenCalledWith('course-1');
    });
    expect(pushMock).toHaveBeenCalledWith('/en-US/workspace/learning/courses');
  });

  it('renders danger-zone API errors without navigating away', async () => {
    const user = userEvent.setup();
    vi.mocked(archiveCourse).mockResolvedValueOnce({ success: false, error: 'Course has active sessions.' });

    render(<DangerPage params={params} />);

    expect(await screen.findByText('Settings')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /archive course/i }));

    expect(await screen.findByText('Course has active sessions.')).toBeInTheDocument();
    expect(pushMock).not.toHaveBeenCalled();
  });

  it('transfers ownership after confirming the course title', async () => {
    const user = userEvent.setup();

    render(<DangerPage params={params} />);

    expect(await screen.findByText('Transfer course ownership')).toBeInTheDocument();
    const transferButton = screen.getByRole('button', { name: /transfer ownership/i });
    expect(transferButton).toBeDisabled();

    await user.type(screen.getByLabelText(/new owner email/i), 'new-owner@gameguild.gg');
    await user.type(screen.getByLabelText(/type boss ai to confirm transfer/i), 'Boss AI');
    await user.click(transferButton);

    await waitFor(() => {
      expect(transferCourseOwnership).toHaveBeenCalledWith('course-1', 'new-owner@gameguild.gg');
    });
    expect(await screen.findByText('Course ownership was transferred.')).toBeInTheDocument();
  });
});
