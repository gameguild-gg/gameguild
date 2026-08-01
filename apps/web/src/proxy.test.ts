import { describe, expect, it, vi } from 'vitest';
import { NextRequest } from 'next/server';

vi.mock('@/i18n', () => ({
  routing: {
    defaultLocale: 'en-US',
    locales: ['en-US', 'pt-BR'],
  },
}));

vi.mock('@/lib/tracks/catalog', () => ({
  getTrackProgramHref: (slug: string) => `/programs/${slug}`,
}));

vi.mock('@/lib/server/request-logging', () => ({
  elapsedMs: () => 1,
  getRequestId: () => 'request-id',
  logWebRequest: vi.fn(),
}));

vi.stubEnv('UNIFIED_LEARNING_ENABLED', 'true');

const { config, proxy } = await import('./proxy');

describe('GameGuild proxy matcher', () => {
  it('runs for dashboard RSC prefetch requests instead of skipping them', () => {
    expect(config.matcher).toEqual([
      '/((?!api|_next/static|_next/image|favicon.ico|favicon.svg|manifest.webmanifest|sitemap.xml|robots.txt).*)',
    ]);
  });

  it('rewrites clean learner-host URLs into the internal learn route', async () => {
    const request = new NextRequest(
      'https://learning.gameguild.gg/courses/game-ai/content?module=2',
      {
        headers: {
          'x-forwarded-host': 'learning.gameguild.gg',
        },
      },
    );

    const response = await proxy(request);

    expect(response.headers.get('x-middleware-rewrite')).toBe(
      'https://learning.gameguild.gg/en-US/learn/courses/game-ai/content?module=2',
    );
    expect(response.headers.get('x-request-id')).toBe('request-id');
  });

  it('redirects internal learner routes away from the website host', async () => {
    const request = new NextRequest(
      'https://gameguild.gg/pt-BR/learn/courses/game-ai?tab=progress',
      {
        headers: {
          'x-forwarded-host': 'gameguild.gg',
        },
      },
    );

    const response = await proxy(request);

    expect(response.status).toBe(308);
    expect(response.headers.get('location')).toBe(
      'https://learning.gameguild.gg/pt-BR/courses/game-ai?tab=progress',
    );
  });
});
