import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CourseNav } from './course-nav';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...rest }: { children: ReactNode; href: string }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
  usePathname: () => '/dashboard/learning/courses/course-123/listing',
}));

vi.mock('next/navigation', () => ({
  useParams: () => ({ course: 'course-123' }),
}));

const enabledFeatures = {
  hasClasses: true,
  hasRecordings: true,
  hasSchedule: true,
  hasOnDemandContent: true,
  hasPricing: true,
  hasCertificate: true,
  hasAssessments: true,
  hasDiscussions: true,
};

describe('CourseNav', () => {
  const writeTextMock = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    Object.defineProperty(navigator, 'clipboard', {
      configurable: true,
      value: { writeText: writeTextMock },
    });
  });

  it('links the preview action to the authenticated dashboard storefront preview', () => {
    render(
      <CourseNav
        courseTitle="AI for Boss Encounters"
        courseDescription="Build readable encounter AI."
        courseStatus="published"
        courseSlug="ai-for-boss-encounters"
        features={enabledFeatures}
      >
        <div>Course editor content</div>
      </CourseNav>,
    );

    expect(screen.getByRole('link', { name: /preview/i })).toHaveAttribute('href', '/dashboard/learning/courses/course-123/preview');
  });

  it('copies the public storefront course URL from the share action', async () => {
    render(
      <CourseNav
        courseTitle="AI for Boss Encounters"
        courseDescription="Build readable encounter AI."
        courseStatus="published"
        courseSlug="ai-for-boss-encounters"
        features={enabledFeatures}
      >
        <div>Course editor content</div>
      </CourseNav>,
    );

    fireEvent.click(screen.getByRole('button', { name: /share/i }));

    expect(writeTextMock).toHaveBeenCalledWith('http://localhost:3000/courses/ai-for-boss-encounters');
    expect(await screen.findByRole('button', { name: /copied/i })).toBeInTheDocument();
  });

  it('keeps public sharing disabled when a course has no public slug', () => {
    render(
      <CourseNav
        courseTitle="Untitled Draft"
        courseDescription="Draft course."
        courseStatus="draft"
        courseSlug=""
        features={enabledFeatures}
      >
        <div>Course editor content</div>
      </CourseNav>,
    );

    expect(screen.getByRole('link', { name: /preview/i })).toHaveAttribute('href', '/dashboard/learning/courses/course-123/preview');
    expect(screen.getByRole('button', { name: /share/i })).toBeDisabled();
  });
});
