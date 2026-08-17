import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ThreadActionPanel } from './thread-action-panel';

const createDiscussionReplyMock = vi.fn();
const updateDiscussionPinMock = vi.fn();
const resolveDiscussionMock = vi.fn();
const acceptDiscussionReplyMock = vi.fn();
const upvoteDiscussionReplyMock = vi.fn();
const refreshMock = vi.fn();

vi.mock('@/lib/learning/actions', () => ({
  createDiscussionReply: (...args: unknown[]) => createDiscussionReplyMock(...args),
  updateDiscussionPin: (...args: unknown[]) => updateDiscussionPinMock(...args),
  resolveDiscussion: (...args: unknown[]) => resolveDiscussionMock(...args),
  acceptDiscussionReply: (...args: unknown[]) => acceptDiscussionReplyMock(...args),
  upvoteDiscussionReply: (...args: unknown[]) => upvoteDiscussionReplyMock(...args),
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/workspace/learning',
  useRouter: () => ({ refresh: refreshMock }),
}));

describe('ThreadActionPanel', () => {
  beforeEach(() => {
    createDiscussionReplyMock.mockReset();
    updateDiscussionPinMock.mockReset();
    resolveDiscussionMock.mockReset();
    acceptDiscussionReplyMock.mockReset();
    upvoteDiscussionReplyMock.mockReset();
    refreshMock.mockReset();
    createDiscussionReplyMock.mockResolvedValue({ success: true, data: { id: 'reply-2' } });
    updateDiscussionPinMock.mockResolvedValue({ success: true, data: null });
    resolveDiscussionMock.mockResolvedValue({ success: true, data: null });
    acceptDiscussionReplyMock.mockResolvedValue({ success: true, data: null });
    upvoteDiscussionReplyMock.mockResolvedValue({ success: true, data: null });
  });

  it('posts replies and refreshes the discussion thread', async () => {
    render(<ThreadActionPanel courseId="course-1" threadId="thread-1" pinned={false} resolved={false} replies={[]} />);

    fireEvent.change(screen.getByLabelText(/^reply$/i), {
      target: { value: 'Use the updated milestone rubric before resubmitting.' },
    });
    fireEvent.click(screen.getByRole('button', { name: /post reply/i }));

    await waitFor(() => {
      expect(createDiscussionReplyMock).toHaveBeenCalledWith({
        courseId: 'course-1',
        discussionId: 'thread-1',
        content: 'Use the updated milestone rubric before resubmitting.',
      });
    });
    expect(refreshMock).toHaveBeenCalled();
    expect(screen.getByText('Reply posted.')).toBeInTheDocument();
  });

  it('pins and resolves discussion threads from the action panel', async () => {
    render(<ThreadActionPanel courseId="course-1" threadId="thread-1" pinned={false} resolved={false} replies={[]} />);

    fireEvent.click(screen.getByRole('button', { name: /^pin$/i }));
    await waitFor(() => {
      expect(updateDiscussionPinMock).toHaveBeenCalledWith('course-1', 'thread-1', true);
    });
    await screen.findByText('Discussion pinned.');
  });

  it('resolves discussion threads from the action panel', async () => {
    render(<ThreadActionPanel courseId="course-1" threadId="thread-1" pinned={false} resolved={false} replies={[]} />);

    fireEvent.click(screen.getByRole('button', { name: /^resolve$/i }));
    await waitFor(() => {
      expect(resolveDiscussionMock).toHaveBeenCalledWith('course-1', 'thread-1');
    });
    await screen.findByText('Discussion marked resolved.');
  });

  it('accepts and upvotes replies from the action panel', async () => {
    render(
      <ThreadActionPanel
        courseId="course-1"
        threadId="thread-1"
        pinned={false}
        resolved={false}
        replies={[
          {
            id: 'reply-1',
            threadId: 'thread-1',
            authorId: 'student-1',
            authorName: 'Student 1',
            authorRole: 'student',
            content: 'This helped.',
            upvotes: 3,
            isAnswer: false,
            createdAt: '2026-01-01T00:00:00.000Z',
            updatedAt: '2026-01-01T00:00:00.000Z',
          },
        ]}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /^accept$/i }));
    await waitFor(() => {
      expect(acceptDiscussionReplyMock).toHaveBeenCalledWith('course-1', 'thread-1', 'reply-1');
    });
    await screen.findByText('Answer accepted.');
  });

  it('upvotes replies from the action panel', async () => {
    render(
      <ThreadActionPanel
        courseId="course-1"
        threadId="thread-1"
        pinned={false}
        resolved={false}
        replies={[
          {
            id: 'reply-1',
            threadId: 'thread-1',
            authorId: 'student-1',
            authorName: 'Student 1',
            authorRole: 'student',
            content: 'This helped.',
            upvotes: 3,
            isAnswer: false,
            createdAt: '2026-01-01T00:00:00.000Z',
            updatedAt: '2026-01-01T00:00:00.000Z',
          },
        ]}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /^upvote$/i }));
    await waitFor(() => {
      expect(upvoteDiscussionReplyMock).toHaveBeenCalledWith('course-1', 'thread-1', 'reply-1');
    });
  });
});
