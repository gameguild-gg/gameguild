import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getRequestAuthContext: vi.fn(),
  getPromoCampaignsForGetPromoCampaigns: vi.fn(),
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getRequestAuthContext: mocks.getRequestAuthContext,
}));

vi.mock('@/i18n/navigation', () => ({
  redirect: vi.fn(() => {
    throw new Error('redirect');
  }),
}));

vi.mock('@/components/billing/promo-campaigns-console', () => ({
  PromoCampaignsConsole: () => <div data-testid="promo-campaigns-console" />,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    CommerceProductsPromoCampaignsModule: class {
      getPromoCampaignsForGetPromoCampaigns = mocks.getPromoCampaignsForGetPromoCampaigns;
    },
  },
}));

const { default: PromoCampaignsPage } = await import('./page');

function apiOk(data: unknown) {
  return { ok: true as const, data };
}

function apiError(status: number) {
  return { ok: false as const, error: { name: 'ApiError' as const, status, code: 'ERROR' as const, message: 'boom' } };
}

const campaign = {
  id: 'b1000000-0000-0000-0000-000000000001',
  name: 'Launch week',
  key: 'launch-week',
  description: null,
  status: 'Active',
  startsAtLocal: null,
  endsAtLocal: null,
  timeZoneId: 'America/Sao_Paulo',
  budget: 5000,
  acquisitionSpend: 1000,
  currency: 'USD',
  codeCount: 12,
  attributedCustomers: 7,
  customerAcquisitionCost: 14.29,
};

describe('PromoCampaignsPage (issue #1217: served /v1 prefix, no silent empty render)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'admin-1' } });
    mocks.getRequestAuthContext.mockResolvedValue({ token: 'session-token' });
    mocks.getPromoCampaignsForGetPromoCampaigns.mockResolvedValue(apiOk({ items: [campaign] }));
  });

  it('loads the first page through the generated list method and renders the console', async () => {
    render(await PromoCampaignsPage({ params: Promise.resolve({ locale: 'en' }) }));

    expect(mocks.getPromoCampaignsForGetPromoCampaigns).toHaveBeenCalledWith({ skip: 0, take: 50 });
    expect(screen.getByRole('heading', { name: 'Promo campaigns' })).toBeInTheDocument();
    expect(screen.getByTestId('promo-campaigns-console')).toBeInTheDocument();
  });

  it('renders the load-error fallback state instead of an empty dashboard when the API rejects the list', async () => {
    mocks.getPromoCampaignsForGetPromoCampaigns.mockResolvedValue(apiError(503));

    render(await PromoCampaignsPage({ params: Promise.resolve({ locale: 'en' }) }));

    expect(screen.getByText('Campaigns could not be loaded')).toBeInTheDocument();
    expect(screen.getByText(/HTTP 503/)).toBeInTheDocument();
    expect(screen.queryByTestId('promo-campaigns-console')).not.toBeInTheDocument();
  });

  it('renders the load-error fallback state when the generated client reports contract drift', async () => {
    mocks.getPromoCampaignsForGetPromoCampaigns.mockRejectedValue(new Error('Response validation failed'));

    render(await PromoCampaignsPage({ params: Promise.resolve({ locale: 'en' }) }));

    expect(screen.getByText('Campaigns could not be loaded')).toBeInTheDocument();
    expect(screen.queryByTestId('promo-campaigns-console')).not.toBeInTheDocument();
  });

  it('renders the console with an empty list when the API legitimately returns no campaigns', async () => {
    mocks.getPromoCampaignsForGetPromoCampaigns.mockResolvedValue(apiOk({ items: [] }));

    render(await PromoCampaignsPage({ params: Promise.resolve({ locale: 'en' }) }));

    expect(screen.getByTestId('promo-campaigns-console')).toBeInTheDocument();
    expect(screen.queryByText('Campaigns could not be loaded')).not.toBeInTheDocument();
  });
});
