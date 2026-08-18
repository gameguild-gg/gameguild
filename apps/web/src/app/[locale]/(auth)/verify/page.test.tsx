import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

vi.mock('next-intl/server', () => ({
  getTranslations: async (namespace: string) => (key: string) => `${namespace}.${key}`,
}));
vi.mock('@/components/auth/verify-page-content', () => ({
  VerifyPageContent: () => <div data-testid="verify-content" />,
}));
vi.mock('@/components/auth/auth-error-notice', () => ({
  AuthErrorNotice: ({ errorCode }: { errorCode?: string }) =>
    errorCode ? <div data-testid="auth-error-notice">{errorCode}</div> : null,
}));

import VerifyPage from './page';

async function renderPage(query: Record<string, string | undefined>) {
  const page = await VerifyPage({
    params: Promise.resolve({ locale: 'en-US' }),
    searchParams: Promise.resolve(query),
  } as never);
  return render(page);
}

describe('verify page inline auth errors', () => {
  afterEach(cleanup);

  it('renders the inline error notice when ?error= is present', async () => {
    await renderPage({ error: 'verification' });

    expect(screen.getByTestId('auth-error-notice')).toHaveTextContent('verification');
    expect(screen.getByTestId('verify-content')).toBeInTheDocument();
  });

  it('renders no notice without an error', async () => {
    await renderPage({});

    expect(screen.queryByTestId('auth-error-notice')).not.toBeInTheDocument();
    expect(screen.getByTestId('verify-content')).toBeInTheDocument();
  });
});
