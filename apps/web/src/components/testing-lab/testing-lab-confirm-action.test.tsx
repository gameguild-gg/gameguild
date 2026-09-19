import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  error: vi.fn(),
  push: vi.fn(),
  success: vi.fn(),
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/workspace/learning',
  useRouter: () => ({ push: mocks.push }),
}));

vi.mock('sonner', () => ({
  toast: mocks,
}));

import { TestingLabConfirmAction } from './testing-lab-confirm-action';

describe('TestingLabConfirmAction', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

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

  it('navigates to the configured destination after a successful mutation', async () => {
    const action = vi.fn().mockResolvedValue({
      success: true,
      message: 'Testing request archived.',
    });

    render(
      <TestingLabConfirmAction
        action={action}
        fields={{ requestId: 'request-1' }}
        label="Archive"
        title="Archive this testing request?"
        description="The request is hidden from active operations."
        confirmLabel="Archive request"
        successHref="/workspace/testing-lab/projects"
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Archive' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Archive request' }));

    await waitFor(() => {
      expect(mocks.push).toHaveBeenCalledWith('/workspace/testing-lab/projects');
    });
  });

  it('cancels the delayed dialog close when the component unmounts', async () => {
    const setTimeoutSpy = vi.spyOn(window, 'setTimeout');
    const clearTimeoutSpy = vi.spyOn(window, 'clearTimeout');
    const action = vi.fn().mockResolvedValue({
      success: true,
      message: 'Testing location archived.',
    });

    const { unmount } = render(
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
      expect(setTimeoutSpy).toHaveBeenCalledWith(expect.any(Function), 650);
    });
    const closeTimerIndex = setTimeoutSpy.mock.calls.findIndex(([, delay]) => delay === 650);
    const closeTimer = setTimeoutSpy.mock.results[closeTimerIndex]?.value;

    unmount();

    expect(clearTimeoutSpy).toHaveBeenCalledWith(closeTimer);
  });
});
