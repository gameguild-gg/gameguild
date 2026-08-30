import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type React from 'react';
import { beforeAll, describe, expect, it, vi } from 'vitest';
import { CourseList } from './course-list';

Object.defineProperties(HTMLElement.prototype, {
  hasPointerCapture: { value: vi.fn(() => false) },
  setPointerCapture: { value: vi.fn() },
  releasePointerCapture: { value: vi.fn() },
  scrollIntoView: { value: vi.fn() },
});

beforeAll(() => {
  global.ResizeObserver = class ResizeObserver {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
});

vi.mock('@/i18n/navigation', () => ({
  Link: (props: { href: string; children: React.ReactNode; locale?: string; prefetch?: boolean }) => {
    const { href, children, ...anchorProps } = props;
    delete anchorProps.locale;
    delete anchorProps.prefetch;
    return <a href={href} {...anchorProps}>{children}</a>;
  },
}));

vi.mock('@/lib/learning/course-route', () => ({
  buildDashboardCoursePath: (course: string | { id: string; routeParam?: string }, path = '') => {
    const id = typeof course === 'string' ? course : course.routeParam || course.id;
    return `/workspace/learning/courses/${id}${path ? `/${path}` : ''}`;
  },
}));

const courses = [
  {
    id: 'course-1',
    routeParam: 'boss-ai-by-gameguild',
    title: 'Boss AI',
    status: 'published',
    visibility: 'public',
    enrolledCount: 42,
    completionPercent: 75,
    avgRating: '4.9',
  },
  {
    id: 'course-2',
    routeParam: 'cinematic-lighting-by-gameguild',
    title: 'Cinematic Lighting',
    status: 'draft',
    visibility: 'private',
    enrolledCount: 8,
    completionPercent: null,
    avgRating: null,
  },
  {
    id: 'course-3',
    routeParam: 'technical-art-by-gameguild',
    title: 'Technical Art',
    status: 'archived',
    visibility: 'private',
    enrolledCount: 12,
    completionPercent: 20,
    avgRating: '3.8',
  },
];

describe('CourseList', () => {
  it('renders grid cards and filters by search and status', async () => {
    const user = userEvent.setup();

    render(<CourseList courses={courses} locale="en-US" />);

    expect(screen.getByRole('link', { name: /boss ai/i })).toHaveAttribute(
      'href',
      '/workspace/learning/courses/boss-ai-by-gameguild',
    );
    expect(screen.getByText('42 enrolled')).toBeInTheDocument();

    await user.type(screen.getByPlaceholderText(/search courses/i), 'lighting');
    expect(screen.getByText('Showing 1 of 3 courses')).toBeInTheDocument();
    expect(screen.getByText('Cinematic Lighting')).toBeInTheDocument();
    expect(screen.queryByText('Boss AI')).not.toBeInTheDocument();

    await user.clear(screen.getByPlaceholderText(/search courses/i));
    await user.click(screen.getByRole('combobox', { name: /course status filter/i }));
    await user.click(await screen.findByRole('option', { name: /archived/i }));

    expect(screen.getByText('Technical Art')).toBeInTheDocument();
    expect(screen.queryByText('Boss AI')).not.toBeInTheDocument();
  });

  it('switches to table view, sorts rows, and exposes row actions', async () => {
    const user = userEvent.setup();

    render(<CourseList courses={courses} locale="en-US" />);

    await user.click(screen.getByRole('button', { name: /table view/i }));
    expect(screen.getByText('All Courses')).toBeInTheDocument();

    await user.click(screen.getByText('Enrolled'));
    const rows = screen.getAllByRole('row').slice(1);
    expect(within(rows[0]).getByText('Cinematic Lighting')).toBeInTheDocument();

    await user.click(screen.getByText('Enrolled'));
    const resortedRows = screen.getAllByRole('row').slice(1);
    expect(within(resortedRows[0]).getByText('Boss AI')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /open boss ai actions/i }));
    expect(await screen.findByRole('menuitem', { name: /^edit$/i })).toBeInTheDocument();
    expect(screen.getByRole('menuitem', { name: /preview/i })).toBeInTheDocument();
    expect(screen.getByRole('menuitem', { name: /manage lifecycle/i })).toBeInTheDocument();
  });

  it('covers table fallbacks, alternate sorts, and grid view toggle', () => {
    const courseWithUnknownStatus = {
      id: 'course-4',
      title: 'Unlisted Prototype',
      status: 'review',
      visibility: 'private',
      enrolledCount: 1,
      completionPercent: null,
      avgRating: null,
    };

    render(<CourseList courses={[...courses, courseWithUnknownStatus]} locale="en-US" />);

    fireEvent.click(screen.getByRole('button', { name: /table view/i }));
    expect(screen.getByText('All Courses')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /unlisted prototype/i })).toHaveAttribute(
      'href',
      '/workspace/learning/courses/course-4',
    );
    expect(screen.getAllByText('—').length).toBeGreaterThanOrEqual(2);

    fireEvent.click(screen.getByText('Completion'));
    let rows = screen.getAllByRole('row').slice(1);
    expect(rows.length).toBe(4);
    expect(within(rows[0]).getAllByText('—').length).toBeGreaterThan(0);

    fireEvent.click(screen.getByText('Rating'));
    rows = screen.getAllByRole('row').slice(1);
    expect(rows.length).toBe(4);
    expect(within(rows[0]).getAllByText('—').length).toBeGreaterThan(0);

    fireEvent.click(screen.getByRole('button', { name: /grid view/i }));
    expect(screen.queryByText('All Courses')).not.toBeInTheDocument();
    expect(screen.getByText('Unlisted Prototype')).toBeInTheDocument();
  });

  it('uses singular result copy when a single course is available', async () => {
    const user = userEvent.setup();

    render(<CourseList courses={[courses[0]]} locale="en-US" />);

    await user.type(screen.getByPlaceholderText(/search courses/i), 'boss');
    expect(screen.getByText('Showing 1 of 1 course')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /table view/i }));
    expect(screen.getByText('1 course')).toBeInTheDocument();
  });

  it('renders the empty state for no courses and no filtered results', async () => {
    const user = userEvent.setup();
    const { rerender } = render(<CourseList courses={[]} locale="en-US" />);

    expect(screen.getByText('No courses found')).toBeInTheDocument();
    expect(screen.getByText('Create your first course to start teaching.')).toBeInTheDocument();

    rerender(<CourseList courses={courses} locale="en-US" />);
    await user.type(screen.getByPlaceholderText(/search courses/i), 'missing course');
    expect(screen.getByText('Try adjusting your search or filter criteria.')).toBeInTheDocument();
  });
});
