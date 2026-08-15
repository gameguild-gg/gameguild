import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  redirect: vi.fn((args: unknown) => {
    throw new Error(`redirect:${JSON.stringify(args)}`);
  }),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth }));
vi.mock('@/i18n/navigation', () => ({
  redirect: mocks.redirect,
  Link: ({ href, children }: { href: string; children: ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));
vi.mock('@/components/app/app-shell', () => ({
  AppShell: async ({ children }: { children: ReactNode }) => (
    <div data-testid="public-shell">{children}</div>
  ),
}));

import PrivateLayout from './layout';

const layoutProps = {
  children: <div data-testid="private-content">member content</div>,
  params: Promise.resolve({ locale: 'en-US' }),
};

describe('private layout auth gate', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('redirects anonymous users to sign-in', async () => {
    mocks.auth.mockResolvedValue(null);

    await expect(PrivateLayout(layoutProps as never)).rejects.toThrow(
      'redirect:{"href":"/sign-in","locale":"en-US"}',
    );
  });

  it('redirects when auth returns an invalid session shape', async () => {
    mocks.auth.mockResolvedValue(() => undefined);

    await expect(PrivateLayout(layoutProps as never)).rejects.toThrow('redirect:');
  });

  it('renders children inside the public shell for authenticated users', async () => {
    mocks.auth.mockResolvedValue({ user: { id: 'user-1', name: 'Ada' } });

    const ui = await PrivateLayout(layoutProps as never);
    render(ui);

    expect(screen.getByTestId('public-shell')).toBeInTheDocument();
    expect(screen.getByTestId('private-content')).toBeInTheDocument();
  });
});
