import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  getEvents: vi.fn(),
  getSuppressions: vi.fn(),
  getDeadletters: vi.fn(),
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));

vi.mock('next-intl/server', () => ({
  getTranslations: async (opts: { namespace: string }) => (key: string) => `${opts.namespace}.${key}`,
}));

vi.mock('@/components/console/platform/email-deliverability/email-deliverability', () => ({
  EmailDeliverability: () => <div data-testid="email-deliverability" />,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    NotificationsModule: class {
      getEmailDeliveryEmailEvents = mocks.getEvents;
      getEmailDeliverySuppressions = mocks.getSuppressions;
      getEmailDeliveryDeadletters = mocks.getDeadletters;
    },
  },
}));

const { default: EmailDeliverabilityPage } = await import('./page');

const eventsPayload = {
  items: [
    {
      id: 'e-1',
      occurredAt: '2026-08-19T10:03:00Z',
      eventType: 'Bounce',
      recipientEmail: 'hard@bouncer.co',
      providerMessageId: 'ses-1',
      bounceType: 'Permanent',
      diagnosticCode: '5.1.1',
      payloadPreview: null,
    },
  ],
  totalCount: 1,
};

const suppressionsPayload = {
  items: [
    { id: 's-1', emailAddress: 'hard@bouncer.co', reason: 'HardBounce', bounceType: 'Permanent', suppressedAt: '2026-08-19T10:03:30Z', releasedAt: null, isActive: true },
  ],
  totalCount: 1,
};

const deadLettersPayload = {
  items: [
    { id: 'n-1', title: 'Monthly statement ready', type: 'MonthlyStatement', channel: 'Email', recipientEmail: 'hard@bouncer.co', recipientId: 'u-1', lastError: 'suppressed', attemptCount: 2, requeueCount: 0, createdAt: '2026-08-19T10:03:31Z' },
  ],
  totalCount: 1,
};

function apiOk(data: unknown) {
  return { ok: true as const, data };
}

function apiError(status: number) {
  return { ok: false as const, error: { name: 'ApiError' as const, status, code: 'ERROR' as const, message: 'boom' } };
}

describe('EmailDeliverabilityPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'admin-1' } });
    mocks.getToken.mockResolvedValue('token');
    mocks.getEvents.mockResolvedValue(apiOk(eventsPayload));
    mocks.getSuppressions.mockResolvedValue(apiOk(suppressionsPayload));
    mocks.getDeadletters.mockResolvedValue(apiOk(deadLettersPayload));
  });

  it('renders header and the deliverability component when all feeds load', async () => {
    render(await EmailDeliverabilityPage({ params: Promise.resolve({ locale: 'en-US' }) }));

    expect(screen.getByRole('heading', { name: 'emailDeliverability.title' })).toBeInTheDocument();
    expect(screen.getByTestId('email-deliverability')).toBeInTheDocument();
    expect(mocks.getEvents).toHaveBeenCalledWith({ take: 20 });
    expect(mocks.getSuppressions).toHaveBeenCalledWith({ take: 20, includeReleased: true });
    expect(mocks.getDeadletters).toHaveBeenCalledWith({ take: 20 });
  });

  it('renders the error state instead of crashing when the API is down', async () => {
    mocks.getEvents.mockResolvedValue(apiError(503));
    mocks.getSuppressions.mockResolvedValue(apiError(503));
    mocks.getDeadletters.mockResolvedValue(apiError(503));

    render(await EmailDeliverabilityPage({ params: Promise.resolve({ locale: 'en-US' }) }));

    expect(screen.getByText('emailDeliverability.loadError.title')).toBeInTheDocument();
    expect(screen.queryByTestId('email-deliverability')).not.toBeInTheDocument();
  });

  it('renders the error state when the caller lacks the admin policy (403)', async () => {
    mocks.getDeadletters.mockResolvedValue(apiError(403));

    render(await EmailDeliverabilityPage({ params: Promise.resolve({ locale: 'en-US' }) }));

    expect(screen.getByText('emailDeliverability.loadError.title')).toBeInTheDocument();
  });
});
