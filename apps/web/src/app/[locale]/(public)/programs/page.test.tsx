import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  redirect: vi.fn((args: unknown) => {
    throw new Error(`redirect:${JSON.stringify(args)}`);
  }),
}));

vi.mock('@/i18n/navigation', () => ({ redirect: mocks.redirect }));

import ProgramsRedirectPage from './page';

describe('legacy /programs redirect', () => {
  it('forwards to the unified catalog with ?type=program', async () => {
    await expect(
      ProgramsRedirectPage({ params: Promise.resolve({ locale: 'pt-BR' }) } as never),
    ).rejects.toThrow(
      'redirect:{"href":"/courses","locale":"pt-BR"}',
    );
  });
});
