import '@testing-library/jest-dom/vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { CourseCard, CourseTableActions } from './course-card';

vi.mock('@/i18n/navigation', () => ({
  Link: (props: { children: ReactNode; href: string; locale?: string; prefetch?: boolean }) => {
    const { children, href, ...anchorProps } = props;
    const locale = anchorProps.locale;
    delete anchorProps.locale;
    delete anchorProps.prefetch;
    return (
      <a href={href} data-locale={locale} {...anchorProps}>
        {children}
      </a>
    );
  },
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

    const menu = await screen.findByRole('menu');
    const editLink = within(menu).getByRole('menuitem', { name: /edit course/i });
    const previewLink = within(menu).getByRole('menuitem', { name: /^preview$/i });

    expect(editLink).toHaveAttribute('href', '/workspace/learning/courses/combat-design-foundations-by-ada-lovelace');
    expect(editLink).toHaveAttribute('data-locale', 'en-US');
    expect(previewLink).toHaveAttribute('href', '/workspace/learning/courses/combat-design-foundations-by-ada-lovelace/preview');
    expect(previewLink).toHaveAttribute('data-locale', 'en-US');
  });

  it('exposes accessible edit and preview links from the table row menu', async () => {
    render(<CourseTableActions courseRouteParam="combat-design-foundations-by-ada-lovelace" courseTitle="Combat Design Foundations" locale="en-US" />);

    await userEvent.click(screen.getByRole('button', { name: /open combat design foundations actions/i }));

    const menu = await screen.findByRole('menu');
    const editLink = within(menu).getByRole('menuitem', { name: /^edit$/i });
    const previewLink = within(menu).getByRole('menuitem', { name: /^preview$/i });

    expect(editLink).toHaveAttribute('href', '/workspace/learning/courses/combat-design-foundations-by-ada-lovelace');
    expect(editLink).toHaveAttribute('data-locale', 'en-US');
    expect(previewLink).toHaveAttribute('href', '/workspace/learning/courses/combat-design-foundations-by-ada-lovelace/preview');
    expect(previewLink).toHaveAttribute('data-locale', 'en-US');
  });
});
