import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

const mockAuth = vi.fn();
const mockGetCourseAttendanceData = vi.fn();
const mockRedirect = vi.fn((target: string) => {
    throw new Error(`REDIRECT:${target}`);
});
const mockNotFound = vi.fn(() => {
    throw new Error('NOT_FOUND');
});

vi.mock('@/auth', () => ({
    auth: () => mockAuth(),
}));

vi.mock('@/lib/courses', () => ({
    getCourseAttendanceData: (...args: unknown[]) => mockGetCourseAttendanceData(...args),
}));

vi.mock('@/components/course-attendance-shell', () => ({
    CourseAttendanceShell: ({ course }: { course: { title: string } }) => (
        <div data-testid="course-attendance-shell">{course.title}</div>
    ),
}));

vi.mock('next/navigation', () => ({
    redirect: (target: string) => mockRedirect(target),
    notFound: () => mockNotFound(),
}));

const { default: CourseContentPage } = await import('./page');

describe('CourseContentPage', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('redirects unauthenticated learners to sign-in with the classroom target', async () => {
        mockAuth.mockResolvedValue(null);

        await expect(
            CourseContentPage({ params: Promise.resolve({ slug: 'intro-to-rpg' }) })
        ).rejects.toThrow(
            'REDIRECT:/sign-in?redirectTo=%2Fcourses%2Fintro-to-rpg%2Fcontent'
        );
    });

    it('renders the classroom shell for authenticated learners', async () => {
        mockAuth.mockResolvedValue({ user: { id: 'user-1' } });
        mockGetCourseAttendanceData.mockResolvedValue({
            id: 'course-1',
            title: 'Intro to RPG Systems',
        });

        const page = await CourseContentPage({
            params: Promise.resolve({ slug: 'intro-to-rpg' }),
        });

        render(page);

        expect(mockGetCourseAttendanceData).toHaveBeenCalledWith('intro-to-rpg', {
            includeProgress: true,
        });
        expect(screen.getByTestId('course-attendance-shell')).toHaveTextContent(
            'Intro to RPG Systems'
        );
    });

    it('blocks authenticated learners without course access', async () => {
        mockAuth.mockResolvedValue({ user: { id: 'user-1' } });
        mockGetCourseAttendanceData.mockResolvedValue(null);

        await expect(
            CourseContentPage({ params: Promise.resolve({ slug: 'intro-to-rpg' }) })
        ).rejects.toThrow('NOT_FOUND');

        expect(mockGetCourseAttendanceData).toHaveBeenCalledWith('intro-to-rpg', {
            includeProgress: true,
        });
    });
});
