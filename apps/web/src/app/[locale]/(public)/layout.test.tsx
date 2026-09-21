import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';

vi.mock('@/components/app/app-shell', () => ({
  PublicWebsiteShell: async ({ children }: { children: ReactNode }) => (
    <div data-testid="app-shell">{children}</div>
  ),
}));

import PublicLayout from './layout';

describe('public layout', () => {
  afterEach(cleanup);

  it('wraps public pages in the PublicWebsiteShell', async () => {
    const ui = await PublicLayout({
      children: <div data-testid="public-content">catalog</div>,
    } as never);
    render(ui);

    expect(screen.getByTestId('app-shell')).toBeInTheDocument();
    expect(screen.getByTestId('public-content')).toBeInTheDocument();
  });
});
