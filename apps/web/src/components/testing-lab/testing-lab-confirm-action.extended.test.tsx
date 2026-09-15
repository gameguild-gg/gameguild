import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { TestingLabActionResult } from '@/lib/testing-lab/actions';

const mocks = vi.hoisted(() => ({
  error: vi.fn(),
  push: vi.fn(),
  success: vi.fn(),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: mocks.push }),
}));

vi.mock('sonner', () => ({ toast: mocks }));

import { TestingLabConfirmAction } from './testing-lab-confirm-action';

function renderAction({
  action,
  intent,
}: {
  action: (formData: FormData) => Promise<TestingLabActionResult<unknown>>;
  intent: 'archive' | 'delete' | 'restore';
}) {
  return render(
    <TestingLabConfirmAction
      action={action}
      fields={{ eventId: 'event-1', reason: 'cleanup' }}
      label={intent === 'delete' ? 'Delete' : intent === 'restore' ? 'Restore' : 'Archive'}
      title={`${intent} this event?`}
      description="This operation requires confirmation."
      confirmLabel={`Confirm ${intent}`}
      intent={intent}
    />,
  );
}

describe('TestingLabConfirmAction extended behavior', () => {
  beforeEach(() => vi.clearAllMocks());

  it('renders a destructive delete failure and clears it when cancelled', async () => {
    const action = vi.fn().mockResolvedValue({ success: false, error: 'Delete denied.' });
    renderAction({ action, intent: 'delete' });

    fireEvent.click(screen.getByRole('button', { name: 'Delete' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Confirm delete' }));
    expect(await screen.findByText('Delete denied.')).toBeInTheDocument();
    expect(mocks.error).toHaveBeenCalledWith('Delete denied.');
    const data = action.mock.calls[0]![0] as FormData;
    expect(Object.fromEntries(data.entries())).toEqual({ eventId: 'event-1', reason: 'cleanup' });

    await waitFor(() => expect(screen.getByRole('button', { name: 'Cancel' })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument());
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }));
    expect(screen.queryByText('Delete denied.')).not.toBeInTheDocument();
  });

  it.each([
    [new Error('Restore service offline'), 'Restore service offline'],
    ['failure', 'The Testing Lab operation failed.'],
  ])('turns thrown restore failures into visible feedback', async (failure, message) => {
    const action = vi.fn().mockRejectedValue(failure);
    renderAction({ action, intent: 'restore' });
    fireEvent.click(screen.getByRole('button', { name: 'Restore' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Confirm restore' }));
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(mocks.error).toHaveBeenCalledWith(message);
  });

  it('closes a successful restore after the feedback delay', async () => {
    const action = vi.fn().mockResolvedValue({ success: true, message: 'Event restored.' });
    renderAction({ action, intent: 'restore' });
    fireEvent.click(screen.getByRole('button', { name: 'Restore' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Confirm restore' }));
    await waitFor(() => expect(mocks.success).toHaveBeenCalledWith('Event restored.'));
    expect(mocks.push).not.toHaveBeenCalled();
    await waitFor(
      () => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument(),
      { timeout: 2_000 },
    );
  });
});
