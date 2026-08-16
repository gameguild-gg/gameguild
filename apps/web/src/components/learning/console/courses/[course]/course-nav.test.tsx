import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CourseNav } from './course-nav';

const refreshMock = vi.fn();
const actionMocks = vi.hoisted(() => ({
  publishCourse: vi.fn(),
  restoreCourse: vi.fn(),
  unpublishCourse: vi.fn(),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({
    children,
    href,
    locale,
    prefetch: _prefetch,
    ...rest
  }: {
    children: ReactNode;
    href: string;
    locale?: string;
    prefetch?: boolean;
  }) => (
    <a href={href} data-locale={locale} {...rest}>
      {children}
    </a>
  ),
  usePathname: () => '/en-US/workspace/learning/courses/ai-for-boss-encounters/listing',
  useRouter: () => ({ refresh: refreshMock }),
}));

vi.mock('@/lib/learning/actions', () => ({
  publishCourse: actionMocks.publishCourse,
  restoreCourse: actionMocks.restoreCourse,
  unpublishCourse: actionMocks.unpublishCourse,
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
    actionMocks.publishCourse.mockResolvedValue({ success: true, data: null });
    actionMocks.restoreCourse.mockResolvedValue({ success: true, data: null });
    actionMocks.unpublishCourse.mockResolvedValue({ success: true, data: null });
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
        courseRouteParam="ai-for-boss-encounters"
        locale="en-US"
        features={enabledFeatures}
      >
        <div>Course editor content</div>
      </CourseNav>,
    );

    const previewLink = screen.getByRole('link', { name: /preview/i });
    expect(previewLink).toHaveAttribute('href', '/workspace/learning/courses/ai-for-boss-encounters/preview');
    expect(previewLink).toHaveAttribute('data-locale', 'en-US');
    expect(screen.getAllByText('Course editor content')).toHaveLength(1);
  });

  it('copies the public storefront course URL from the share action', async () => {
    render(
      <CourseNav
        courseTitle="AI for Boss Encounters"
        courseDescription="Build readable encounter AI."
        courseStatus="published"
        courseSlug="ai-for-boss-encounters"
        courseRouteParam="ai-for-boss-encounters"
        locale="en-US"
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
        courseRouteParam="untitled-draft"
        locale="en-US"
        features={enabledFeatures}
      >
        <div>Course editor content</div>
      </CourseNav>,
    );

    expect(screen.getByRole('link', { name: /preview/i })).toHaveAttribute('href', '/workspace/learning/courses/untitled-draft/preview');
    expect(screen.getByRole('button', { name: /share/i })).toBeDisabled();
  });

  it('confirms before unpublishing a published course', async () => {
    render(
      <CourseNav
        courseTitle="AI for Boss Encounters"
        courseDescription="Build readable encounter AI."
        courseStatus="published"
        courseSlug="ai-for-boss-encounters"
        courseRouteParam="ai-for-boss-encounters-by-gameguild"
        locale="en-US"
        features={enabledFeatures}
      >
        <div>Course editor content</div>
      </CourseNav>,
    );

    fireEvent.click(screen.getByRole('button', { name: /^unpublish$/i }));
    expect(screen.getByRole('heading', { name: /unpublish this course/i })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /unpublish course/i }));

    expect(actionMocks.unpublishCourse).toHaveBeenCalledWith('ai-for-boss-encounters-by-gameguild');
    expect(await screen.findByRole('button', { name: /^publish$/i })).toBeEnabled();
  });

  it('restores an archived course to draft before it can be published again', async () => {
    render(
      <CourseNav
        courseTitle="AI for Boss Encounters"
        courseDescription="Build readable encounter AI."
        courseStatus="archived"
        courseSlug="ai-for-boss-encounters"
        courseRouteParam="ai-for-boss-encounters-by-gameguild"
        locale="en-US"
        features={enabledFeatures}
      >
        <div>Course editor content</div>
      </CourseNav>,
    );

    const restoreButton = screen.getByRole('button', { name: /^restore$/i });
    expect(restoreButton).toBeEnabled();
    fireEvent.click(restoreButton);

    expect(actionMocks.restoreCourse).toHaveBeenCalledWith('ai-for-boss-encounters-by-gameguild');
    expect(actionMocks.publishCourse).not.toHaveBeenCalled();
    expect(await screen.findByText('Draft')).toBeInTheDocument();
    expect(await screen.findByRole('button', { name: /^publish$/i })).toBeEnabled();
  });
});
