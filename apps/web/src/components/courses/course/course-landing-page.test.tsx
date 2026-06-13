import '@testing-library/jest-dom/vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ImgHTMLAttributes, ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { PUBLIC_COURSE_SNAPSHOT } from '@/lib/courses/public-programs';
import { CourseLandingPage } from './course-landing-page';

type MockImageProps = ImgHTMLAttributes<HTMLImageElement> & {
  alt: string;
  src: string;
  fill?: boolean;
  unoptimized?: boolean;
  priority?: boolean;
};

vi.mock('next/image', () => ({
  default: (props: MockImageProps) => {
    const imageProps = { ...props };
    delete imageProps.fill;
    delete imageProps.unoptimized;
    delete imageProps.priority;

    // eslint-disable-next-line @next/next/no-img-element
    return <img {...imageProps} />;
  },
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...rest }: { children: ReactNode; href: string }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

vi.mock('./course-self-enroll-button', () => ({
  CourseSelfEnrollButton: ({ courseSlug }: { courseSlug: string }) => <button type="button">Enroll in {courseSlug}</button>,
}));

describe('CourseLandingPage', () => {
  it('renders a project gallery and checkpoint-based course journey for advanced AI', () => {
    const course = PUBLIC_COURSE_SNAPSHOT.find((item) => item.slug === 'ai4games2');

    expect(course).toBeDefined();

    render(<CourseLandingPage course={course!} viewerAccess={{ state: 'signed-out' }} />);

    const projectGallery = screen.getByRole('region', { name: /project gallery/i });
    expect(within(projectGallery).getByRole('heading', { name: /Influence-map arena/i })).toBeInTheDocument();
    expect(within(projectGallery).getByRole('heading', { name: /Decision scoring encounter/i })).toBeInTheDocument();
    expect(within(projectGallery).getByRole('heading', { name: /Prototype polish pass/i })).toBeInTheDocument();
    expect(within(projectGallery).getAllByText(/Deliverable/i)).toHaveLength(3);

    const courseJourney = screen.getByRole('region', { name: /course journey/i });
    expect(within(courseJourney).getAllByText(/Checkpoint output/i)).toHaveLength(5);
    expect(within(courseJourney).getByText(/Spatial reasoning map/i)).toBeInTheDocument();
    expect(within(courseJourney).getByText(/Portfolio-ready AI breakdown/i)).toBeInTheDocument();
  });

  it('lets students move through the project gallery', async () => {
    const user = userEvent.setup();
    const course = PUBLIC_COURSE_SNAPSHOT.find((item) => item.slug === 'ai4games2');

    expect(course).toBeDefined();

    render(<CourseLandingPage course={course!} viewerAccess={{ state: 'signed-out' }} />);

    const projectGallery = screen.getByRole('region', { name: /project gallery/i });
    expect(within(projectGallery).getByRole('button', { name: /Influence-map arena/i })).toHaveAttribute('aria-current', 'true');

    await user.click(within(projectGallery).getByRole('button', { name: /show next project/i }));

    expect(within(projectGallery).getByRole('button', { name: /Decision scoring encounter/i })).toHaveAttribute('aria-current', 'true');
  });
});
