import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  error: vi.fn(),
  success: vi.fn(),
}));

vi.mock('sonner', () => ({
  toast: mocks,
}));

import { TestingLabConfirmAction } from './testing-lab-confirm-action';

describe('TestingLabConfirmAction', () => {
  it('publishes successful mutation feedback outside the row that may unmount', async () => {
    const action = vi.fn().mockResolvedValue({
      success: true,
      message: 'Testing location archived.',
    });

    render(
      <TestingLabConfirmAction
        action={action}
        fields={{ locationId: 'location-1' }}
        label="Archive"
        title="Archive this location?"
        description="The location is hidden from scheduling."
        confirmLabel="Archive location"
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Archive' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Archive location' }));

    await waitFor(() => {
      expect(action).toHaveBeenCalledOnce();
      expect(mocks.success).toHaveBeenCalledWith('Testing location archived.');
    });
  });
});
