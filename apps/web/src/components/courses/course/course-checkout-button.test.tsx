import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CourseCheckoutButton } from './course-checkout-button';

const mocks = vi.hoisted(() => ({
  completeCourseCheckout: vi.fn(),
  push: vi.fn(),
  refresh: vi.fn(),
}));

vi.mock('@/lib/courses/actions/enrollment.actions', () => ({
  completeCourseCheckout: mocks.completeCourseCheckout,
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/workspace/learning',
  useRouter: () => ({
    push: mocks.push,
    refresh: mocks.refresh,
  }),
}));

describe('CourseCheckoutButton', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.completeCourseCheckout.mockResolvedValue({
      success: true,
      message: 'Checkout complete. Your course access is active.',
      learningUrl: '/learn/courses/paid-course/content',
    });
  });

  it('opens checkout, completes payment, and navigates to classroom access', async () => {
    const user = userEvent.setup();

    render(
      <CourseCheckoutButton
        courseSlug="paid-course"
        products={[
          {
            id: 'product-1',
            name: 'Course access',
            price: 49,
            currency: 'USD',
          },
        ]}
      />,
    );

    await user.click(screen.getByRole('button', { name: /enroll for \$49/i }));
    expect(screen.getByRole('dialog', { name: /complete enrollment/i })).toBeInTheDocument();
    expect(screen.getByText(/total due today/i)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /confirm and enter classroom/i }));

    expect(mocks.completeCourseCheckout).toHaveBeenCalledWith('paid-course', 'product-1');
    expect(mocks.refresh).not.toHaveBeenCalled();
    expect(mocks.push).toHaveBeenCalledWith('/learn/courses/paid-course/content');
  });

  it('allows selecting a different product before checkout', async () => {
    const user = userEvent.setup();

    render(
      <CourseCheckoutButton
        courseSlug="bundle-course"
        products={[
          {
            id: 'course-product',
            name: 'Course only',
            price: 49,
            currency: 'USD',
          },
          {
            id: 'bundle-product',
            name: 'Program bundle',
            price: 149,
            currency: 'USD',
          },
        ]}
      />,
    );

    await user.click(screen.getByRole('button', { name: /enroll for \$49/i }));
    await user.click(screen.getByText(/program bundle/i));
    await user.click(screen.getByRole('button', { name: /confirm and enter classroom/i }));

    expect(mocks.completeCourseCheckout).toHaveBeenCalledWith('bundle-course', 'bundle-product');
  });
});
