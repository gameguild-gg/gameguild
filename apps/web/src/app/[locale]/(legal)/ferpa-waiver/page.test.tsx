import { describe, expect, it, vi } from 'vitest';

vi.mock('next/navigation', () => ({
  usePathname: () => '/workspace/learning',
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
}));

import Page from './page';

describe('legacy /ferpa-waiver redirect', () => {
  it('forwards to /legal/ferpa-waiver', async () => {
    await expect(
      Page({ params: Promise.resolve({ locale: 'en-US' }) } as never),
    ).rejects.toThrow('redirect:/en-US/legal/ferpa-waiver');
  });
});
