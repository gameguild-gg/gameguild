import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ClassDetailActions } from './class-detail-actions';

const updateCourseClassMock = vi.fn();
const updateCourseClassStatusMock = vi.fn();
const refreshMock = vi.fn();

vi.mock('@/lib/learning/actions', () => ({
  updateCourseClass: (...args: unknown[]) => updateCourseClassMock(...args),
  updateCourseClassStatus: (...args: unknown[]) => updateCourseClassStatusMock(...args),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ refresh: refreshMock }),
}));

const classDetail = {
  id: 'class-1',
  courseId: 'course-1',
  title: 'July Production Cohort',
  description: 'Live review.',
  status: 'scheduled',
  scheduledAt: '2026-07-14T14:00:00.000Z',
  duration: 120,
  maxAttendees: 20,
  attendeeCount: 0,
  location: { meetingUrl: 'https://meet.example.test/gameguild' },
  attendees: [],
} as const;

describe('ClassDetailActions', () => {
  beforeEach(() => {
    updateCourseClassMock.mockReset();
    updateCourseClassStatusMock.mockReset();
    refreshMock.mockReset();
    updateCourseClassMock.mockResolvedValue({ success: true, data: null });
    updateCourseClassStatusMock.mockResolvedValue({ success: true, data: null });
  });

  it('refreshes the route after saving class edits', async () => {
    render(<ClassDetailActions courseId="course-1" classDetail={classDetail} />);

    fireEvent.change(screen.getByLabelText(/^name$/i), { target: { value: 'August Production Cohort' } });
    fireEvent.click(screen.getByRole('button', { name: /save class/i }));

    await waitFor(() => {
      expect(updateCourseClassMock).toHaveBeenCalledWith(expect.objectContaining({
        courseId: 'course-1',
        classId: 'class-1',
        name: 'August Production Cohort',
      }));
    });
    expect(refreshMock).toHaveBeenCalledTimes(1);
    expect(screen.getByText('Class updated.')).toBeInTheDocument();
  });

  it('refreshes the route after lifecycle status changes', async () => {
    render(<ClassDetailActions courseId="course-1" classDetail={classDetail} />);

    fireEvent.click(screen.getByRole('button', { name: /open enrollment/i }));

    await waitFor(() => {
      expect(updateCourseClassStatusMock).toHaveBeenCalledWith('course-1', 'class-1', 'open');
    });
    expect(refreshMock).toHaveBeenCalledTimes(1);
    expect(screen.getByText('Class status updated.')).toBeInTheDocument();
  });
});
