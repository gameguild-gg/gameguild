import { describe, expect, it, vi } from 'vitest';

const resolveLegacyBlogSlug = vi.hoisted(() => vi.fn());

vi.mock('@/lib/blogs/legacy-redirect', () => ({
  resolveLegacyBlogSlug: (...args: unknown[]) => resolveLegacyBlogSlug(...args),
  legacyRedirect: (target: string) =>
    new Response(null, { status: 308, headers: { Location: target, 'X-Robots-Tag': 'noindex' } }),
  legacyNotFound: () => new Response('Not Found', { status: 404, headers: { 'X-Robots-Tag': 'noindex' } }),
}));

const { GET } = await import('./route');

describe('legacy /blog/[slug] redirect route handler', () => {
  it('308-redirects a resolved legacy slug to the canonical internal route', async () => {
    resolveLegacyBlogSlug.mockResolvedValue({ handle: 'alice', slug: 'hello-ghost' });

    const response = await GET(new Request('https://gameguild.gg/blog/hello-ghost'), {
      params: Promise.resolve({ slug: 'hello-ghost' }),
    });

    expect(response.status).toBe(308);
    expect(response.headers.get('Location')).toBe('/blogs/alice/hello-ghost');
    expect(response.headers.get('X-Robots-Tag')).toBe('noindex');
  });

  it('404s when no unique published post carries the slug', async () => {
    resolveLegacyBlogSlug.mockResolvedValue(null);

    const response = await GET(new Request('https://gameguild.gg/blog/missing'), {
      params: Promise.resolve({ slug: 'missing' }),
    });

    expect(response.status).toBe(404);
    expect(response.headers.get('X-Robots-Tag')).toBe('noindex');
  });

  it('404s on an incomplete resolution instead of redirecting to a partial route', async () => {
    resolveLegacyBlogSlug.mockResolvedValue({ handle: null, slug: 'x' });

    const response = await GET(new Request('https://gameguild.gg/blog/x'), {
      params: Promise.resolve({ slug: 'x' }),
    });

    expect(response.status).toBe(404);
  });
});
