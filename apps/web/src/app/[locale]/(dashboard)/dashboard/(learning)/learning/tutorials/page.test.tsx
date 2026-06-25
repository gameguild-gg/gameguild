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

const { default: TutorialsPage } = await import('./page');

describe('LearningTutorialsPage', () => {
  beforeEach(() => {
    getLearningContentLibraryMock.mockReset();
  });

  it('renders tutorial-like course content and filters assessments out', async () => {
    getLearningContentLibraryMock.mockResolvedValue({
      error: null,
      items: [
        {
          id: 'lesson-1',
          courseId: 'course-1',
          courseTitle: 'Gameplay Prototyping',
          courseSlug: 'gameplay-prototyping',
          title: 'Prototype combat loop',
          description: 'Step-by-step tutorial.',
          type: 'Code',
          visibility: 'public',
          status: 'published',
          durationMinutes: 90,
          isRequired: true,
          updatedAt: '2026-01-03T00:00:00.000Z',
        },
        {
          id: 'quiz-1',
          courseId: 'course-1',
          courseTitle: 'Gameplay Prototyping',
          courseSlug: 'gameplay-prototyping',
          title: 'Midterm quiz',
          description: 'Assessment.',
          type: 'Questionnaire',
          visibility: 'public',
          status: 'published',
          durationMinutes: 30,
          isRequired: true,
          updatedAt: '2026-01-02T00:00:00.000Z',
        },
      ],
    });

    render(await TutorialsPage());

    expect(screen.getByRole('heading', { name: /tutorials/i })).toBeInTheDocument();
    expect(screen.getByText('Prototype combat loop')).toBeInTheDocument();
    expect(screen.queryByText('Midterm quiz')).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: /edit tutorial/i })).toHaveAttribute(
      'href',
      '/dashboard/learning/courses/course-1/content/lesson-1',
    );
  });
});
