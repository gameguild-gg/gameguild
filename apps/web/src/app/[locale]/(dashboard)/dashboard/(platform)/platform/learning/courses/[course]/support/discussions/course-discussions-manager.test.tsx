import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CourseDiscussionsManager } from './course-discussions-manager';

const createCourseDiscussionMock = vi.fn();
const updateDiscussionPinMock = vi.fn();
const resolveDiscussionMock = vi.fn();
const deleteDiscussionMock = vi.fn();
const refreshMock = vi.fn();

vi.mock('@/lib/learning/actions', () => ({
  createCourseDiscussion: (...args: unknown[]) => createCourseDiscussionMock(...args),
  updateDiscussionPin: (...args: unknown[]) => updateDiscussionPinMock(...args),
  resolveDiscussion: (...args: unknown[]) => resolveDiscussionMock(...args),
  deleteDiscussion: (...args: unknown[]) => deleteDiscussionMock(...args),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ refresh: refreshMock }),
}));

describe('CourseDiscussionsManager', () => {
  beforeEach(() => {
    createCourseDiscussionMock.mockReset();
    updateDiscussionPinMock.mockReset();
    resolveDiscussionMock.mockReset();
    deleteDiscussionMock.mockReset();
    refreshMock.mockReset();
    createCourseDiscussionMock.mockResolvedValue({ success: true, data: { id: 'thread-2' } });
    updateDiscussionPinMock.mockResolvedValue({ success: true, data: null });
    resolveDiscussionMock.mockResolvedValue({ success: true, data: null });
    deleteDiscussionMock.mockResolvedValue({ success: true, data: null });
  });

  it('creates a discussion through the dashboard form', async () => {
    render(<CourseDiscussionsManager courseId="course-1" courseTitle="Boss AI Production" threads={[]} />);

    fireEvent.change(screen.getByLabelText(/^title$/i), {
      target: { value: 'Milestone review question' },
    });
    fireEvent.change(screen.getByLabelText(/^content$/i), {
      target: { value: 'Can I submit a revised prototype after review?' },
    });
    fireEvent.click(screen.getByRole('button', { name: /create discussion/i }));

    await waitFor(() => {
      expect(createCourseDiscussionMock).toHaveBeenCalledWith({
        courseId: 'course-1',
        title: 'Milestone review question',
        content: 'Can I submit a revised prototype after review?',
      });
    });
    expect(refreshMock).toHaveBeenCalled();
    expect(screen.getByText('Discussion created.')).toBeInTheDocument();
  });

  it('pins existing discussion threads from the list', async () => {
    render(
      <CourseDiscussionsManager
        courseId="course-1"
        courseTitle="Boss AI Production"
        threads={[
          {
            id: 'thread-1',
            courseId: 'course-1',
            authorId: 'student-1',
            authorName: 'Student 1',
            title: 'Checkpoint question',
            content: 'How do checkpoints work?',
            pinned: false,
            locked: false,
            replyCount: 1,
            viewCount: 2,
            lastReplyAt: null,
            tags: [],
            createdAt: '2026-01-01T00:00:00.000Z',
            updatedAt: '2026-01-01T00:00:00.000Z',
          },
        ]}
      />,
    );

    expect(screen.getByRole('link', { name: /checkpoint question/i })).toHaveAttribute(
      'href',
      '/dashboard/platform/learning/courses/course-1/support/discussions/thread-1',
    );

    fireEvent.click(screen.getByRole('button', { name: /^pin$/i }));
    await waitFor(() => {
      expect(updateDiscussionPinMock).toHaveBeenCalledWith('course-1', 'thread-1', true);
    });
    await screen.findByText('Discussion pinned.');
  });

  it('resolves existing discussion threads from the list', async () => {
    render(
      <CourseDiscussionsManager
        courseId="course-1"
        courseTitle="Boss AI Production"
        threads={[
          {
            id: 'thread-1',
            courseId: 'course-1',
            authorId: 'student-1',
            authorName: 'Student 1',
            title: 'Checkpoint question',
            content: 'How do checkpoints work?',
            pinned: false,
            locked: false,
            replyCount: 1,
            viewCount: 2,
            lastReplyAt: null,
            tags: [],
            createdAt: '2026-01-01T00:00:00.000Z',
            updatedAt: '2026-01-01T00:00:00.000Z',
          },
        ]}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /^resolve$/i }));
    await waitFor(() => {
      expect(resolveDiscussionMock).toHaveBeenCalledWith('course-1', 'thread-1');
    });
    await screen.findByText('Discussion marked resolved.');
  });

  it('deletes existing discussion threads from the list', async () => {
    render(
      <CourseDiscussionsManager
        courseId="course-1"
        courseTitle="Boss AI Production"
        threads={[
          {
            id: 'thread-1',
            courseId: 'course-1',
            authorId: 'student-1',
            authorName: 'Student 1',
            title: 'Checkpoint question',
            content: 'How do checkpoints work?',
            pinned: false,
            locked: false,
            replyCount: 1,
            viewCount: 2,
            lastReplyAt: null,
            tags: [],
            createdAt: '2026-01-01T00:00:00.000Z',
            updatedAt: '2026-01-01T00:00:00.000Z',
          },
        ]}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /delete checkpoint question/i }));
    await waitFor(() => {
      expect(deleteDiscussionMock).toHaveBeenCalledWith('course-1', 'thread-1');
    });
  });
});
