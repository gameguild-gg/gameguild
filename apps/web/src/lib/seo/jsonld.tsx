/**
 * Shared schema.org JSON-LD builders for rich-results eligibility (issue #1208).
 *
 * Pure functions returning schema.org objects; pages mount them via the
 * `<JsonLd>` server component. Canonical URL rule mirrors `lib/seo/index.ts`:
 * default locale `en-US` is externally unprefixed, so all absolute URLs are
 * built from SITE_BASE_URL without a locale prefix.
 */
import type { ReactNode } from 'react';

import { SITE_BASE_URL } from './index';

export const SITE_NAME = 'GameGuild';

function joinUrl(path: string): string {
  return `${SITE_BASE_URL.replace(/\/$/, '')}${path.startsWith('/') ? path : `/${path}`}`;
}

/** Shared GameGuild Organization node. */
export function gameGuildOrganization(logoUrl?: string): Record<string, unknown> {
  return {
    '@type': 'Organization',
    name: SITE_NAME,
    url: SITE_BASE_URL,
    ...(logoUrl ? { logo: { '@type': 'ImageObject', url: logoUrl } } : {}),
  };
}

/** `Organization` + optional `WebSite` for the root layout. */
export function buildOrganizationJsonLd(logoUrl?: string): Record<string, unknown> {
  return {
    '@context': 'https://schema.org',
    ...gameGuildOrganization(logoUrl),
  };
}

export function buildWebSiteJsonLd(): Record<string, unknown> {
  // No public server-side site search exists (course search is a client-side
  // catalog filter), so no SearchAction potentialAction is emitted.
  return {
    '@context': 'https://schema.org',
    '@type': 'WebSite',
    name: SITE_NAME,
    url: SITE_BASE_URL,
  };
}

export interface CourseJsonLdInput {
  name: string;
  description?: string | null;
  url: string;
  providerName?: string;
  /** Optional default offer (price + ISO 4217 currency) when course pricing is exposed. */
  offer?: { price: number; priceCurrency: string } | null;
  imageUrl?: string | null;
  /** Total estimated workload in hours; omitted (not invented) when unknown. */
  estimatedHours?: number | null;
}

/** `Course` for `/courses/[course]` and `/programs/[program]`. */
export function buildCourseJsonLd({
  name,
  description,
  url,
  providerName = SITE_NAME,
  offer,
  imageUrl,
  estimatedHours,
}: CourseJsonLdInput): Record<string, unknown> {
  return {
    '@context': 'https://schema.org',
    '@type': 'Course',
    name,
    ...(description?.trim() ? { description: description.trim() } : {}),
    url: joinUrl(url),
    ...(imageUrl ? { image: imageUrl } : {}),
    provider: { '@type': 'Organization', name: providerName, url: SITE_BASE_URL },
    hasCourseInstance: {
      '@type': 'CourseInstance',
      courseMode: 'online',
      // Unknown workload is omitted — never published as an invented P0D.
      ...(estimatedHours && estimatedHours > 0
        ? { courseWorkload: `PT${Math.round(estimatedHours)}H` }
        : {}),
    },
    ...(offer
      ? {
          offers: {
            '@type': 'Offer',
            price: offer.price,
            priceCurrency: offer.priceCurrency,
            category: offer.price === 0 ? 'Free' : 'Paid',
          },
        }
      : {}),
  };
}

export interface EventJsonLdInput {
  name: string;
  description?: string | null;
  url: string;
  startDate?: string | null;
  endDate?: string | null;
  /** Event mode from the data source; defaults to online. */
  mode?: string | null;
  organizerName?: string;
}

/** `Event` for `/testing-lab/events/[eventId]` (community playtests). */
export function buildEventJsonLd({
  name,
  description,
  url,
  startDate,
  endDate,
  mode,
  organizerName = SITE_NAME,
}: EventJsonLdInput): Record<string, unknown> {
  const inPerson = mode === 'InPerson';
  return {
    '@context': 'https://schema.org',
    '@type': 'Event',
    name,
    ...(description?.trim() ? { description: description.trim() } : {}),
    url: joinUrl(url),
    ...(startDate ? { startDate } : {}),
    ...(endDate ? { endDate } : {}),
    eventAttendanceMode: inPerson
      ? 'https://schema.org/OfflineEventAttendanceMode'
      : 'https://schema.org/OnlineEventAttendanceMode',
    ...(inPerson
      ? {}
      : {
          location: {
            '@type': 'VirtualLocation',
            url: joinUrl(url),
          },
        }),
    organizer: { '@type': 'Organization', name: organizerName, url: SITE_BASE_URL },
  };
}

export interface ProductJsonLdInput {
  name: string;
  description?: string | null;
  url: string;
  imageUrl?: string | null;
  /** Default pricing entry; omit when no price is exposed. */
  offer?: { price: number; priceCurrency?: string | null } | null;
}

/** `Product` for `/marketplace/[productId]`. */
export function buildProductJsonLd({
  name,
  description,
  url,
  imageUrl,
  offer,
}: ProductJsonLdInput): Record<string, unknown> {
  return {
    '@context': 'https://schema.org',
    '@type': 'Product',
    name,
    ...(description?.trim() ? { description: description.trim() } : {}),
    url: joinUrl(url),
    ...(imageUrl ? { image: imageUrl } : {}),
    ...(offer
      ? {
          offers: {
            '@type': 'Offer',
            price: offer.price,
            ...(offer.priceCurrency ? { priceCurrency: offer.priceCurrency } : {}),
            url: joinUrl(url),
          },
        }
      : {}),
  };
}

/** `ProfilePage` + `mainEntity` Person for `/members/[member]`. */
export function buildProfilePageJsonLd(name: string, url: string): Record<string, unknown> {
  return {
    '@context': 'https://schema.org',
    '@type': 'ProfilePage',
    mainEntity: {
      '@type': 'Person',
      name,
      url: joinUrl(url),
    },
  };
}

export interface CreativeWorkJsonLdInput {
  name: string;
  description?: string | null;
  url: string;
  authorName: string;
  authorUrl?: string;
  imageUrl?: string | null;
  externalUrl?: string | null;
}

/** `CreativeWork` for member portfolio projects (`/members/[member]/projects/[project]`). */
export function buildCreativeWorkJsonLd({
  name,
  description,
  url,
  authorName,
  authorUrl,
  imageUrl,
  externalUrl,
}: CreativeWorkJsonLdInput): Record<string, unknown> {
  return {
    '@context': 'https://schema.org',
    '@type': 'CreativeWork',
    name,
    ...(description?.trim() ? { description: description.trim() } : {}),
    url: joinUrl(url),
    ...(imageUrl ? { image: imageUrl } : {}),
    author: {
      '@type': 'Person',
      name: authorName,
      ...(authorUrl ? { url: joinUrl(authorUrl) } : {}),
    },
    ...(externalUrl ? { sameAs: externalUrl } : {}),
  };
}

/**
 * Renders a JSON-LD script tag. Mirrors the escaping rule from
 * `lib/blogs/seo.ts` (`resolveBlogJsonLd`): `<` becomes `\u003c` so the
 * payload can never terminate the script element.
 */
export function JsonLd({ data }: { data: Record<string, unknown> }): ReactNode {
  const payload = JSON.stringify(data).replace(/</g, '\\u003c');
  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: payload }}
    />
  );
}
