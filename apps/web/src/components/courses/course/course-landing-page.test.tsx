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

  it('prefers dashboard-editable skills over static showcase outcomes', () => {
    const course = PUBLIC_COURSE_SNAPSHOT.find((item) => item.slug === 'ai4games2');

    expect(course).toBeDefined();

    render(
      <CourseLandingPage
        course={{
          ...course!,
          skillsProvided: 'Tune combat director pacing, Package readable AI telemetry',
          skillsRequired: 'Behavior tree fundamentals, Debugging AI state',
        }}
        viewerAccess={{ state: 'signed-out' }}
      />,
    );

    expect(screen.getByText('Tune combat director pacing')).toBeInTheDocument();
    expect(screen.getByText('Package readable AI telemetry')).toBeInTheDocument();
    expect(screen.getByText('Behavior tree fundamentals')).toBeInTheDocument();
    expect(screen.getByText('Debugging AI state')).toBeInTheDocument();
  });

  it('uses an edited course description for the hero copy before static showcase text', () => {
    const course = PUBLIC_COURSE_SNAPSHOT.find((item) => item.slug === 'ai4games2');

    expect(course).toBeDefined();

    render(
      <CourseLandingPage
        course={{
          ...course!,
          description: 'Instructor-edited hero copy for the public storefront.',
        }}
        viewerAccess={{ state: 'signed-out' }}
      />,
    );

    expect(screen.getByText('Instructor-edited hero copy for the public storefront.')).toBeInTheDocument();
  });

  it('uses dashboard-edited FAQ metadata before static showcase FAQ', () => {
    const course = PUBLIC_COURSE_SNAPSHOT.find((item) => item.slug === 'ai4games2');

    expect(course).toBeDefined();

    render(
      <CourseLandingPage
        course={{
          ...course!,
          metadata: JSON.stringify({
            landingFaq: [
              { question: 'Is the FAQ editable from the dashboard?', answer: 'Yes, this public FAQ is metadata-backed.' },
            ],
          }),
        }}
        viewerAccess={{ state: 'signed-out' }}
      />,
    );

    expect(screen.getByText('Is the FAQ editable from the dashboard?')).toBeInTheDocument();
    expect(screen.getByText('Yes, this public FAQ is metadata-backed.')).toBeInTheDocument();
  });
});
