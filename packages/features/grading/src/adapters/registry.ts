import type { GradingAdapter } from './types';

const adapters = new Map<string, GradingAdapter>();

export function registerGradingAdapter(adapter: GradingAdapter): void {
  adapters.set(adapter.contentType, adapter);
}

export function getGradingAdapter(contentType: string): GradingAdapter | null {
  return adapters.get(contentType) ?? null;
}

export function clearGradingAdaptersForTests(): void {
  adapters.clear();
}
