import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

vi.mock('next-intl/server', () => ({
  getTranslations: async (namespace: string) => (key: string) => `${namespace}.${key}`,
}));
vi.mock('@/components/forgot-password-form', () => ({
  ForgotPasswordForm: () => <div data-testid="forgot-password-form" />,
}));
vi.mock('../actions', () => ({
  requestPasswordResetAction: vi.fn(),
}));
vi.mock('@/components/auth/auth-error-notice', () => ({
  AuthErrorNotice: ({ errorCode }: { errorCode?: string }) =>
    errorCode ? <div data-testid="auth-error-notice">{errorCode}</div> : null,
}));

import ForgotPasswordPage from './page';

async function renderPage(query: Record<string, string | undefined>) {
  const page = await ForgotPasswordPage({ searchParams: Promise.resolve(query) });
  return render(page);
}

describe('forgot-password page inline auth errors', () => {
  afterEach(cleanup);

  it('renders the inline error notice when ?error= is present', async () => {
    await renderPage({ error: 'missing_code', email: 'a@b.c' });

    expect(screen.getByTestId('auth-error-notice')).toHaveTextContent('missing_code');
    expect(screen.getByTestId('forgot-password-form')).toBeInTheDocument();
  });

  it('renders no notice without an error', async () => {
    await renderPage({ email: 'a@b.c' });

    expect(screen.queryByTestId('auth-error-notice')).not.toBeInTheDocument();
    expect(screen.getByTestId('forgot-password-form')).toBeInTheDocument();
  });
});
