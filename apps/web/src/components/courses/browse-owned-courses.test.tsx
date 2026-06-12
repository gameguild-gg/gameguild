import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BrowseOwnedCoursesPage } from './browse-owned-courses';

const push = vi.fn();

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push }),
}));

describe('BrowseOwnedCoursesPage', () => {
  beforeEach(() => {
    push.mockClear();
  });

  it('navigates learner actions to content, certificate, and catalog routes', async () => {
    render(<BrowseOwnedCoursesPage />);

    await userEvent.click(screen.getAllByRole('button', { name: /continue/i })[0]);
    expect(push).toHaveBeenCalledWith('/learning/courses/course-1/content/lesson-16');

    await userEvent.click(screen.getByRole('button', { name: /review course/i }));
    expect(push).toHaveBeenCalledWith('/learning/courses/course-2/content');

    await userEvent.click(screen.getByRole('button', { name: /open unity 3d essentials menu/i }));
    await userEvent.click(screen.getByRole('menuitem', { name: /view certificate/i }));
    expect(push).toHaveBeenCalledWith('/learning/certificates/course-2');
  });
});
