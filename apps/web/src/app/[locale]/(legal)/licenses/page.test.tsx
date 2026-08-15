import { describe, expect, it, vi } from 'vitest';

vi.mock('next/navigation', () => ({
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
}));

import Page from './page';

describe('legacy /licenses redirect', () => {
  it('forwards to /legal/licenses', async () => {
    await expect(
      Page({ params: Promise.resolve({ locale: 'en-US' }) } as never),
    ).rejects.toThrow('redirect:/en-US/legal/licenses');
  });
});
