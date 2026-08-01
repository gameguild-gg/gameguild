import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mockAuth = vi.fn();
const mockGetCourseAccessData = vi.fn();
const mockRedirect = vi.fn((target: string) => { throw new Error(`REDIRECT:${target}`); });
const mockNotFound = vi.fn(() => { throw new Error('NOT_FOUND'); });

vi.mock('@/auth', () => ({ auth: () => mockAuth() }));
vi.mock('@/lib/courses', () => ({ getCourseAccessData: (...args: unknown[]) => mockGetCourseAccessData(...args) }));
vi.mock('@/components/course-access-gate', () => ({
    CourseAccessGate: ({ access }: { access: { kind: string } }) => <div data-testid="course-access-gate">{access.kind}</div>,
}));
vi.mock('@/components/course-attendance-shell', () => ({
    CourseAttendanceShell: ({ course }: { course: { title: string } }) => <div data-testid="course-attendance-shell">{course.title}</div>,
}));
vi.mock('next/navigation', () => ({ redirect: (target: string) => mockRedirect(target), notFound: () => mockNotFound() }));

const { default: CourseContentPage } = await import('./page');

describe('CourseContentPage', () => {
    beforeEach(() => vi.clearAllMocks());

    it('redirects unauthenticated learners to sign-in with the classroom target', async () => {
        mockAuth.mockResolvedValue(null);
        await expect(CourseContentPage({ params: Promise.resolve({ slug: 'intro-to-rpg' }) })).rejects.toThrow(
            'REDIRECT:/sign-in?redirectTo=%2Fcourses%2Fintro-to-rpg%2Fcontent',
        );
    });

    it('renders the classroom shell for authenticated learners', async () => {
        mockAuth.mockResolvedValue({ user: { id: 'user-1' } });
        mockGetCourseAccessData.mockResolvedValue({ kind: 'ready', course: { id: 'course-1', title: 'Intro to RPG Systems' } });
        render(await CourseContentPage({ params: Promise.resolve({ slug: 'intro-to-rpg' }) }));
        expect(mockGetCourseAccessData).toHaveBeenCalledWith('intro-to-rpg');
        expect(screen.getByTestId('course-attendance-shell')).toHaveTextContent('Intro to RPG Systems');
    });

    it('renders a payment gate instead of a 404 for a paid course', async () => {
        mockAuth.mockResolvedValue({ user: { id: 'user-1' } });
        mockGetCourseAccessData.mockResolvedValue({
            kind: 'payment-required',
            course: { id: 'course-1', slug: 'intro-to-rpg', title: 'Intro to RPG' },
            price: 49,
            currency: 'USD',
        });
        render(await CourseContentPage({ params: Promise.resolve({ slug: 'intro-to-rpg' }) }));
        expect(screen.getByTestId('course-access-gate')).toHaveTextContent('payment-required');
    });

    it('uses the not-found boundary only when the course does not exist', async () => {
        mockAuth.mockResolvedValue({ user: { id: 'user-1' } });
        mockGetCourseAccessData.mockResolvedValue({ kind: 'not-found' });
        await expect(CourseContentPage({ params: Promise.resolve({ slug: 'missing' }) })).rejects.toThrow('NOT_FOUND');
    });
});