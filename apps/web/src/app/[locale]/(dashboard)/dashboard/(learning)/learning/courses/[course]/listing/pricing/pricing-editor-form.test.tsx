import '@testing-library/jest-dom/vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { PricingEditorForm } from './pricing-editor-form';

const updateCoursePricingMock = vi.fn();

vi.mock('@/lib/learning/actions', () => ({
  updateCoursePricing: (...args: unknown[]) => updateCoursePricingMock(...args),
}));

beforeAll(() => {
  global.ResizeObserver = class ResizeObserver {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
});

describe('PricingEditorForm', () => {
  beforeEach(() => {
    updateCoursePricingMock.mockReset();
    updateCoursePricingMock.mockResolvedValue({ success: true, data: null });
  });

  it('submits enabled pricing to the course pricing action', async () => {
    const user = userEvent.setup();

    render(
      <PricingEditorForm
        courseId="course-1"
        pricing={{
          tiers: [],
          discounts: [],
          refundPolicy: 'Free courses do not collect payment.',
          hasFreeTrial: false,
        }}
      />,
    );

    await user.click(screen.getByRole('switch', { name: /enable monetization/i }));
    await user.clear(screen.getByLabelText(/price/i));
    await user.type(screen.getByLabelText(/price/i), '149.99');
    await user.clear(screen.getByLabelText(/currency/i));
    await user.type(screen.getByLabelText(/currency/i), 'eur');

    await user.click(screen.getByRole('button', { name: /save pricing/i }));

    await waitFor(() => {
      expect(updateCoursePricingMock).toHaveBeenCalledWith({
        courseId: 'course-1',
        isMonetizationEnabled: true,
        price: 149.99,
        currency: 'EUR',
        isSubscription: false,
        subscriptionDurationDays: null,
      });
    });
    expect(screen.getByText('Pricing updated successfully.')).toBeInTheDocument();
  });
});
