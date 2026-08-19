import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  getPreferences: vi.fn(),
  getTypesCatalog: vi.fn(),
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));

vi.mock('next-intl/server', () => ({
  getTranslations: async (opts: { namespace: string }) => (key: string) => `${opts.namespace}.${key}`,
}));

vi.mock('@/i18n/navigation', () => ({
  redirect: vi.fn(),
}));

vi.mock('@/components/settings/notifications/notification-preferences', () => ({
  NotificationPreferences: () => <div data-testid="notification-preferences" />,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    NotificationsModule: class {
      getApiNotificationsPreferences = mocks.getPreferences;
      getApiNotificationsTypesCatalog = mocks.getTypesCatalog;
    },
  },
}));

const { default: NotificationsSettingsPage } = await import('./page');

const preferencesPayload = {
  emailEnabled: true,
  inAppEnabled: true,
  pushEnabled: false,
  smsEnabled: false,
  marketingEnabled: true,
  socialEnabled: true,
  learningEnabled: true,
  achievementsEnabled: true,
  emailDigestFrequency: null,
  quietHoursStart: null,
  quietHoursEnd: null,
  timezone: null,
  mutedTypes: [],
};

const catalogPayload = [
  { type: 'Billing', displayName: 'Billing', category: 'Billing', suppressible: true },
];

function apiError(status: number) {
  return { ok: false as const, error: { name: 'ApiError' as const, status, code: 'ERROR' as const, message: 'boom' } };
}

describe('NotificationSettingsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });
    mocks.getToken.mockResolvedValue('access-token');
  });

  it('renders the preferences component when preferences and catalog load', async () => {
    mocks.getPreferences.mockResolvedValue({ ok: true, data: preferencesPayload });
    mocks.getTypesCatalog.mockResolvedValue({ ok: true, data: catalogPayload });

    const page = await NotificationsSettingsPage({
      params: Promise.resolve({ locale: 'en-US' }),
    });
    const { container } = render(<div>{page}</div>);

    expect(screen.getByTestId('notification-preferences')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'settings.notificationsTitle' })).toBeInTheDocument();
    expect(container.textContent).toContain('settings.notificationsDescription');
  });

  it('renders an error state instead of crashing when the API is down', async () => {
    mocks.getPreferences.mockResolvedValue(apiError(500));
    mocks.getTypesCatalog.mockResolvedValue(apiError(500));

    const page = await NotificationsSettingsPage({
      params: Promise.resolve({ locale: 'en-US' }),
    });
    render(<div>{page}</div>);

    expect(screen.queryByTestId('notification-preferences')).not.toBeInTheDocument();
    expect(
      screen.getByText('notificationPrefs.loadError.title'),
    ).toBeInTheDocument();
    expect(
      screen.getByText('notificationPrefs.loadError.description'),
    ).toBeInTheDocument();
  });

  it('renders an error state when the catalog comes back empty', async () => {
    mocks.getPreferences.mockResolvedValue({ ok: true, data: preferencesPayload });
    mocks.getTypesCatalog.mockResolvedValue({ ok: true, data: [] });

    const page = await NotificationsSettingsPage({
      params: Promise.resolve({ locale: 'en-US' }),
    });
    render(<div>{page}</div>);

    expect(screen.queryByTestId('notification-preferences')).not.toBeInTheDocument();
    expect(screen.getByText('notificationPrefs.loadError.title')).toBeInTheDocument();
  });
});
