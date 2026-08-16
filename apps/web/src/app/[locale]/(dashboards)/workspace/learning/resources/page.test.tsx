import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const getLearningContentLibraryMock = vi.fn();

vi.mock('@/lib/learning', () => ({
  getLearningContentLibrary: getLearningContentLibraryMock,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

const { default: ResourcesPage } = await import('./page');

describe('LearningResourcesPage', () => {
  beforeEach(() => {
    getLearningContentLibraryMock.mockReset();
  });

  it('renders API-backed course resources with editor links', async () => {
    getLearningContentLibraryMock.mockResolvedValue({
      error: null,
      items: [
        {
          id: 'content-1',
          courseId: 'course-1',
          courseTitle: 'Boss AI Production',
          courseSlug: 'boss-ai-production',
          title: 'Behavior tree worksheet',
          description: 'Downloadable planning sheet.',
          type: 'Lesson',
          visibility: 'public',
          status: 'published',
          durationMinutes: 45,
          isRequired: true,
          updatedAt: '2026-01-02T00:00:00.000Z',
        },
      ],
    });

    render(await ResourcesPage());

    expect(screen.getByRole('heading', { name: /resources/i })).toBeInTheDocument();
    expect(screen.getByText('Behavior tree worksheet')).toBeInTheDocument();
    expect(screen.getByText('Boss AI Production')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /edit resource/i })).toHaveAttribute(
      'href',
      '/workspace/learning/courses/course-1/content/content-1',
    );
  });
});
