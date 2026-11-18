import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  loadSessionsData: vi.fn(),
  loadMfaData: vi.fn(),
}));

vi.mock('next-intl/server', () => ({
  getTranslations: async (namespace: string) => (key: string) => `${namespace}.${key}`,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock('@/lib/auth/sessions-actions', () => ({
  loadSessionsData: mocks.loadSessionsData,
}));

vi.mock('@/lib/auth/mfa-actions', () => ({
  loadMfaData: mocks.loadMfaData,
}));

vi.mock('@/components/settings/security-policies-card', () => ({
  SecurityPoliciesCard: () => <div data-testid="security-policies-card" />,
}));

vi.mock('@/components/settings/mfa-methods-manager', () => ({
  MfaMethodsManager: () => <div data-testid="mfa-methods-manager" />,
}));

vi.mock('@/components/settings/sessions-manager', () => ({
  SessionsManager: () => <div data-testid="sessions-manager" />,
}));

const { default: SecuritySettingsPage } = await import('./page');

const OK_SESSIONS = {
  sessions: [{ id: '11111111-1111-1111-1111-111111111111', isCurrent: true }],
  analysis: null,
  trustedDevices: [],
  loadStatus: 'ok' as const,
};

const OK_MFA = {
  configuration: { isEnabled: false },
  methods: { methods: [] },
  backupCodes: null,
  loadStatus: 'ok' as const,
};

describe('SecuritySettingsPage (issue #1298 J4: load failures render an explicit error state, never a blank page)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.loadSessionsData.mockResolvedValue(OK_SESSIONS);
    mocks.loadMfaData.mockResolvedValue(OK_MFA);
  });

  it('renders the managers when both loaders succeed', async () => {
    render(await SecuritySettingsPage({ params: Promise.resolve({ locale: 'en-US' }) }));

    expect(screen.getByTestId('security-policies-card')).toBeInTheDocument();
    expect(screen.getByTestId('mfa-methods-manager')).toBeInTheDocument();
    expect(screen.getByTestId('sessions-manager')).toBeInTheDocument();
    expect(screen.queryByTestId('security-load-error')).not.toBeInTheDocument();
  });

  it('renders the destructive load-error alert with a retry instead of the session manager when the session reads fail', async () => {
    mocks.loadSessionsData.mockResolvedValue({
      sessions: [],
      analysis: null,
      trustedDevices: [],
      loadStatus: 'serverError',
    });

    render(await SecuritySettingsPage({ params: Promise.resolve({ locale: 'en-US' }) }));

    expect(screen.getByTestId('security-load-error')).toBeInTheDocument();
    expect(screen.getByText('settings.security.loadError.title')).toBeInTheDocument();
    expect(screen.getByText('settings.security.loadError.serverError')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /settings\.security\.loadError\.retry/ })).toBeInTheDocument();
    expect(screen.queryByTestId('sessions-manager')).not.toBeInTheDocument();
    expect(screen.queryByTestId('mfa-methods-manager')).not.toBeInTheDocument();
  });

  it('renders the contract-drift message when the generated client throws on MFA reads', async () => {
    mocks.loadMfaData.mockResolvedValue({
      configuration: null,
      methods: null,
      backupCodes: null,
      loadStatus: 'contractError',
    });

    render(await SecuritySettingsPage({ params: Promise.resolve({ locale: 'en-US' }) }));

    expect(screen.getByTestId('security-load-error')).toBeInTheDocument();
    expect(screen.getByText('settings.security.loadError.contractError')).toBeInTheDocument();
    expect(screen.queryByTestId('mfa-methods-manager')).not.toBeInTheDocument();
    expect(screen.queryByTestId('sessions-manager')).not.toBeInTheDocument();
  });

  it('prefers the unauthorized message when either loader reports an expired session', async () => {
    mocks.loadMfaData.mockResolvedValue({
      configuration: null,
      methods: null,
      backupCodes: null,
      loadStatus: 'unauthorized',
    });

    render(await SecuritySettingsPage({ params: Promise.resolve({ locale: 'en-US' }) }));

    expect(screen.getByText('settings.security.loadError.unauthorized')).toBeInTheDocument();
    expect(screen.queryByText('settings.security.loadError.serverError')).not.toBeInTheDocument();
  });
});
