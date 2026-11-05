import { render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ auth: vi.fn(), context: vi.fn(), fetch: vi.fn(), redirect: vi.fn() }));
vi.mock('@/auth', () => ({ auth: mocks.auth, getRequestAuthContext: mocks.context }));
vi.mock('@/i18n/navigation', () => ({ redirect: mocks.redirect }));

import PromoCampaignsPage from './page';

const row = {
  id: 'b1000000-0000-0000-0000-000000000001', name: 'Launch week', key: 'launch-week', status: 'Active',
  timeZoneId: 'America/Sao_Paulo', currency: 'USD', codeCount: 12, attributedCustomers: 7, customerAcquisitionCost: 14.29,
};
const props = () => ({ params: Promise.resolve({ locale: 'en' }) });

describe('promo campaign server page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('fetch', mocks.fetch);
    vi.stubEnv('API_URL', 'http://api.example');
    mocks.auth.mockResolvedValue({ user: { id: 'admin' } });
    mocks.context.mockResolvedValue({ token: 'admin-token' });
    mocks.redirect.mockImplementation(() => { throw new Error('SIGN_IN_REDIRECT'); });
    mocks.fetch.mockImplementation((url: string) => Promise.resolve(new URL(url).pathname === '/v1/promo-campaigns'
      ? Response.json({ items: [row], totalCount: 1 })
      : new Response('not found', { status: 404 })));
  });
  afterEach(() => { vi.unstubAllGlobals(); vi.unstubAllEnvs(); });

  it('loads and renders the real campaign route with its session authorization', async () => {
    render(await PromoCampaignsPage(props()));
    expect(screen.getByText('Launch week')).toBeInTheDocument();
    expect(mocks.fetch).toHaveBeenCalledWith('http://api.example/v1/promo-campaigns?skip=0&take=50', expect.objectContaining({
      headers: expect.objectContaining({ authorization: 'Bearer admin-token' }), cache: 'no-store',
    }));
  });

  it.each([401, 403, 404, 500])('shows a safe error instead of a successful empty list for HTTP %s', async (status) => {
    mocks.fetch.mockResolvedValue(new Response('private upstream error <script>internal()</script>', { status }));
    render(await PromoCampaignsPage(props()));
    expect(screen.getByRole('alert')).toHaveTextContent(`HTTP ${status}`);
    expect(screen.queryByText(/No campaigns match/)).not.toBeInTheDocument();
    expect(screen.queryByText(/private upstream/)).not.toBeInTheDocument();
  });

  it('shows a safe error for a network failure', async () => {
    mocks.fetch.mockRejectedValue(new Error('private backend address'));
    render(await PromoCampaignsPage(props()));
    expect(screen.getByRole('alert')).toHaveTextContent(/campaigns could not be loaded/i);
    expect(screen.queryByText(/No campaigns match/)).not.toBeInTheDocument();
    expect(screen.queryByText(/private backend/)).not.toBeInTheDocument();
  });

  it.each(['not-json', '{}', 'null'])('does not treat an invalid success body as an empty list: %s', async (body) => {
    mocks.fetch.mockResolvedValue(new Response(body, { status: 200 }));
    render(await PromoCampaignsPage(props()));
    expect(screen.getByRole('alert')).toHaveTextContent(/campaigns could not be loaded/i);
    expect(screen.queryByText(/No campaigns match/)).not.toBeInTheDocument();
  });

  it('retains the empty state for an actual successful empty list', async () => {
    mocks.fetch.mockResolvedValue(Response.json({ items: [], totalCount: 0 }));
    render(await PromoCampaignsPage(props()));
    expect(screen.getByText(/No campaigns match/)).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('renders fresh server data after an earlier failed load instead of retaining a stale error', async () => {
    mocks.fetch.mockResolvedValueOnce(new Response('unavailable', { status: 500 }))
      .mockResolvedValueOnce(Response.json({ items: [row], totalCount: 1 }));
    const view = render(await PromoCampaignsPage(props()));
    expect(screen.getByRole('alert')).toBeInTheDocument();

    view.rerender(await PromoCampaignsPage(props()));

    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(screen.getByText('Launch week')).toBeInTheDocument();
  });

  it.each(['session', 'token'])('redirects before fetching when the %s is missing', async (missing) => {
    if (missing === 'session') mocks.auth.mockResolvedValue(null);
    else mocks.context.mockResolvedValue({ token: null });
    await expect(PromoCampaignsPage(props())).rejects.toThrow('SIGN_IN_REDIRECT');
    expect(mocks.fetch).not.toHaveBeenCalled();
  });
});
