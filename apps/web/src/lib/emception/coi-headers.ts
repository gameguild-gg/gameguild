// COI (Cross-Origin Isolation) header rules for the learn routes, shared between
// next.config.ts headers() and the vitest unit test. emception's WASM workers need
// SharedArrayBuffer, which requires COEP: credentialless + COOP: same-origin on
// activity pages (not lesson pages, which embed YouTube video iframes). `credentialless`
// (not `require-corp`) keeps CDN imports working.
export interface Header {
  key: string;
  value: string;
}

export interface HeaderRule {
  source: string;
  headers: Header[];
}

export const COI_LEARN_HEADERS: Header[] = [
  { key: 'Cross-Origin-Embedder-Policy', value: 'credentialless' },
  { key: 'Cross-Origin-Opener-Policy', value: 'same-origin' },
];

export const COI_LEARN_RULES: HeaderRule[] = [
  {
    source: '/:locale/learn/courses/:slug/activities/:path*',
    headers: COI_LEARN_HEADERS,
  },
  {
    source: '/learn/courses/:slug/activities/:path*',
    headers: COI_LEARN_HEADERS,
  },
  {
    source: '/:locale/learn/activities/:path*',
    headers: COI_LEARN_HEADERS,
  },
  {
    source: '/learn/activities/:path*',
    headers: COI_LEARN_HEADERS,
  },
];

// Matches a pathname against a Next.js header source pattern. Supports the subset
// of path-to-regexp syntax used by COI_LEARN_RULES: literal segments, `:param`
// (exactly one segment) and trailing `:param*` (zero or more segments).
export function sourceMatches(source: string, pathname: string): boolean {
  const sourceSegments = source.split('/').filter(Boolean);
  const pathSegments = pathname.split('/').filter(Boolean);

  for (let i = 0; i < sourceSegments.length; i++) {
    const segment = sourceSegments[i];
    if (segment.startsWith(':')) {
      if (segment.endsWith('*')) {
        // `:param*` consumes the rest of the path, so it must be the last segment.
        return i === sourceSegments.length - 1;
      }
      // `:param` consumes exactly one segment; nothing to compare against.
      if (pathSegments.length <= i) return false;
      continue;
    }
    if (pathSegments[i] !== segment) return false;
  }
  return pathSegments.length === sourceSegments.length;
}

export function coiHeadersForPath(pathname: string): Header[] | undefined {
  return COI_LEARN_RULES.some((rule) => sourceMatches(rule.source, pathname))
    ? COI_LEARN_HEADERS
    : undefined;
}
