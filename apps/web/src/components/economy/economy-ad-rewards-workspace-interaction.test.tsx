import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  callbacks: null as null | Record<string, (...args: never[]) => unknown>,
  cleanup: vi.fn(),
  complete: vi.fn(),
  request: vi.fn(),
  start: vi.fn(),
}));

vi.mock('@/lib/ads/google-ad-manager-web-rewarded-adapter', () => ({
  GoogleAdManagerWebRewardedAdapter: class {
    request(...args: unknown[]) { return mocks.request(...args); }
  },
}));
vi.mock('@/lib/economy/actions', () => ({
  completeAdRewardSessionAction: mocks.complete,
  startAdRewardSessionAction: mocks.start,
}));
vi.mock('next-intl', () => ({ useTranslations: () => (key: string) => key }));
vi.mock('@/i18n/navigation', () => ({ Link: ({ children, href }: React.AnchorHTMLAttributes<HTMLAnchorElement>) => <a href={String(href)}>{children}</a> }));

import { EconomyAdRewardsWorkspace } from './economy-ad-rewards-workspace';

function prepareForm() {
  fireEvent.change(screen.getByLabelText('adRewards.creative'), { target: { value: '/network/rewarded' } });
  fireEvent.change(screen.getByLabelText('adRewards.duration'), { target: { value: '90' } });
  fireEvent.click(screen.getByLabelText('adRewards.consent'));
}

describe('EconomyAdRewardsWorkspace interaction', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.callbacks = null;
    mocks.start.mockResolvedValue({ success: true, message: 'started', data: { sessionId: 'session', signedToken: 'signed' } });
    mocks.complete.mockResolvedValue({ success: true, message: 'deferred claim recorded' });
    mocks.request.mockImplementation(async (_request: unknown, callbacks: Record<string, (...args: never[]) => unknown>) => {
      mocks.callbacks = callbacks;
      return mocks.cleanup;
    });
  });

  it('records the full granted/completed lifecycle as a deferred backend claim', async () => {
    render(<EconomyAdRewardsWorkspace />);
    expect(screen.getByRole('button', { name: 'adRewards.start' })).toBeDisabled();
    prepareForm();
    fireEvent.submit(screen.getByRole('button', { name: 'adRewards.start' }).closest('form')!);

    await waitFor(() => expect(mocks.request).toHaveBeenCalledWith(
      { adUnitPath: '/network/rewarded', consentGranted: true }, expect.any(Object),
    ));
    expect(await screen.findByRole('link', { name: /common.open · session/ })).toHaveAttribute('href', '/workspace/economy/ad-rewards/session');
    act(() => { mocks.callbacks?.onReady?.((() => true) as never); });
    act(() => { mocks.callbacks?.onReady?.((() => false) as never); });
    expect(await screen.findByText('adRewards.couldNotShow')).toBeInTheDocument();
    act(() => {
      mocks.callbacks?.onGranted?.();
      mocks.callbacks?.onVideoCompleted?.();
      mocks.callbacks?.onClosed?.();
    });
    await waitFor(() => expect(mocks.complete).toHaveBeenCalledWith(expect.objectContaining({
      sessionId: 'session', signedToken: 'signed', creativeId: '/network/rewarded',
      playbackDuration: '00:01:30', visibleDuration: '00:01:30',
    }), expect.any(String)));
    await screen.findByText('deferred claim recorded');
  });

  it('rejects early close, forwards adapter errors, and releases a live slot on unmount', async () => {
    const view = render(<EconomyAdRewardsWorkspace />);
    prepareForm();
    fireEvent.submit(screen.getByRole('button', { name: 'adRewards.start' }).closest('form')!);
    await waitFor(() => expect(mocks.callbacks).not.toBeNull());
    act(() => { mocks.callbacks?.onError?.('provider unavailable' as never); });
    expect(await screen.findByText('provider unavailable')).toBeInTheDocument();
    act(() => { mocks.callbacks?.onClosed?.(); });
    expect(await screen.findByText('adRewards.closedEarly')).toBeInTheDocument();
    expect(mocks.complete).not.toHaveBeenCalled();
    view.unmount();

    const second = render(<EconomyAdRewardsWorkspace />);
    prepareForm();
    fireEvent.submit(screen.getByRole('button', { name: 'adRewards.start' }).closest('form')!);
    await waitFor(() => expect(mocks.request).toHaveBeenCalledTimes(2));
    second.unmount();
    expect(mocks.cleanup).toHaveBeenCalled();
  });

  it('stops after rejected or incomplete session creation', async () => {
    mocks.start.mockResolvedValueOnce({ success: false, message: 'session blocked' });
    const first = render(<EconomyAdRewardsWorkspace />);
    prepareForm();
    fireEvent.submit(screen.getByRole('button', { name: 'adRewards.start' }).closest('form')!);
    await screen.findByText('session blocked');
    expect(mocks.request).not.toHaveBeenCalled();
    first.unmount();

    mocks.start.mockResolvedValueOnce({ success: true, message: 'incomplete', data: {} });
    render(<EconomyAdRewardsWorkspace />);
    prepareForm();
    fireEvent.submit(screen.getByRole('button', { name: 'adRewards.start' }).closest('form')!);
    await screen.findByText('incomplete');
    expect(mocks.request).not.toHaveBeenCalled();
  });

  it('normalizes adapter failures from Error and unknown values', async () => {
    mocks.request.mockRejectedValueOnce(new Error('adapter failed'));
    const first = render(<EconomyAdRewardsWorkspace />);
    prepareForm();
    fireEvent.submit(screen.getByRole('button', { name: 'adRewards.start' }).closest('form')!);
    await screen.findByText('adapter failed');
    first.unmount();

    mocks.request.mockRejectedValueOnce('offline');
    render(<EconomyAdRewardsWorkspace />);
    prepareForm();
    fireEvent.submit(screen.getByRole('button', { name: 'adRewards.start' }).closest('form')!);
    await screen.findByText('adRewards.unavailable');
  });
});
