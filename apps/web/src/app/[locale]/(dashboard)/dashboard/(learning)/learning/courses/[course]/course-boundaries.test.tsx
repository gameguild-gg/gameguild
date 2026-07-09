import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import type React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: React.ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

import CourseLoading from './loading';
import CourseError from './error';
import CourseNotFound from './not-found';
import CourseForbidden from './forbidden';
import CourseUnauthorized from './unauthorized';
import CoursesLoading from '../loading';
import OverviewLoading from './overview/loading';
import ClassesLoading from './classes/loading';
import ClassDetailError from './classes/[classId]/error';
import ClassDetailLoading from './classes/[classId]/loading';
import ClassDetailNotFound from './classes/[classId]/not-found';
import ContentLoading from './content/loading';
import ContentItemError from './content/[contentId]/error';
import ContentItemLoading from './content/[contentId]/loading';
import ContentItemNotFound from './content/[contentId]/not-found';
import SettingsLoading from './settings/loading';
import StudentsLoading from './students/loading';

describe('course-management loading and boundary screens', () => {
  beforeEach(() => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  it('renders primary course loading and recovery states', () => {
    const { container, rerender } = render(<CoursesLoading />);
    expect(container.querySelectorAll('.animate-pulse').length).toBeGreaterThan(0);

    rerender(<CourseLoading />);
    expect(container.querySelectorAll('.animate-pulse').length).toBeGreaterThan(0);

    const reset = vi.fn();
    rerender(<CourseError error={Object.assign(new Error('Course failed'), { digest: 'abc123' })} reset={reset} />);
    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
    expect(screen.getByText('Course failed')).toBeInTheDocument();
    expect(screen.getByText('Reference: abc123')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /try again/i }));
    expect(reset).toHaveBeenCalled();

    rerender(<CourseNotFound />);
    expect(screen.getByText('Course not found')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /back to courses/i })).toHaveAttribute('href', '/dashboard/learning/courses');

    rerender(<CourseForbidden />);
    expect(screen.getByText('Access denied')).toBeInTheDocument();

    rerender(<CourseUnauthorized />);
    expect(screen.getByText('Sign in required')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /sign in/i })).toHaveAttribute('href', '/sign-in');
  });

  it('renders loading placeholders for course subroutes', () => {
    const { rerender } = render(<OverviewLoading />);
    expect(document.querySelectorAll('.animate-pulse').length).toBeGreaterThan(0);

    rerender(<ClassesLoading />);
    expect(screen.getByText('Loading class schedule...')).toBeInTheDocument();

    rerender(<StudentsLoading />);
    expect(document.querySelectorAll('.animate-pulse').length).toBeGreaterThan(0);

    rerender(<SettingsLoading />);
    expect(document.querySelectorAll('.animate-pulse').length).toBeGreaterThan(0);

    rerender(<ContentLoading />);
    expect(screen.getByText('Loading content structure...')).toBeInTheDocument();
  });

  it('renders class and content item boundaries', () => {
    const reset = vi.fn();
    const { rerender } = render(<ClassDetailError error={new Error('class failed')} reset={reset} />);
    expect(screen.getByText('Error loading class details. Please try again.')).toBeInTheDocument();

    rerender(<ClassDetailLoading />);
    expect(screen.getByText('Loading class details...')).toBeInTheDocument();

    rerender(<ClassDetailNotFound />);
    expect(screen.getByText('Class not found.')).toBeInTheDocument();

    rerender(<ContentItemError error={new Error('content failed')} reset={reset} />);
    expect(screen.getByText('Error loading content item. Please try again.')).toBeInTheDocument();

    rerender(<ContentItemLoading />);
    expect(screen.getByText('Loading content item...')).toBeInTheDocument();

    rerender(<ContentItemNotFound />);
    expect(screen.getByText('Content item not found.')).toBeInTheDocument();
  });
});
