import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createCourseDiscussion } from '@/lib/learner-activity-actions';
import { CourseCommunity } from './course-community';

vi.mock('@/lib/learner-activity-actions', () => ({ createCourseDiscussion: vi.fn() }));
vi.mock('next/navigation', () => ({ useRouter: () => ({ refresh: vi.fn() }) }));

describe('CourseCommunity', () => {
    beforeEach(() => { vi.clearAllMocks(); vi.mocked(createCourseDiscussion).mockResolvedValue({ success: true }); });

    it('renders persisted discussions and creates a new course thread', async () => {
        render(<CourseCommunity courseId="course-1" courseSlug="course" courseTitle="Game Production" discussions={[{ id: 'discussion-1', courseId: 'course-1', title: 'Critique exchange', content: 'Share the feedback you need.', replyCount: 3, createdAt: '2026-08-01T12:00:00Z' }]} />);
        expect(screen.getByText('Critique exchange')).toBeInTheDocument();
        await userEvent.click(screen.getByRole('button', { name: 'Start discussion' }));
        await userEvent.type(screen.getByLabelText('Title'), 'Playtest coordination');
        await userEvent.type(screen.getByLabelText('Message'), 'Who is available to test this week?');
        await userEvent.click(screen.getByRole('button', { name: 'Publish discussion' }));
        expect(createCourseDiscussion).toHaveBeenCalledOnce();
        expect(await screen.findByText('Discussion published')).toBeInTheDocument();
    });
});