import '@testing-library/jest-dom/vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { CourseCard, CourseTableActions } from './course-card';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, locale, prefetch: _prefetch, ...rest }: { children: ReactNode; href: string; locale?: string; prefetch?: boolean }) => (
    <a href={href} data-locale={locale} {...rest}>
      {children}
    </a>
  ),
}));

const course = {
  id: 'course-123',
  slug: 'combat-design-foundations',
  routeParam: 'combat-design-foundations-by-ada-lovelace',
  title: 'Combat Design Foundations',
  status: 'draft',
  visibility: 'public',
  enrolledCount: 12,
  completionPercent: 42,
  avgRating: '4.7',
};

describe('CourseCard actions', () => {
  it('exposes accessible edit and preview links from the grid card menu', async () => {
    render(<CourseCard course={course} locale="en-US" />);

    await userEvent.click(screen.getByRole('button', { name: /open combat design foundations actions/i }));

    const menu = screen.getByRole('menu');
    const editLink = within(menu).getByRole('menuitem', { name: /edit course/i });
    const previewLink = within(menu).getByRole('menuitem', { name: /^preview$/i });

    expect(editLink).toHaveAttribute('href', '/dashboard/platform/learning/courses/combat-design-foundations-by-ada-lovelace');
    expect(editLink).toHaveAttribute('data-locale', 'en-US');
    expect(previewLink).toHaveAttribute('href', '/dashboard/platform/learning/courses/combat-design-foundations-by-ada-lovelace/preview');
    expect(previewLink).toHaveAttribute('data-locale', 'en-US');
  });

  it('exposes accessible edit and preview links from the table row menu', async () => {
    render(<CourseTableActions courseRouteParam="combat-design-foundations-by-ada-lovelace" courseTitle="Combat Design Foundations" locale="en-US" />);

    await userEvent.click(screen.getByRole('button', { name: /open combat design foundations actions/i }));

    const menu = screen.getByRole('menu');
    const editLink = within(menu).getByRole('menuitem', { name: /^edit$/i });
    const previewLink = within(menu).getByRole('menuitem', { name: /^preview$/i });

    expect(editLink).toHaveAttribute('href', '/dashboard/platform/learning/courses/combat-design-foundations-by-ada-lovelace');
    expect(editLink).toHaveAttribute('data-locale', 'en-US');
    expect(previewLink).toHaveAttribute('href', '/dashboard/platform/learning/courses/combat-design-foundations-by-ada-lovelace/preview');
    expect(previewLink).toHaveAttribute('data-locale', 'en-US');
  });
});
