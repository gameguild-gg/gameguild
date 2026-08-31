import type { RewardedAdAdapter, RewardedAdCallbacks, RewardedAdRequest } from './rewarded-ad-adapter';

interface GoogleSlot {
  addService(service: unknown): GoogleSlot;
}

interface GoogleEvent {
  makeRewardedVisible?: () => boolean;
  slot: GoogleSlot;
}

interface GooglePubAds {
  addEventListener(name: string, listener: (event: GoogleEvent) => void): void;
  removeEventListener(name: string, listener: (event: GoogleEvent) => void): void;
}

interface GoogleTag {
  cmd: Array<() => void>;
  defineOutOfPageSlot(path: string, format: unknown): GoogleSlot | null;
  destroySlots(slots: GoogleSlot[]): boolean;
  display(slot: GoogleSlot): void;
  enableServices(): void;
  enums: { OutOfPageFormat: { REWARDED: unknown } };
  pubads(): GooglePubAds;
}

let activeCleanup: (() => void) | null = null;

function loadGooglePublisherTag(): Promise<GoogleTag> {
  const browserWindow = window as typeof window & { googletag?: GoogleTag };
  if (browserWindow.googletag?.defineOutOfPageSlot) return Promise.resolve(browserWindow.googletag);
  browserWindow.googletag = browserWindow.googletag ?? ({ cmd: [] } as unknown as GoogleTag);
  const existing = document.querySelector<HTMLScriptElement>('script[data-gameguild-gpt="true"]');
  return new Promise((resolve, reject) => {
    const script = existing ?? document.createElement('script');
    const loaded = () => resolve(browserWindow.googletag!);
    const failed = () => reject(new Error('Google Publisher Tag failed to load.'));
    script.addEventListener('load', loaded, { once: true });
    script.addEventListener('error', failed, { once: true });
    if (!existing) {
      script.async = true;
      script.dataset.gameguildGpt = 'true';
      script.src = 'https://securepubads.g.doubleclick.net/tag/js/gpt.js';
      document.head.appendChild(script);
    }
  });
}

export class GoogleAdManagerWebRewardedAdapter implements RewardedAdAdapter {
  async request(request: RewardedAdRequest, callbacks: RewardedAdCallbacks): Promise<() => void> {
    if (!request.consentGranted) throw new Error('Explicit ad consent is required.');
    if (!request.adUnitPath.trim()) throw new Error('A Google Ad Manager ad unit is required.');
    if (activeCleanup) throw new Error('Only one rewarded slot may be active.');
    const google = await loadGooglePublisherTag();

    return await new Promise<() => void>((resolve, reject) => {
      google.cmd.push(() => {
        const slot = google.defineOutOfPageSlot(request.adUnitPath, google.enums.OutOfPageFormat.REWARDED);
        if (!slot) {
          callbacks.onError('Rewarded ads are unsupported on this device.');
          reject(new Error('Rewarded ads are unsupported on this device.'));
          return;
        }
        slot.addService(google.pubads());
        const listeners: Array<[string, (event: GoogleEvent) => void]> = [];
        const listen = (name: string, listener: (event: GoogleEvent) => void) => {
          listeners.push([name, listener]);
          google.pubads().addEventListener(name, listener);
        };
        const cleanup = () => {
          for (const [name, listener] of listeners) google.pubads().removeEventListener(name, listener);
          google.destroySlots([slot]);
          if (activeCleanup === cleanup) activeCleanup = null;
        };
        activeCleanup = cleanup;
        listen('rewardedSlotReady', (event) => {
          if (event.slot === slot && event.makeRewardedVisible) callbacks.onReady(event.makeRewardedVisible);
        });
        listen('rewardedSlotGranted', (event) => {
          if (event.slot === slot) callbacks.onGranted();
        });
        listen('rewardedSlotVideoCompleted', (event) => {
          if (event.slot === slot) callbacks.onVideoCompleted();
        });
        listen('rewardedSlotClosed', (event) => {
          if (event.slot !== slot) return;
          callbacks.onClosed();
          cleanup();
        });
        google.enableServices();
        google.display(slot);
        resolve(cleanup);
      });
    });
  }
}
