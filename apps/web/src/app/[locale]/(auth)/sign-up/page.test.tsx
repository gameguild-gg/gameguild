import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
}));

vi.mock('next-intl/server', () => ({
  getTranslations: async (namespace: string) => (key: string) => `${namespace}.${key}`,
}));
vi.mock('@/auth', () => ({ auth: mocks.auth }));
vi.mock('@/lib/auth/cross-domain-auth', () => ({
  resolveAllowedAuthRedirect: (value: string | undefined) => value ?? '/',
}));
vi.mock('@/components/signup-form', () => ({
  SignupForm: () => <div data-testid="signup-form" />,
}));
vi.mock('@/components/google-one-tap', () => ({
  GoogleOneTap: () => <div data-testid="google-one-tap" />,
}));
vi.mock('@/components/google-sign-in-button', () => ({
  GoogleSignInButton: () => <div data-testid="google-sign-in-button" />,
}));
vi.mock('@/components/auth/auth-error-notice', () => ({
  AuthErrorNotice: ({ errorCode }: { errorCode?: string }) =>
    errorCode ? <div data-testid="auth-error-notice">{errorCode}</div> : null,
}));

import SignUpPage from './page';

async function renderPage(query: Record<string, string | undefined>) {
  const page = await SignUpPage({
    searchParams: Promise.resolve(query),
  } as never);
  return render(page);
}

describe('sign-up page inline auth errors', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue(null);
  });

  afterEach(cleanup);

  it('renders the inline error notice when ?error= is present', async () => {
    await renderPage({ error: 'state_mismatch' });

    expect(screen.getByTestId('auth-error-notice')).toHaveTextContent('state_mismatch');
    expect(screen.getByTestId('signup-form')).toBeInTheDocument();
  });

  it('renders no notice without an error', async () => {
    await renderPage({});

    expect(screen.queryByTestId('auth-error-notice')).not.toBeInTheDocument();
    expect(screen.getByTestId('signup-form')).toBeInTheDocument();
  });
});
