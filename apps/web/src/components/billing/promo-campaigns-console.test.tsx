import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { PromoCampaignsConsole, type PromoCampaignRow } from './promo-campaigns-console';

function campaign(overrides: Partial<PromoCampaignRow> = {}): PromoCampaignRow {
  return {
    id: 'b1000000-0000-0000-0000-000000000001',
    name: 'Launch week',
    key: 'launch-week',
    description: null,
    status: 'Active',
    startsAtLocal: '2026-11-01T00:00:00',
    endsAtLocal: '2026-11-08T00:00:00',
    timeZoneId: 'America/Sao_Paulo',
    budget: 5000,
    acquisitionSpend: 1000,
    currency: 'USD',
    codeCount: 12,
    attributedCustomers: 7,
    customerAcquisitionCost: 14.29,
    ...overrides,
  };
}

describe('PromoCampaignsConsole', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('fetch', vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('renders campaign rows with lifecycle state and CAC attribution', () => {
    render(<PromoCampaignsConsole initialCampaigns={[campaign()]} />);
    expect(screen.getByText('Launch week')).toBeInTheDocument();
    expect(screen.getByText('launch-week')).toBeInTheDocument();
    expect(screen.getByText('Active')).toBeInTheDocument();
    expect(screen.getByTestId('campaign-row-b1000000-0000-0000-0000-000000000001')).toBeInTheDocument();
    expect(screen.getByText('$14.29')).toBeInTheDocument();
    expect(screen.getByText(/America\/Sao_Paulo/)).toBeInTheDocument();
  });

  it('renders an empty-state row when no campaigns match', () => {
    render(<PromoCampaignsConsole initialCampaigns={[]} />);
    expect(screen.getByText(/No campaigns match the current filter/i)).toBeInTheDocument();
  });

  it('loads the performance report inline on demand', async () => {
    (globalThis.fetch as ReturnType<typeof vi.fn>).mockResolvedValue(
      new Response(
        JSON.stringify({
          campaignId: 'b1000000-0000-0000-0000-000000000001',
          totalUses: 40,
          uniqueUsers: 22,
          totalDiscountGiven: 310.5,
          conversionRate: 0.34,
          customerAcquisitionCost: 142.85,
        }),
        { status: 200 },
      ),
    );
    render(<PromoCampaignsConsole initialCampaigns={[campaign()]} />);

    fireEvent.click(screen.getByRole('button', { name: /load report/i }));

    await waitFor(() => {
      expect(screen.getByTestId('uses-b1000000-0000-0000-0000-000000000001')).toHaveTextContent('40 / 22');
    });
    expect(screen.getByText('34.0%')).toBeInTheDocument();
    expect(screen.getByText('$142.85')).toBeInTheDocument();
  });

  it('surfaces a rejection from the performance API', async () => {
    (globalThis.fetch as ReturnType<typeof vi.fn>).mockResolvedValue(new Response('denied', { status: 403 }));
    render(<PromoCampaignsConsole initialCampaigns={[campaign()]} />);

    fireEvent.click(screen.getByRole('button', { name: /load report/i }));

    await waitFor(() => {
      expect(screen.getByText(/HTTP 403/)).toBeInTheDocument();
    });
  });

  it('issues a lifecycle transition and refreshes the campaign list', async () => {
    const fetchMock = globalThis.fetch as ReturnType<typeof vi.fn>;
    fetchMock.mockImplementation((_input: RequestInfo | URL, init?: RequestInit) =>
      init?.method === 'POST'
        ? Promise.resolve(new Response('{}', { status: 200 }))
        : Promise.resolve(new Response(JSON.stringify({ items: [campaign({ status: 'Paused' })] }), { status: 200 })),
    );
    render(<PromoCampaignsConsole initialCampaigns={[campaign()]} />);

    fireEvent.click(screen.getByRole('button', { name: /pause/i }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/billing/promo-campaigns',
        expect.objectContaining({ method: 'POST', body: JSON.stringify({ campaignId: campaign().id, action: 'pause' }) }),
      );
    });
    await waitFor(() => {
      expect(screen.getByText('Paused')).toBeInTheDocument();
    });
  });
});
