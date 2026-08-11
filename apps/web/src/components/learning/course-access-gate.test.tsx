import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  enroll: vi.fn(),
  replace: vi.fn(),
}));

vi.mock('@/lib/learner/enrollment-actions', () => ({
  enrollInCourse: mocks.enroll,
}));

vi.mock('@/i18n/navigation', () => ({
  useRouter: () => ({ replace: mocks.replace }),
  Link: ({ children, href, ...props }: { children: ReactNode; href: string }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

const { CourseAccessGate } = await import('./course-access-gate');

describe('CourseAccessGate', () => {
  it('enters course content through the App Router after enrollment', async () => {
    mocks.enroll.mockResolvedValue({ success: true });

    render(
      <CourseAccessGate
        access={{
          kind: 'enrollment-required',
          course: {
            id: 'course-1',
            slug: 'game-production',
            title: 'Game Production',
          },
        } as never}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Enroll for free' }));

    await waitFor(() => {
      expect(mocks.enroll).toHaveBeenCalledWith('course-1');
      expect(mocks.replace).toHaveBeenCalledWith('/learn/courses/game-production/content');
    });
  });
});
