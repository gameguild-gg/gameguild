import { describe, expect, it, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { NextIntlClientProvider } from 'next-intl';
import userEvent from '@testing-library/user-event';
import { renderWithUser } from '@/test/auth-test-helpers';
import enMessages from '@/i18n/messages/en-US.json';

const mocks = vi.hoisted(() => ({
  changePasswordAction: vi.fn(),
  update: vi.fn(),
  toastSuccess: vi.fn(),
  toastError: vi.fn(),
}));

vi.mock('@/lib/auth/password-change-action', () => ({
  changePasswordAction: mocks.changePasswordAction,
}));

vi.mock('@game-guild/client/react', () => ({
  useSession: () => ({ data: null, status: 'authenticated', update: mocks.update }),
}));

vi.mock('sonner', () => ({
  toast: { success: mocks.toastSuccess, error: mocks.toastError },
}));

global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

const { PasswordCard } = await import('@/components/password-card');

function renderCard() {
  return renderWithUser(
    <NextIntlClientProvider locale="en-US" messages={enMessages}>
      <PasswordCard />
    </NextIntlClientProvider>,
  );
}

describe('PasswordCard', () => {
  beforeEach(() => {
    mocks.changePasswordAction.mockReset();
    mocks.update.mockReset();
    mocks.toastSuccess.mockReset();
    mocks.toastError.mockReset();
  });

  it('renders all fields, the policy hint, and the revoke checkbox (default checked)', () => {
    renderCard();

    expect(screen.getByLabelText('Current password')).toBeInTheDocument();
    expect(screen.getByLabelText('New password')).toBeInTheDocument();
    expect(screen.getByLabelText('Confirm new password')).toBeInTheDocument();
    expect(screen.getByText('Leave blank if you never set a password.')).toBeInTheDocument();
    expect(
      screen.getByText('At least 8 characters, with upper and lower case letters, a number, and a special character.'),
    ).toBeInTheDocument();
    expect(screen.getByLabelText('Sign out other devices')).toBeChecked();
    expect(screen.getByRole('button', { name: 'Change password' })).toBeInTheDocument();
  });

  it('blocks submit and shows an inline error when passwords do not match', async () => {
    const { user } = renderCard();

    await user.type(screen.getByLabelText('New password'), 'NewPassw0rd!');
    await user.type(screen.getByLabelText('Confirm new password'), 'Different0!');
    await user.click(screen.getByRole('button', { name: 'Change password' }));

    expect(screen.getByText('Passwords do not match.')).toBeInTheDocument();
    expect(mocks.changePasswordAction).not.toHaveBeenCalled();
  });

  it('submits the form payload when passwords match', async () => {
    mocks.changePasswordAction.mockResolvedValue({ success: true, status: 'success' });
    const { user } = renderCard();

    await user.type(screen.getByLabelText('Current password'), 'Admin123!');
    await user.type(screen.getByLabelText('New password'), 'NewPassw0rd!');
    await user.type(screen.getByLabelText('Confirm new password'), 'NewPassw0rd!');
    await user.click(screen.getByRole('button', { name: 'Change password' }));

    await waitFor(() => {
      expect(mocks.changePasswordAction).toHaveBeenCalledWith({
        currentPassword: 'Admin123!',
        newPassword: 'NewPassw0rd!',
        confirmPassword: 'NewPassw0rd!',
        revokeOtherSessions: true,
      });
    });
  });

  it('refreshes the session and toasts success after a successful change', async () => {
    mocks.changePasswordAction.mockResolvedValue({ success: true, status: 'success' });
    mocks.update.mockResolvedValue(null);
    const { user } = renderCard();

    await user.type(screen.getByLabelText('New password'), 'NewPassw0rd!');
    await user.type(screen.getByLabelText('Confirm new password'), 'NewPassw0rd!');
    await user.click(screen.getByRole('button', { name: 'Change password' }));

    await waitFor(() => expect(mocks.update).toHaveBeenCalled());
    expect(mocks.toastSuccess).toHaveBeenCalledWith('Password updated.');
    expect(mocks.toastError).not.toHaveBeenCalled();
  });

  it('toasts the localized wrong-current error on wrongCurrent status', async () => {
    mocks.changePasswordAction.mockResolvedValue({ success: false, status: 'wrongCurrent' });
    const { user } = renderCard();

    await user.type(screen.getByLabelText('New password'), 'NewPassw0rd!');
    await user.type(screen.getByLabelText('Confirm new password'), 'NewPassw0rd!');
    await user.click(screen.getByRole('button', { name: 'Change password' }));

    await waitFor(() =>
      expect(mocks.toastError).toHaveBeenCalledWith('Your current password is incorrect.'),
    );
    expect(mocks.update).not.toHaveBeenCalled();
  });
});
