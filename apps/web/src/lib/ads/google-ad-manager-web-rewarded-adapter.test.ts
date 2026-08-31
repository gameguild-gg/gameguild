import { afterEach, describe, expect, it, vi } from 'vitest';
import { GoogleAdManagerWebRewardedAdapter } from './google-ad-manager-web-rewarded-adapter';

interface RewardedEvent {
  makeRewardedVisible?: () => boolean;
  slot: object;
}

function installGoogleTag() {
  const listeners = new Map<string, (event: RewardedEvent) => void>();
  const slot = { addService: vi.fn(() => slot) };
  const pubads = {
    addEventListener: vi.fn((name: string, listener: (event: RewardedEvent) => void) => listeners.set(name, listener)),
    removeEventListener: vi.fn((name: string) => listeners.delete(name)),
  };
  const googleTag = {
    cmd: { push: (command: () => void) => { command(); return 1; } },
    defineOutOfPageSlot: vi.fn(() => slot),
    destroySlots: vi.fn(() => true),
    display: vi.fn(),
    enableServices: vi.fn(),
    enums: { OutOfPageFormat: { REWARDED: 'rewarded' } },
    pubads: vi.fn(() => pubads),
  };
  Object.assign(window, { googletag: googleTag });
  return { googleTag, listeners, slot };
}

afterEach(() => {
  Reflect.deleteProperty(window, 'googletag');
  document.head.querySelectorAll('script[data-gameguild-gpt="true"]').forEach((script) => script.remove());
});

describe('GoogleAdManagerWebRewardedAdapter', () => {
  it('does not load Google Publisher Tag before explicit consent', async () => {
    const adapter = new GoogleAdManagerWebRewardedAdapter();

    await expect(adapter.request({ adUnitPath: '/rewarded', consentGranted: false }, {
      onReady: vi.fn(), onGranted: vi.fn(), onVideoCompleted: vi.fn(), onClosed: vi.fn(), onError: vi.fn(),
    })).rejects.toThrow('Explicit ad consent');

    expect(document.querySelector('script[data-gameguild-gpt="true"]')).toBeNull();
    await expect(adapter.request({ adUnitPath: ' ', consentGranted: true }, {
      onReady: vi.fn(), onGranted: vi.fn(), onVideoCompleted: vi.fn(), onClosed: vi.fn(), onError: vi.fn(),
    })).rejects.toThrow('ad unit is required');
  });

  it('allows one slot, forwards verified lifecycle events, and cleans up', async () => {
    const { googleTag, listeners, slot } = installGoogleTag();
    const callbacks = {
      onReady: vi.fn(), onGranted: vi.fn(), onVideoCompleted: vi.fn(), onClosed: vi.fn(), onError: vi.fn(),
    };
    const adapter = new GoogleAdManagerWebRewardedAdapter();
    const cleanup = await adapter.request({ adUnitPath: '/rewarded', consentGranted: true }, callbacks);

    await expect(adapter.request({ adUnitPath: '/second', consentGranted: true }, callbacks))
      .rejects.toThrow('Only one rewarded slot');
    const show = vi.fn(() => true);
    const unrelated = {};
    listeners.get('rewardedSlotReady')?.({ slot: unrelated });
    listeners.get('rewardedSlotGranted')?.({ slot: unrelated });
    listeners.get('rewardedSlotVideoCompleted')?.({ slot: unrelated });
    listeners.get('rewardedSlotClosed')?.({ slot: unrelated });
    listeners.get('rewardedSlotReady')?.({ slot, makeRewardedVisible: show });
    listeners.get('rewardedSlotGranted')?.({ slot });
    listeners.get('rewardedSlotVideoCompleted')?.({ slot });
    listeners.get('rewardedSlotClosed')?.({ slot });

    expect(callbacks.onReady).toHaveBeenCalledWith(show);
    expect(callbacks.onGranted).toHaveBeenCalledOnce();
    expect(callbacks.onVideoCompleted).toHaveBeenCalledOnce();
    expect(callbacks.onClosed).toHaveBeenCalledOnce();
    expect(googleTag.destroySlots).toHaveBeenCalledWith([slot]);
    cleanup();
    const nextCleanup = await adapter.request({ adUnitPath: '/next', consentGranted: true }, callbacks);
    expect(nextCleanup).toBeTypeOf('function');
    nextCleanup();
  });

  it('reports unsupported rewarded inventory without activating a slot', async () => {
    const { googleTag } = installGoogleTag();
    googleTag.defineOutOfPageSlot.mockReturnValueOnce(null as never);
    const onError = vi.fn();

    await expect(new GoogleAdManagerWebRewardedAdapter().request(
      { adUnitPath: '/unsupported', consentGranted: true },
      { onReady: vi.fn(), onGranted: vi.fn(), onVideoCompleted: vi.fn(), onClosed: vi.fn(), onError },
    )).rejects.toThrow('unsupported');

    expect(onError).toHaveBeenCalledWith('Rewarded ads are unsupported on this device.');
  });

  it('loads a new GPT script and reuses an existing pending script', async () => {
    const callbacks = { onReady: vi.fn(), onGranted: vi.fn(), onVideoCompleted: vi.fn(), onClosed: vi.fn(), onError: vi.fn() };
    const pending = new GoogleAdManagerWebRewardedAdapter().request({ adUnitPath: '/loaded', consentGranted: true }, callbacks);
    const script = document.querySelector<HTMLScriptElement>('script[data-gameguild-gpt="true"]')!;
    expect(script.src).toContain('securepubads.g.doubleclick.net');
    installGoogleTag();
    script.dispatchEvent(new Event('load'));
    const cleanup = await pending;
    cleanup();

    Reflect.deleteProperty(window, 'googletag');
    script.remove();
    const existing = document.createElement('script');
    existing.dataset.gameguildGpt = 'true';
    document.head.appendChild(existing);
    const reused = new GoogleAdManagerWebRewardedAdapter().request({ adUnitPath: '/reused', consentGranted: true }, callbacks);
    installGoogleTag();
    existing.dispatchEvent(new Event('load'));
    (await reused)();
  });

  it('surfaces GPT script loading failures', async () => {
    const pending = new GoogleAdManagerWebRewardedAdapter().request(
      { adUnitPath: '/failed', consentGranted: true },
      { onReady: vi.fn(), onGranted: vi.fn(), onVideoCompleted: vi.fn(), onClosed: vi.fn(), onError: vi.fn() },
    );
    document.querySelector<HTMLScriptElement>('script[data-gameguild-gpt="true"]')?.dispatchEvent(new Event('error'));
    await expect(pending).rejects.toThrow('failed to load');
  });
});
