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
  /** Absolute title INCLUDING the brand — the root `%s | GameGuild` template is bypassed. */
  title: string;
  description: string;
  ogImage?: string;
  noIndex?: boolean;
  /** Active locale for localized canonical/OG URLs (defaults to the unprefixed default locale). */
  locale?: string;
  /** Force the large Twitter card even without an inline ogImage (routes using the opengraph-image.tsx file convention). */
  twitterLargeCard?: boolean;
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

function localizedPath(path: string, locale?: string): string {
  if (!locale || locale === 'en-US') return normalizePath(path);
  return `/${locale}${normalizePath(path)}`;
}

/**
 * Standard page metadata: unique title/description, absolute canonical for
 * the ACTIVE locale (default-locale URLs unprefixed, others prefixed),
 * hreflang alternates (`x-default` always points at the default-locale
 * URL), OpenGraph website card, and Twitter summary.
 */
export function buildPageMetadata({
  path,
  title,
  description,
  ogImage,
  noIndex,
  locale,
  twitterLargeCard,
}: PageMetadataInput): Metadata {
  const canonical = joinUrl(SITE_BASE_URL, localizedPath(path, locale));
  const enUs = joinUrl(SITE_BASE_URL, normalizePath(path));
  const ptBr = joinUrl(SITE_BASE_URL, `/${SECONDARY_LOCALE}${normalizePath(path)}`);
  const images = ogImage ? { images: [ogImage] } : {};
  const largeCard = Boolean(ogImage) || twitterLargeCard === true;

  return {
    // Absolute title: the root layout template (`%s | GameGuild`) would
    // otherwise append a second brand suffix to already-branded titles.
    title: { absolute: title },
    description,
    ...(noIndex ? { robots: { index: false, follow: false } } : { robots: { index: true, follow: true } }),
    alternates: {
      canonical,
      languages: {
        'en-US': enUs,
        'pt-BR': ptBr,
        'x-default': enUs,
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
      card: largeCard ? 'summary_large_image' : 'summary',
      title,
      description,
      ...images,
    },
  };
}
