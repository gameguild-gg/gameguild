import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import {
  LearnerRouteError,
  LearnerRouteLoading,
  LearnerRouteNotFound,
} from './learner-route-state';

describe('learner route states', () => {
  it('announces course loading without relying on visible placeholder text', () => {
    render(<LearnerRouteLoading scope="course" />);

    expect(screen.getByRole('status')).toHaveAttribute('aria-busy', 'true');
    expect(screen.getByText('Loading course workspace')).toHaveClass('sr-only');
  });

  it('offers a deterministic recovery path from course errors', () => {
    const reset = vi.fn();
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    render(<LearnerRouteError error={new Error('Network failure')} reset={reset} scope="course" />);

    expect(screen.getByRole('alert')).toHaveAccessibleName('Course workspace could not be loaded');
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    expect(reset).toHaveBeenCalledOnce();
  });

  it('explains unavailable resources and returns to enrolled courses', () => {
    render(<LearnerRouteNotFound scope="course" />);

    expect(screen.getByRole('heading', { name: 'Course resource not found' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Return to my courses' })).toHaveAttribute(
      'href',
      '/courses',
    );
  });
});
