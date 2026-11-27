import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithUser } from '@/test/auth-test-helpers';

vi.mock('next-intl', () => ({
  useTranslations:
    (namespace: string) =>
    (key: string, params?: Record<string, unknown>) =>
      params ? `${namespace}.${key}:${JSON.stringify(params)}` : `${namespace}.${key}`,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({
    children,
    href,
    ...rest
  }: {
    children: React.ReactNode;
    href: string;
  }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

const { EmailVerificationBanner, EMAIL_VERIFICATION_BANNER_STORAGE_KEY } = await import(
  '@/components/auth/email-verification-banner'
);

const EMAIL = 'member@example.com';

/** Lets React switch from the server snapshot to the browser's dismissal flag. */
async function settle() {
  await new Promise((resolve) => {
    setTimeout(resolve, 0);
  });
}

describe('EmailVerificationBanner (#1298 J1)', () => {
  beforeEach(() => {
    window.sessionStorage.clear();
    window.localStorage.clear();
  });

  afterEach(cleanup);

  it('reminds the signed-in user and links to the verification page for their address', async () => {
    render(<EmailVerificationBanner email={EMAIL} />);

    const banner = await screen.findByTestId('email-verification-banner');
    expect(banner).toHaveTextContent(`emailVerification.banner.title:{"email":"${EMAIL}"}`);
    expect(screen.getByTestId('email-verification-banner-action')).toHaveAttribute(
      'href',
      `/verify?email=${encodeURIComponent(EMAIL)}`,
    );
    expect(screen.getByTestId('email-verification-banner-action')).toHaveTextContent(
      'emailVerification.banner.action',
    );
  });

  it('stays hidden once it was dismissed earlier in the browser session', async () => {
    window.sessionStorage.setItem(EMAIL_VERIFICATION_BANNER_STORAGE_KEY, '1');
    render(<EmailVerificationBanner email={EMAIL} />);

    await settle();

    expect(screen.queryByTestId('email-verification-banner')).not.toBeInTheDocument();
  });

  it('hides for the rest of the browser session when dismissed', async () => {
    const { user } = renderWithUser(<EmailVerificationBanner email={EMAIL} />);
    await screen.findByTestId('email-verification-banner');

    await user.click(screen.getByTestId('email-verification-banner-dismiss'));

    expect(window.sessionStorage.getItem(EMAIL_VERIFICATION_BANNER_STORAGE_KEY)).toBe('1');
    expect(window.localStorage.length).toBe(0);
    expect(screen.queryByTestId('email-verification-banner')).not.toBeInTheDocument();
  });

  it('shows again in a new browser session after a dismissal', async () => {
    const { user } = renderWithUser(<EmailVerificationBanner email={EMAIL} />);
    await screen.findByTestId('email-verification-banner');
    await user.click(screen.getByTestId('email-verification-banner-dismiss'));
    // A new browser session starts with an empty session store.
    window.sessionStorage.clear();
    cleanup();

    render(<EmailVerificationBanner email={EMAIL} />);

    expect(await screen.findByTestId('email-verification-banner')).toBeInTheDocument();
  });
});
