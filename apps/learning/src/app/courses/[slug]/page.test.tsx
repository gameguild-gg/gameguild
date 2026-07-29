import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mockAuth = vi.fn();
const mockGetCourseAccessData = vi.fn();
const mockRedirect = vi.fn((target: string) => {
    throw new Error(`REDIRECT:${target}`);
});
const mockNotFound = vi.fn(() => {
    throw new Error('NOT_FOUND');
});

vi.mock('@/auth', () => ({ auth: () => mockAuth() }));
vi.mock('@/lib/courses', () => ({
    getCourseAccessData: (...args: unknown[]) => mockGetCourseAccessData(...args),
}));
vi.mock('@/components/course-access-gate', () => ({
    CourseAccessGate: ({ access }: { access: { kind: string } }) => (
        <div data-testid="course-access-gate">{access.kind}</div>
    ),
}));
vi.mock('@game-guild/ui/components/badge', () => ({ Badge: ({ children }: { children: unknown }) => <div>{children}</div> }));
vi.mock('@game-guild/ui/components/button', () => ({ Button: ({ children }: { children: unknown }) => <div>{children}</div> }));
vi.mock('@game-guild/ui/components/card', () => ({
    Card: ({ children }: { children: unknown }) => <div>{children}</div>,
    CardContent: ({ children }: { children: unknown }) => <div>{children}</div>,
    CardHeader: ({ children }: { children: unknown }) => <div>{children}</div>,
    CardTitle: ({ children }: { children: unknown }) => <div>{children}</div>,
}));
vi.mock('lucide-react', () => ({ ArrowRight: () => null, Clock3: () => null, Layers3: () => null }));
vi.mock('next/image', () => ({ default: () => null }));
vi.mock('next/link', () => ({ default: ({ children }: { children: unknown }) => <>{children}</> }));
vi.mock('next/navigation', () => ({
    redirect: (target: string) => mockRedirect(target),
    notFound: () => mockNotFound(),
}));

const { default: CourseOverviewPage } = await import('./page');

describe('CourseOverviewPage', () => {
    beforeEach(() => vi.clearAllMocks());

    it('redirects unauthenticated learners to sign-in with the overview target', async () => {
        mockAuth.mockResolvedValue(null);
        await expect(CourseOverviewPage({ params: Promise.resolve({ slug: 'intro-to-rpg' }) })).rejects.toThrow(
            'REDIRECT:/sign-in?redirectTo=%2Fcourses%2Fintro-to-rpg',
        );
    });

    it('renders an enrollment gate instead of a 404 for an available course', async () => {
        mockAuth.mockResolvedValue({ user: { id: 'user-1' } });
        mockGetCourseAccessData.mockResolvedValue({
            kind: 'enrollment-required',
            course: { id: 'course-1', slug: 'intro-to-rpg', title: 'Intro to RPG' },
        });

        render(await CourseOverviewPage({ params: Promise.resolve({ slug: 'intro-to-rpg' }) }));

        expect(screen.getByTestId('course-access-gate')).toHaveTextContent('enrollment-required');
        expect(mockGetCourseAccessData).toHaveBeenCalledWith('intro-to-rpg');
    });

    it('uses the not-found boundary only when the course does not exist', async () => {
        mockAuth.mockResolvedValue({ user: { id: 'user-1' } });
        mockGetCourseAccessData.mockResolvedValue({ kind: 'not-found' });
        await expect(CourseOverviewPage({ params: Promise.resolve({ slug: 'missing' }) })).rejects.toThrow('NOT_FOUND');
    });
});