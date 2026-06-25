import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ContentTree } from './content-tree';
import type { ContentItem } from '@/lib/learning/types';
import { TooltipProvider } from '@game-guild/ui/components/tooltip';

Object.defineProperties(HTMLElement.prototype, {
  hasPointerCapture: { value: vi.fn(() => false) },
  setPointerCapture: { value: vi.fn() },
  releasePointerCapture: { value: vi.fn() },
  scrollIntoView: { value: vi.fn() },
});

vi.mock('next/navigation', () => ({
  usePathname: () => '/en-US/dashboard/learning/courses/course-1/content',
  useRouter: () => ({
    push: vi.fn(),
    refresh: vi.fn(),
  }),
}));

vi.mock('@/lib/learning/actions', () => ({
  addContent: vi.fn(),
  deleteContent: vi.fn(),
  reorderContent: vi.fn(),
  updateAssessment: vi.fn(),
  updateContent: vi.fn(),
}));

const moduleItem = {
  id: 'module-1',
  parentId: null,
  order: 0,
  type: 'Lesson',
  title: 'Week 01',
  description: null,
  status: 'published',
  duration: null,
  metadata: {},
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-01T00:00:00.000Z',
} satisfies ContentItem;

describe('ContentTree lesson creation', () => {
  it('offers lesson and activity types without legacy Page or Challenge options', async () => {
    const user = userEvent.setup();

    render(
      <TooltipProvider>
        <ContentTree
          courseId="course-1"
          modules={[moduleItem]}
          allItems={[moduleItem]}
          assessments={[]}
        />
      </TooltipProvider>,
    );

    await user.click(screen.getByRole('button', { name: /add lesson/i }));
    await user.click(screen.getByRole('combobox', { name: /type/i }));

    expect(screen.getByRole('option', { name: 'Lesson' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Quiz' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Assignment' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Project' })).toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Page' })).not.toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Challenge' })).not.toBeInTheDocument();
  });
});
