import { describe, expect, it, vi } from 'vitest';

vi.mock('next/navigation', () => ({
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
}));

import Page from './page';

describe('legacy /terms-of-service redirect', () => {
  it('forwards to /terms-of-service', async () => {
    await expect(
      Page({ params: Promise.resolve({ locale: 'en-US' }) } as never),
    ).rejects.toThrow('redirect:/en-US/terms-of-service');
  });
});
