/**
 * Shared SEO metadata builder for public pages.
 *
 * Canonical rule (mirrors `src/lib/blogs/seo.ts` and `src/i18n/routing.ts`):
 * default locale `en-US` is externally unprefixed, so canonical URLs NEVER
 * carry a locale prefix. Other locales are prefixed (`/pt-BR<path>`).
 */
import type { Metadata } from 'next';

export const SITE_BASE_URL = process.env.NEXT_PUBLIC_APP_URL ?? 'https://gameguild.gg';

const SECONDARY_LOCALE = 'pt-BR';

export interface PageMetadataInput {
  /** Unprefixed site path, e.g. `/courses` or `/courses/unity-fundamentals`. */
  path: string;
  title: string;
  description: string;
  ogImage?: string;
  noIndex?: boolean;
}

/** Normalizes an unprefixed path to exactly one leading slash and no trailing slash (except root). */
function normalizePath(path: string): string {
  const trimmed = path.trim();
  const withLeading = trimmed.startsWith('/') ? trimmed : `/${trimmed}`;
  return withLeading.length > 1 && withLeading.endsWith('/') ? withLeading.slice(0, -1) : withLeading;
}

function joinUrl(base: string, path: string): string {
  const normalized = normalizePath(path);
  return `${base.replace(/\/$/, '')}${normalized === '/' ? '' : normalized}`;
}

/**
 * Standard page metadata: unique title/description, absolute unprefixed
 * canonical, hreflang alternates (`en-US` = canonical, `pt-BR` = prefixed,
 * `x-default` = canonical), OpenGraph website card, and Twitter summary.
 */
export function buildPageMetadata({
  path,
  title,
  description,
  ogImage,
  noIndex,
}: PageMetadataInput): Metadata {
  const canonical = joinUrl(SITE_BASE_URL, path);
  const ptBr = joinUrl(SITE_BASE_URL, `/${SECONDARY_LOCALE}${normalizePath(path)}`);
  const images = ogImage ? { images: [ogImage] } : {};

  return {
    title,
    description,
    ...(noIndex ? { robots: { index: false, follow: false } } : { robots: { index: true, follow: true } }),
    alternates: {
      canonical,
      languages: {
        'en-US': canonical,
        'pt-BR': ptBr,
        'x-default': canonical,
      },
    },
    openGraph: {
      type: 'website',
      title,
      description,
      url: canonical,
      siteName: 'GameGuild',
      ...images,
    },
    twitter: {
      card: ogImage ? 'summary_large_image' : 'summary',
      title,
      description,
      ...images,
    },
  };
}
