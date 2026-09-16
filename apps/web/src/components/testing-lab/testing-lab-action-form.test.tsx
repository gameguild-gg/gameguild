import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { TestingLabActionForm } from './testing-lab-action-form';

describe('TestingLabActionForm', () => {
  it('submits form data and announces success', async () => {
    const action = vi.fn(async () => ({ success: true as const, data: null, message: 'Saved.' }));
    render(
      <TestingLabActionForm action={action} submitLabel="Save">
        <label htmlFor="name">Name</label>
        <input id="name" name="name" defaultValue="Remote lab" />
      </TestingLabActionForm>,
    );

    await userEvent.click(screen.getByRole('button', { name: 'Save' }));
    await waitFor(() => expect(action).toHaveBeenCalledOnce());
    expect((action.mock.calls[0]?.[0] as FormData).get('name')).toBe('Remote lab');
    expect(await screen.findByText('Saved.')).toBeVisible();
  });

  it('keeps form values and announces API errors', async () => {
    const action = vi.fn(async () => ({ success: false as const, error: 'Forbidden' }));
    render(
      <TestingLabActionForm action={action} submitLabel="Save">
        <input aria-label="Name" name="name" defaultValue="Keep me" />
      </TestingLabActionForm>,
    );

    fireEvent.submit(screen.getByRole('button', { name: 'Save' }).closest('form')!);
    expect(await screen.findByText('Forbidden')).toBeVisible();
    expect(screen.getByLabelText('Name')).toHaveValue('Keep me');
  });

  it('does not run an action while native form validation fails', () => {
    const action = vi.fn();
    render(
      <TestingLabActionForm action={action} submitLabel="Save">
        <input aria-label="Required name" name="name" required />
      </TestingLabActionForm>,
    );
    const form = screen.getByRole('button', { name: 'Save' }).closest('form')!;
    vi.spyOn(form, 'reportValidity').mockReturnValue(false);
    fireEvent.submit(form);
    expect(action).not.toHaveBeenCalled();
  });

  it('resets successful forms only when requested', async () => {
    const action = vi.fn(async () => ({ success: true as const, data: null, message: 'Created.' }));
    const user = userEvent.setup();
    render(
      <TestingLabActionForm action={action} submitLabel="Create" resetOnSuccess>
        <input aria-label="Name" name="name" defaultValue="" />
      </TestingLabActionForm>,
    );
    await user.type(screen.getByLabelText('Name'), 'Temporary value');
    await user.click(screen.getByRole('button', { name: 'Create' }));
    expect(await screen.findByText('Created.')).toBeVisible();
    expect(screen.getByLabelText('Name')).toHaveValue('');
  });

  it('runs a secondary action and exposes a shared pending state', async () => {
    let finish!: (value: { success: true; data: null; message: string }) => void;
    const secondaryAction = vi.fn(
      () => new Promise<{ success: true; data: null; message: string }>((resolve) => { finish = resolve; }),
    );
    const primaryAction = vi.fn();
    const user = userEvent.setup();
    render(
      <TestingLabActionForm
        action={primaryAction}
        submitLabel="Save"
        pendingLabel="Saving changes..."
        secondaryAction={secondaryAction}
        secondaryLabel="Archive"
        secondaryVariant="destructive"
      >
        <input name="eventId" value="event-1" readOnly />
      </TestingLabActionForm>,
    );

    await user.click(screen.getByRole('button', { name: 'Archive' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Saving changes...' })).toBeDisabled());
    expect(screen.getByRole('button', { name: 'Archive' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Archive' }).querySelector('.animate-spin')).not.toBeNull();
    expect(primaryAction).not.toHaveBeenCalled();
    expect((secondaryAction.mock.calls[0]?.[0] as FormData).get('eventId')).toBe('event-1');

    finish({ success: true, data: null, message: 'Archived.' });
    expect(await screen.findByText('Archived.')).toBeVisible();
  });

  it('omits incomplete secondary controls and normalizes rejected actions', async () => {
    const errorAction = vi.fn().mockRejectedValueOnce(new Error('API unavailable'));
    const secondaryAction = vi.fn();
    const { unmount } = render(
      <TestingLabActionForm
        action={errorAction}
        submitLabel="Save"
        secondaryAction={secondaryAction}
      >
        <input name="name" defaultValue="Test" />
      </TestingLabActionForm>,
    );
    expect(screen.getAllByRole('button')).toHaveLength(1);
    fireEvent.submit(screen.getByRole('button', { name: 'Save' }).closest('form')!);
    expect(await screen.findByText('API unavailable')).toBeVisible();
    unmount();

    const unknownErrorAction = vi.fn().mockRejectedValueOnce('offline');
    render(
      <TestingLabActionForm
        action={unknownErrorAction}
        submitLabel="Retry"
        secondaryLabel="Incomplete"
      >
        <input name="name" defaultValue="Test" />
      </TestingLabActionForm>,
    );
    expect(screen.getAllByRole('button')).toHaveLength(1);
    fireEvent.submit(screen.getByRole('button', { name: 'Retry' }).closest('form')!);
    expect(await screen.findByText('The Testing Lab operation failed.')).toBeVisible();
  });
});
