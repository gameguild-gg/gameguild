import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

vi.mock('next-intl/server', () => ({
  getTranslations: async (namespace: string) => (key: string) => `${namespace}.${key}`,
}));
vi.mock('@/components/reset-password-form', () => ({
  ResetPasswordForm: () => <div data-testid="reset-password-form" />,
}));
vi.mock('../actions', () => ({
  completePasswordResetAction: vi.fn(),
}));
vi.mock('@/components/auth/auth-error-notice', () => ({
  AuthErrorNotice: ({ errorCode }: { errorCode?: string }) =>
    errorCode ? <div data-testid="auth-error-notice">{errorCode}</div> : null,
}));

import ResetPasswordPage from './page';

async function renderPage(query: Record<string, string | undefined>) {
  const page = await ResetPasswordPage({ searchParams: Promise.resolve(query) });
  return render(page);
}

describe('reset-password page inline auth errors', () => {
  afterEach(cleanup);

  it('renders the inline error notice when ?error= is present', async () => {
    await renderPage({ error: 'callback_failed', token: 'tok' });

    expect(screen.getByTestId('auth-error-notice')).toHaveTextContent('callback_failed');
    expect(screen.getByTestId('reset-password-form')).toBeInTheDocument();
  });

  it('renders no notice without an error', async () => {
    await renderPage({ token: 'tok' });

    expect(screen.queryByTestId('auth-error-notice')).not.toBeInTheDocument();
    expect(screen.getByTestId('reset-password-form')).toBeInTheDocument();
  });
});
