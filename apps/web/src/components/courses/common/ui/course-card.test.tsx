import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { CourseCard } from './course-card';

describe('CourseCard', () => {
  it('normalizes generated course status and difficulty values', async () => {
    const onEnroll = vi.fn();

    render(
      <CourseCard
        course={{
          id: 'course-1',
          title: 'Shader Production',
          description: 'Build a real-time material portfolio piece.',
          status: 'Published',
          difficulty: 'Expert',
          currentEnrollments: 7,
          estimatedHours: 18,
        }}
        onEnroll={onEnroll}
      />,
    );

    expect(screen.getByText('published')).toBeInTheDocument();
    expect(screen.getByText(/Level 4/)).toHaveTextContent('Level 4 • Expert');

    await userEvent.click(screen.getByRole('button', { name: /enroll/i }));

    expect(onEnroll).toHaveBeenCalledWith(expect.objectContaining({ id: 'course-1' }));
  });
});
