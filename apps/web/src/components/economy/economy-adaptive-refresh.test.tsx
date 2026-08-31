import { act, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ pathname: '/workspace/economy/wallet' as string | null, refresh: vi.fn() }));
vi.mock('next/navigation', () => ({
  usePathname: () => mocks.pathname,
  useRouter: () => ({ refresh: mocks.refresh }),
}));
vi.mock('next-intl', () => ({ useTranslations: () => (key: string) => key }));

import { EconomyAdaptiveRefresh, getEconomyPollingInterval } from './economy-adaptive-refresh';

describe('EconomyAdaptiveRefresh', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.clearAllMocks();
    mocks.pathname = '/workspace/economy/wallet';
    Object.defineProperty(navigator, 'onLine', { configurable: true, value: true });
    Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'visible' });
  });

  afterEach(() => vi.useRealTimers());

  it('uses 15 seconds for operational queues and 30 seconds for histories/configuration', () => {
    expect(getEconomyPollingInterval('/console/economy/ledger')).toBe(15_000);
    expect(getEconomyPollingInterval('/workspace/economy/payouts/one')).toBe(15_000);
    expect(getEconomyPollingInterval('/workspace/economy/wallet')).toBe(30_000);
  });

  it('refreshes automatically and always exposes manual refresh', () => {
    render(<EconomyAdaptiveRefresh><p>content</p></EconomyAdaptiveRefresh>);
    act(() => vi.advanceTimersByTime(30_000));
    expect(mocks.refresh).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole('button', { name: /manual/ }));
    expect(mocks.refresh).toHaveBeenCalledTimes(2);
  });

  it('pauses for edited forms and resumes after submit', () => {
    render(<EconomyAdaptiveRefresh><form><input aria-label="field" /><button type="submit">save</button></form></EconomyAdaptiveRefresh>);
    fireEvent.input(screen.getByLabelText('field'), { target: { value: 'edited' } });
    expect(screen.getByText('paused')).toBeInTheDocument();
    act(() => vi.advanceTimersByTime(60_000));
    expect(mocks.refresh).not.toHaveBeenCalled();
    fireEvent.submit(screen.getByRole('button', { name: 'save' }).closest('form')!);
    act(() => vi.advanceTimersByTime(30_000));
    expect(mocks.refresh).toHaveBeenCalledTimes(1);
  });

  it('resumes a dirty form after reset and supports an absent pathname', () => {
    mocks.pathname = null;
    render(<EconomyAdaptiveRefresh><form><input aria-label="field" /><button type="reset">reset</button></form></EconomyAdaptiveRefresh>);
    fireEvent.change(screen.getByLabelText('field'), { target: { value: 'edited' } });
    expect(screen.getByText('paused')).toBeInTheDocument();
    fireEvent.reset(screen.getByRole('button', { name: 'reset' }).closest('form')!);
    expect(screen.getByText('active')).toBeInTheDocument();
  });

  it('resets dirty state after navigation', () => {
    const { rerender } = render(<EconomyAdaptiveRefresh><input aria-label="field" /></EconomyAdaptiveRefresh>);
    fireEvent.input(screen.getByLabelText('field'), { target: { value: 'edited' } });
    expect(screen.getByText('paused')).toBeInTheDocument();

    mocks.pathname = '/workspace/economy/orders';
    rerender(<EconomyAdaptiveRefresh><input aria-label="field" /></EconomyAdaptiveRefresh>);

    expect(screen.getByText('active')).toBeInTheDocument();
  });

  it('pauses in a hidden tab and backs off while offline', () => {
    const { rerender } = render(<EconomyAdaptiveRefresh><p>content</p></EconomyAdaptiveRefresh>);
    Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'hidden' });
    fireEvent(document, new Event('visibilitychange'));
    act(() => vi.advanceTimersByTime(60_000));
    expect(mocks.refresh).not.toHaveBeenCalled();

    Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'visible' });
    Object.defineProperty(navigator, 'onLine', { configurable: true, value: false });
    fireEvent(document, new Event('visibilitychange'));
    fireEvent(window, new Event('offline'));
    rerender(<EconomyAdaptiveRefresh><p>content</p></EconomyAdaptiveRefresh>);
    expect(screen.getByText('offline')).toBeInTheDocument();
    act(() => vi.advanceTimersByTime(120_000));
    expect(mocks.refresh).not.toHaveBeenCalled();
  });
});
