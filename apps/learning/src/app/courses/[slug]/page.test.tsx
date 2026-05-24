import { beforeEach, describe, expect, it, vi } from 'vitest';

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

vi.mock('@game-guild/ui/components/badge', () => ({
    Badge: ({ children }: { children: unknown }) => <div>{children}</div>,
}));

vi.mock('@game-guild/ui/components/button', () => ({
    Button: ({ children }: { children: unknown }) => <div>{children}</div>,
}));

vi.mock('@game-guild/ui/components/card', () => ({
    Card: ({ children }: { children: unknown }) => <div>{children}</div>,
    CardContent: ({ children }: { children: unknown }) => <div>{children}</div>,
    CardHeader: ({ children }: { children: unknown }) => <div>{children}</div>,
    CardTitle: ({ children }: { children: unknown }) => <div>{children}</div>,
}));

vi.mock('lucide-react', () => ({
    ArrowRight: () => null,
    Clock3: () => null,
    Layers3: () => null,
}));

vi.mock('next/image', () => ({
    default: () => null,
}));

vi.mock('next/link', () => ({
    default: ({ children }: { children: unknown }) => <>{children}</>,
}));

vi.mock('next/navigation', () => ({
    redirect: (target: string) => mockRedirect(target),
    notFound: () => mockNotFound(),
}));

const { default: CourseOverviewPage } = await import('./page');

describe('CourseOverviewPage', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('redirects unauthenticated learners to sign-in with the overview target', async () => {
        mockAuth.mockResolvedValue(null);

        await expect(
            CourseOverviewPage({ params: Promise.resolve({ slug: 'intro-to-rpg' }) })
        ).rejects.toThrow('REDIRECT:/sign-in?redirectTo=%2Fcourses%2Fintro-to-rpg');
    });

    it('blocks authenticated learners without course access', async () => {
        mockAuth.mockResolvedValue({ user: { id: 'user-1' } });
        mockGetCourseAttendanceData.mockResolvedValue(null);

        await expect(
            CourseOverviewPage({ params: Promise.resolve({ slug: 'intro-to-rpg' }) })
        ).rejects.toThrow('NOT_FOUND');

        expect(mockGetCourseAttendanceData).toHaveBeenCalledWith('intro-to-rpg', {
            includeProgress: true,
        });
    });
});
