import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { completeCourseContent } from '@/lib/course-progress-actions';
import { CourseAttendanceShell } from './course-attendance-shell';

const refreshMock = vi.fn();

vi.mock('next/navigation', () => ({
    useRouter: () => ({
        refresh: refreshMock,
    }),
}));

vi.mock('@/lib/course-progress-actions', () => ({
    beginCourseContent: vi.fn(),
    completeCourseContent: vi.fn(),
}));

const course = {
    id: 'course-1',
    title: 'Portfolio Production',
    slug: 'portfolio-production',
    description: 'Build a portfolio through critique.',
    thumbnail: null,
    overallProgress: 0,
    totalItems: 1,
    completedItems: 0,
    remainingMinutes: 45,
    modules: [
        {
            id: 'module-1',
            title: 'Critique',
            description: 'Peer review module',
            order: 1,
            progress: 0,
            items: [
                {
                    id: 'peer-review-1',
                    title: 'Peer Portfolio Review',
                    type: 'peer-review',
                    status: 'in-progress',
                    duration: 45,
                    description: 'Review a peer portfolio submission.',
                    order: 1,
                    isRequired: true,
                    content: JSON.stringify({
                        prompt: 'Give actionable feedback on presentation, clarity, and production readiness.',
                        criteria: [
                            { name: 'clarity', description: 'Is the work easy to understand?' },
                            { name: 'usefulness', description: 'Will the feedback help the creator improve?' },
                        ],
                    }),
                },
            ],
        },
    ],
} as const;

describe('CourseAttendanceShell', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        vi.mocked(completeCourseContent).mockResolvedValue({ success: true });
    });

    it('renders a contextual peer-review submission flow and completes the item after submit', async () => {
        render(<CourseAttendanceShell course={course} />);

        expect(screen.getByRole('heading', { name: /peer portfolio review/i })).toBeInTheDocument();
        expect(screen.getByText(/give actionable feedback on presentation/i)).toBeInTheDocument();

        await userEvent.click(screen.getByRole('button', { name: /rate clarity 4/i }));
        await userEvent.click(screen.getByRole('button', { name: /rate usefulness 5/i }));
        fireEvent.change(screen.getByLabelText(/written feedback/i), {
            target: {
                value: 'The case study is polished. Add more process captions and make the production role easier to scan.',
            },
        });
        await userEvent.click(screen.getByRole('button', { name: /submit peer review/i }));

        await waitFor(() => {
            expect(completeCourseContent).toHaveBeenCalledWith('course-1', 'peer-review-1');
        });
        expect(refreshMock).toHaveBeenCalled();
    });
});
