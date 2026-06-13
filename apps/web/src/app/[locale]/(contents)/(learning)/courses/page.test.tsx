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

vi.mock('next/image', () => ({
  default: ({ alt, src, fill: _fill, unoptimized: _unoptimized, priority: _priority, ...rest }: { alt: string; src: string; fill?: boolean; unoptimized?: boolean; priority?: boolean }) => <img alt={alt} src={src} {...rest} />,
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

const { default: CoursesPage } = await import('./page');

function hasStat(value: string, label: string) {
  return screen.getByText((_, element) => element?.textContent === `${value}${label}`);
}

describe('CoursesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders course storefront stats and passes courses to the catalog component', async () => {
    getPublicCourseCatalogMock.mockResolvedValue({
      success: true,
      source: 'api',
      data: courseFixtures,
    });

    render(await CoursesPage());

    expect(screen.getByText('Build the game development portfolio you want to be known for.')).toBeInTheDocument();
    expect(hasStat('2', 'Courses')).toBeInTheDocument();
    expect(hasStat('1', 'Open seats')).toBeInTheDocument();
    expect(hasStat('30h', 'Study time')).toBeInTheDocument();
    expect(screen.getAllByText('Python Programming').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Portfolio Development').length).toBeGreaterThan(0);
    expect(publicCourseCatalogMock).toHaveBeenCalledWith(courseFixtures);
  });

  it('discloses when the page is using the imported snapshot fallback', async () => {
    getPublicCourseCatalogMock.mockResolvedValue({
      success: true,
      source: 'snapshot-fallback',
      data: courseFixtures,
    });

    render(await CoursesPage());

    expect(screen.getByText(/showing the imported GameGuild course snapshot/i)).toBeInTheDocument();
    expect(publicCourseCatalogMock).toHaveBeenCalledWith(courseFixtures);
  });
});
