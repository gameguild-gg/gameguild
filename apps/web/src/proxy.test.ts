import { describe, expect, it, vi } from 'vitest';

vi.mock('@/i18n', () => ({
  routing: {
    defaultLocale: 'en-US',
    locales: ['en-US', 'pt-BR'],
  },
}));

vi.mock('@/lib/tracks/catalog', () => ({
  getTrackProgramHref: (slug: string) => `/programs/${slug}`,
}));

const { config } = await import('./proxy');

describe('GameGuild proxy matcher', () => {
  it('runs for dashboard RSC prefetch requests instead of skipping them', () => {
    expect(config.matcher).toEqual([
      '/((?!api|_next/static|_next/image|favicon.ico|favicon.svg|manifest.webmanifest|sitemap.xml|robots.txt).*)',
    ]);
  });
});
