import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { NextIntlClientProvider } from 'next-intl';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import enMessages from '@/i18n/messages/en-US.json';
import {
  NotificationPreferences,
  type NotificationPreferencesData,
  type NotificationTypeCatalogItem,
} from './notification-preferences';

const actionMocks = vi.hoisted(() => ({
  updatePreferenceFlagsAction: vi.fn(),
  updateMutedTypesAction: vi.fn(),
  updateDigestFrequencyAction: vi.fn(),
  updateQuietHoursAction: vi.fn(),
}));

vi.mock('@/lib/notifications/preferences-action', () => actionMocks);

const preferences: NotificationPreferencesData = {
  emailEnabled: true,
  inAppEnabled: true,
  pushEnabled: false,
  smsEnabled: false,
  marketingEnabled: true,
  socialEnabled: true,
  learningEnabled: true,
  achievementsEnabled: true,
  emailDigestFrequency: null,
  quietHoursStart: '22:00:00',
  quietHoursEnd: '07:00:00',
  timezone: 'UTC',
  mutedTypes: ['MonthlyStatement'],
};

const catalog: NotificationTypeCatalogItem[] = [
  { type: 'MonthlyStatement', displayName: 'Monthly Statement', category: 'Billing', suppressible: true },
  { type: 'Billing', displayName: 'Billing', category: 'Billing', suppressible: true },
  { type: 'FeatureAnnouncement', displayName: 'Feature Announcement', category: 'Marketing', suppressible: true },
  { type: 'DirectMessage', displayName: 'Direct Message', category: 'Social', suppressible: true },
  { type: 'PasswordReset', displayName: 'Password Reset', category: 'Transactional', suppressible: false },
  { type: 'EmailVerification', displayName: 'Email Verification', category: 'Transactional', suppressible: false },
];

function renderPreferences(overrides?: Partial<NotificationPreferencesData>) {
  render(
    <NextIntlClientProvider locale="en-US" messages={enMessages}>
      <NotificationPreferences preferences={{ ...preferences, ...overrides }} catalog={catalog} />
    </NextIntlClientProvider>,
  );
}

describe('NotificationPreferences', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    actionMocks.updatePreferenceFlagsAction.mockResolvedValue({ success: true, status: 'success' });
    actionMocks.updateMutedTypesAction.mockResolvedValue({ success: true, status: 'success' });
    actionMocks.updateDigestFrequencyAction.mockResolvedValue({ success: true, status: 'success' });
    actionMocks.updateQuietHoursAction.mockResolvedValue({ success: true, status: 'success' });
  });

  it('renders channel and category sections from preferences', () => {
    renderPreferences();

    expect(screen.getByText('Delivery channels')).toBeInTheDocument();
    expect(screen.getByText('Email')).toBeInTheDocument();
    expect(screen.getByText('In-app')).toBeInTheDocument();
    expect(screen.getByText('Push')).toBeInTheDocument();
    expect(screen.getByText('SMS')).toBeInTheDocument();
    expect(screen.getByText('Categories')).toBeInTheDocument();
    expect(screen.getByTestId('category-marketing')).toBeInTheDocument();
    expect(screen.getByTestId('category-learning')).toBeInTheDocument();
  });

  it('renders catalog types grouped by category', () => {
    renderPreferences();

    expect(screen.getByTestId('type-group-Billing')).toBeInTheDocument();
    expect(screen.getByTestId('type-group-Marketing')).toBeInTheDocument();
    expect(screen.getByTestId('type-group-Social')).toBeInTheDocument();
    expect(screen.getByTestId('type-group-Transactional')).toBeInTheDocument();
    expect(screen.getByText('Monthly Statement')).toBeInTheDocument();
    expect(screen.getByText('Feature Announcement')).toBeInTheDocument();
  });

  it('renders transactional types with an always-sent hint instead of a toggle', () => {
    renderPreferences();

    const transactional = screen.getByTestId('type-PasswordReset');
    expect(transactional).toHaveTextContent('Always sent');
    expect(transactional.querySelector('button[role="switch"]')).toBeNull();

    expect(screen.getByTestId('type-EmailVerification')).toHaveTextContent('Always sent');
  });

  it('renders initially muted suppressible types as off', () => {
    renderPreferences();

    const muted = screen
      .getByTestId('type-MonthlyStatement')
      .querySelector('button[role="switch"]');
    expect(muted).toHaveAttribute('data-state', 'unchecked');

    const active = screen.getByTestId('type-Billing').querySelector('button[role="switch"]');
    expect(active).toHaveAttribute('data-state', 'checked');
  });

  it('unmuting a type sends the full replacement list including existing mutes', async () => {
    renderPreferences();

    fireEvent.click(
      screen.getByTestId('type-MonthlyStatement').querySelector('button[role="switch"]')!,
    );

    await waitFor(() => {
      expect(actionMocks.updateMutedTypesAction).toHaveBeenCalledWith([]);
    });
  });

  it('muting a type keeps already muted types in the payload', async () => {
    renderPreferences();

    fireEvent.click(
      screen.getByTestId('type-Billing').querySelector('button[role="switch"]')!,
    );

    await waitFor(() => {
      expect(actionMocks.updateMutedTypesAction).toHaveBeenCalledWith([
        'MonthlyStatement',
        'Billing',
      ]);
    });
  });

  it('toggling a channel sends just that flag', async () => {
    renderPreferences();

    fireEvent.click(screen.getByTestId('channel-email').querySelector('button[role="switch"]')!);

    await waitFor(() => {
      expect(actionMocks.updatePreferenceFlagsAction).toHaveBeenCalledWith({ emailEnabled: false });
    });
  });

  it('rolls a failed channel toggle back to the previous state', async () => {
    actionMocks.updatePreferenceFlagsAction.mockResolvedValue({
      success: false,
      status: 'error',
    });
    renderPreferences();

    const toggle = screen.getByTestId('channel-email').querySelector('button[role="switch"]')!;
    fireEvent.click(toggle);

    await waitFor(() => {
      expect(toggle).toHaveAttribute('data-state', 'checked');
    });
  });

  it('selecting a digest frequency sends the mapped value', async () => {
    renderPreferences();

    fireEvent.click(screen.getByRole('combobox'));
    fireEvent.click(screen.getByRole('option', { name: 'Weekly' }));

    await waitFor(() => {
      expect(actionMocks.updateDigestFrequencyAction).toHaveBeenCalledWith('Weekly');
    });
  });

  it('saving quiet hours sends normalized times and timezone', async () => {
    renderPreferences();

    fireEvent.change(screen.getByLabelText('Start'), { target: { value: '23:00' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save quiet hours' }));

    await waitFor(() => {
      expect(actionMocks.updateQuietHoursAction).toHaveBeenCalledWith(
        '23:00:00',
        '07:00:00',
        'UTC',
      );
    });
  });

  it('clearing both quiet hours inputs sends nulls', async () => {
    renderPreferences();

    fireEvent.change(screen.getByLabelText('Start'), { target: { value: '' } });
    fireEvent.change(screen.getByLabelText('End'), { target: { value: '' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save quiet hours' }));

    await waitFor(() => {
      expect(actionMocks.updateQuietHoursAction).toHaveBeenCalledWith(null, null, 'UTC');
    });
  });
});
