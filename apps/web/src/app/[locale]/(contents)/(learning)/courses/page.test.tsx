import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const getPublicCourseCatalogMock = vi.fn();
const publicCourseCatalogMock = vi.fn();

vi.mock('@/lib/courses/services/course.service', async () => {
    const actual = await vi.importActual('@/lib/courses/services/course.service');
    return {
        ...(actual as object),
        getPublicCourseCatalog: getPublicCourseCatalogMock,
    };
});

vi.mock('@/components/courses/public-course-catalog', () => ({
    PublicCourseCatalog: ({ initialCourses }: { initialCourses: Array<{ title: string }> }) => {
        publicCourseCatalogMock(initialCourses);
        return (
            <div data-testid="public-course-catalog">
                {initialCourses.map((course) => (
                    <span key={course.title}>{course.title}</span>
                ))}
            </div>
        );
    },
}));

vi.mock('next/link', () => ({
    default: ({ children, href, ...rest }: { children: React.ReactNode; href: string }) => (
        <a href={href} {...rest}>
            {children}
        </a>
    ),
}));

vi.mock('@/i18n/navigation', () => ({
    Link: ({ children, href, ...rest }: { children: React.ReactNode; href: string }) => (
        <a href={href} {...rest}>
            {children}
        </a>
    ),
}));

const courseFixtures = [
    {
        id: 'course-python',
        title: 'Python Programming',
        slug: 'python',
        description: 'Students will learn Python programming fundamentals.',
        category: 'Programming',
        difficulty: 'Beginner',
        estimatedHours: 10,
        currentEnrollments: 0,
        averageRating: 0,
        totalRatings: 0,
        isEnrollmentOpen: true,
        thumbnail: null,
        videoShowcaseUrl: null,
        visibility: 'Public',
        status: 'Published',
        maxEnrollments: null,
        enrollmentDeadline: null,
        skillsRequired: null,
        skillsProvided: null,
        programContents: null,
    },
    {
        id: 'course-portfolio',
        title: 'Portfolio Development',
        slug: 'portfolio',
        description: 'Build a professional portfolio to showcase your work.',
        category: 'Design',
        difficulty: 'Beginner',
        estimatedHours: 20,
        currentEnrollments: 0,
        averageRating: 0,
        totalRatings: 0,
        isEnrollmentOpen: false,
        thumbnail: null,
        videoShowcaseUrl: null,
        visibility: 'Public',
        status: 'Published',
        maxEnrollments: null,
        enrollmentDeadline: null,
        skillsRequired: null,
        skillsProvided: null,
        programContents: null,
    },
];

const { default: ProgramsPage } = await import('./page');

function hasStat(value: string, label: string) {
    return screen.getByText((_, element) => element?.textContent === `${value}${label}`);
}

describe('ProgramsPage', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('renders live catalog stats and passes courses to the catalog component', async () => {
        getPublicCourseCatalogMock.mockResolvedValue({
            success: true,
            data: courseFixtures,
        });

        render(await ProgramsPage());

        expect(screen.getByText('🎮 2 Live Courses')).toBeInTheDocument();
        expect(hasStat('2', 'Live Courses')).toBeInTheDocument();
        expect(hasStat('1', 'Open Enrollments')).toBeInTheDocument();
        expect(hasStat('2', 'Active Disciplines')).toBeInTheDocument();
        expect(hasStat('30h', 'Planned Learning Time')).toBeInTheDocument();
        expect(screen.getByText('Python Programming')).toBeInTheDocument();
        expect(screen.getByText('Portfolio Development')).toBeInTheDocument();
        expect(publicCourseCatalogMock).toHaveBeenCalledWith(courseFixtures);
    });

    it('renders the unavailable state when the catalog request fails', async () => {
        getPublicCourseCatalogMock.mockResolvedValue({
            success: false,
            data: [],
            error: 'Request failed',
        });

        render(await ProgramsPage());

        expect(screen.getByText('Catalog Temporarily Unavailable')).toBeInTheDocument();
        expect(screen.getByText('The live course catalog is temporarily unavailable')).toBeInTheDocument();
        expect(screen.getAllByText('--').length).toBeGreaterThanOrEqual(1);
        expect(publicCourseCatalogMock).not.toHaveBeenCalled();
    });
});