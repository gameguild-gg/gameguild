import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/economy/actions', () => ({ createKycAccessTokenAction: vi.fn(), startKycOnboardingAction: vi.fn() }));
vi.mock('next/navigation', () => ({ useRouter: () => ({ refresh: vi.fn() }) }));
vi.mock('next-intl', () => ({ useLocale: () => 'en-US', useTranslations: () => (key: string) => key }));
vi.mock('@sumsub/websdk-react', () => { throw new Error('SDK unavailable'); });

describe('EconomyKycWorkspace SDK loading', () => {
  it('shows a safe fail-closed state when the vendor SDK cannot load', async () => {
    const { EconomyKycWorkspace } = await import('./economy-kyc-workspace');
    render(<EconomyKycWorkspace data={{ issue: null, status: null }} />);
    expect(await screen.findByText('kyc.unavailable')).toBeInTheDocument();
  });
});
