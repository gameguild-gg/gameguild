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
});
