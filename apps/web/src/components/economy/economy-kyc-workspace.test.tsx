import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  onboarding: vi.fn(),
  token: vi.fn(),
  refresh: vi.fn(),
  locale: 'pt-BR',
  sdkProps: null as Record<string, unknown> | null,
}));

vi.mock('@/lib/economy/actions', () => ({
  startKycOnboardingAction: mocks.onboarding,
  createKycAccessTokenAction: mocks.token,
}));
vi.mock('next/navigation', () => ({ useRouter: () => ({ refresh: mocks.refresh }) }));
vi.mock('next-intl', () => ({
  useLocale: () => mocks.locale,
  useTranslations: () => (key: string) => key,
}));
vi.mock('@sumsub/websdk-react', () => ({
  default: (props: Record<string, unknown>) => {
    mocks.sdkProps = props;
    return <div data-testid="sumsub-sdk" />;
  },
}));

import { EconomyKycWorkspace } from './economy-kyc-workspace';

describe('EconomyKycWorkspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.sdkProps = null;
    mocks.locale = 'pt-BR';
    mocks.onboarding.mockResolvedValue({ success: true, message: 'started' });
    mocks.token.mockResolvedValue({ success: true, message: 'token', data: { token: 'short-lived-token' } });
  });

  it('starts server-owned onboarding and configures the official SDK for the locale', async () => {
    render(<EconomyKycWorkspace data={{ issue: null, status: null }} />);

    fireEvent.click(screen.getByRole('button', { name: 'kyc.start' }));

    await screen.findByTestId('sumsub-sdk');
    expect(mocks.onboarding).toHaveBeenCalledOnce();
    expect(mocks.sdkProps).toMatchObject({
      accessToken: 'short-lived-token',
      config: { lang: 'pt' },
      options: { addViewportTag: true, adaptIframeHeight: true },
    });
    expect(mocks.refresh).toHaveBeenCalled();
  });

  it('renews an expired token and refreshes only normalized status events', async () => {
    mocks.token
      .mockResolvedValueOnce({ success: true, message: 'token', data: { token: 'initial' } })
      .mockResolvedValueOnce({ success: true, message: 'token', data: { token: 'renewed' } });
    render(<EconomyKycWorkspace data={{ issue: null, status: null }} />);
    fireEvent.click(screen.getByRole('button', { name: 'kyc.start' }));
    await screen.findByTestId('sumsub-sdk');

    const expirationHandler = mocks.sdkProps?.expirationHandler as () => Promise<string>;
    let renewed = '';
    await act(async () => { renewed = await expirationHandler(); });
    expect(renewed).toBe('renewed');
    await waitFor(() => expect(mocks.sdkProps?.accessToken).toBe('renewed'));
    const onMessage = mocks.sdkProps?.onMessage as (type: string, payload: unknown) => void;
    mocks.refresh.mockClear();
    act(() => onMessage('untrusted.payload', { document: 'must-not-be-logged' }));
    expect(mocks.refresh).not.toHaveBeenCalled();
    act(() => onMessage('idCheck.onApplicantStatusChanged', {}));
    expect(mocks.refresh).toHaveBeenCalledOnce();
    act(() => (mocks.sdkProps?.onError as () => void)());
    await waitFor(() => expect(screen.getAllByText('kyc.unavailable').length).toBeGreaterThan(1));
  });

  it('fails closed for onboarding, token, and renewal failures while rendering current evidence', async () => {
    mocks.locale = 'en-US';
    mocks.onboarding.mockResolvedValueOnce({ success: false, message: 'onboarding blocked' });
    const first = render(<EconomyKycWorkspace data={{ issue: 'feed issue', status: { result: 'Approved', isCurrent: true, hasEvidence: true, expiresAt: null, version: 2 } as never }} />);
    expect(screen.getByText('kyc.current')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'kyc.resume' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'kyc.resume' }));
    await screen.findByText('onboarding blocked');
    first.unmount();

    mocks.token.mockResolvedValueOnce({ success: false, message: 'token blocked' });
    const second = render(<EconomyKycWorkspace data={{ issue: null, status: null }} />);
    fireEvent.click(screen.getByRole('button', { name: 'kyc.start' }));
    await screen.findByText('token blocked');
    expect(mocks.refresh).not.toHaveBeenCalled();
    second.unmount();

    mocks.token
      .mockResolvedValueOnce({ success: true, message: 'token', data: { token: 'initial' } })
      .mockResolvedValueOnce({ success: false, message: 'expired' });
    render(<EconomyKycWorkspace data={{ issue: null, status: null }} />);
    fireEvent.click(screen.getByRole('button', { name: 'kyc.start' }));
    await screen.findByTestId('sumsub-sdk');
    expect(mocks.sdkProps).toMatchObject({ config: { lang: 'en' } });
    await expect((mocks.sdkProps?.expirationHandler as () => Promise<string>)()).rejects.toThrow('kyc.renewalFailed');
  });
});
