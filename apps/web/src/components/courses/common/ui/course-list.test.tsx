import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { AnchorHTMLAttributes, ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('next/navigation', () => ({
  usePathname: () => '/dashboard/platform/learning/courses',
  useRouter: () => ({
    push: vi.fn(),
    replace: vi.fn(),
    refresh: vi.fn(),
  }),
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: AnchorHTMLAttributes<HTMLAnchorElement> & { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
  usePathname: () => '/dashboard/platform/learning/courses',
  useRouter: () => ({
    push: vi.fn(),
    replace: vi.fn(),
    refresh: vi.fn(),
  }),
}));

import { CourseList } from './course-list';

describe('CourseList', () => {
  it('renders table mode as an accessible table with row actions', async () => {
    const onView = vi.fn();
    const onEdit = vi.fn();

    render(
      <CourseList
        initialViewMode="table"
        hideViewToggle
        courses={[
          {
            id: 'course-1',
            title: 'Launch Readiness',
            description: 'Prepare a game launch.',
            status: 'published',
            category: 'Game Development',
            difficulty: 'intermediate',
            estimatedHours: 12,
            currentEnrollments: 42,
          },
        ]}
        onView={onView}
        onEdit={onEdit}
      />,
    );

    const table = screen.getByRole('table', { name: /courses/i });
    const row = within(table).getByRole('row', { name: /launch readiness/i });

    expect(within(row).getByText(/published/i)).toBeInTheDocument();
    expect(within(row).getByText(/game development/i)).toBeInTheDocument();
    expect(within(row).getByText(/12h/i)).toBeInTheDocument();
    expect(within(row).getByText('42')).toBeInTheDocument();

    await userEvent.click(within(row).getByRole('button', { name: /view/i }));
    await userEvent.click(within(row).getByRole('button', { name: /edit/i }));

    expect(onView).toHaveBeenCalledWith(expect.objectContaining({ id: 'course-1' }));
    expect(onEdit).toHaveBeenCalledWith(expect.objectContaining({ id: 'course-1' }));
  });
});
