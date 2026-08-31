export interface RewardedAdCallbacks {
  onClosed(): void;
  onError(message: string): void;
  onGranted(): void;
  onReady(show: () => boolean): void;
  onVideoCompleted(): void;
}

export interface RewardedAdRequest {
  adUnitPath: string;
  consentGranted: boolean;
}

export interface RewardedAdAdapter {
  request(request: RewardedAdRequest, callbacks: RewardedAdCallbacks): Promise<() => void>;
}
