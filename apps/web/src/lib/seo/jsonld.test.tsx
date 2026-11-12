import { describe, expect, it } from 'vitest';

import { SITE_BASE_URL } from '@/lib/seo';
import {
  buildCourseJsonLd,
  buildCreativeWorkJsonLd,
  buildEventJsonLd,
  buildOrganizationJsonLd,
  buildProductJsonLd,
  buildProfilePageJsonLd,
  buildWebSiteJsonLd,
} from './jsonld';

describe('buildOrganizationJsonLd', () => {
  it('emits Organization with name, url, and optional logo', () => {
    const data = buildOrganizationJsonLd('https://gameguild.gg/favicon.svg');
    expect(data['@context']).toBe('https://schema.org');
    expect(data['@type']).toBe('Organization');
    expect(data.name).toBe('GameGuild');
    expect(data.url).toBe(SITE_BASE_URL);
    expect(data.logo).toEqual({ '@type': 'ImageObject', url: 'https://gameguild.gg/favicon.svg' });
  });

  it('omits logo when not provided', () => {
    expect(buildOrganizationJsonLd()).not.toHaveProperty('logo');
  });
});

describe('buildWebSiteJsonLd', () => {
  it('emits WebSite without SearchAction (no server-side site search)', () => {
    const data = buildWebSiteJsonLd();
    expect(data['@type']).toBe('WebSite');
    expect(data.name).toBe('GameGuild');
    expect(data.url).toBe(SITE_BASE_URL);
    expect(data).not.toHaveProperty('potentialAction');
  });
});

describe('buildCourseJsonLd', () => {
  it('emits Course with provider and online CourseInstance', () => {
    const data = buildCourseJsonLd({
      name: 'Unity Fundamentals',
      description: 'Learn Unity.',
      url: '/courses/unity-fundamentals',
    });
    expect(data['@type']).toBe('Course');
    expect(data.name).toBe('Unity Fundamentals');
    expect(data.url).toBe(`${SITE_BASE_URL}/courses/unity-fundamentals`);
    expect(data.provider).toEqual({ '@type': 'Organization', name: 'GameGuild', url: SITE_BASE_URL });
    expect(data.hasCourseInstance).toMatchObject({ '@type': 'CourseInstance', courseMode: 'online' });
    expect(data).not.toHaveProperty('offers');
  });

  it('includes offers only when pricing is exposed', () => {
    const data = buildCourseJsonLd({
      name: 'Paid Course',
      url: '/courses/paid',
      offer: { price: 49, priceCurrency: 'USD' },
    });
    expect(data.offers).toMatchObject({ '@type': 'Offer', price: 49, priceCurrency: 'USD' });
  });
});

describe('buildEventJsonLd', () => {
  it('emits online Event with VirtualLocation and organizer', () => {
    const data = buildEventJsonLd({
      name: 'Community Playtest',
      description: 'Play indie games.',
      url: '/testing-lab/events/evt-1',
      startDate: '2026-11-20T18:00:00Z',
      endDate: '2026-11-20T21:00:00Z',
    });
    expect(data['@type']).toBe('Event');
    expect(data.startDate).toBe('2026-11-20T18:00:00Z');
    expect(data.eventAttendanceMode).toBe('https://schema.org/OnlineEventAttendanceMode');
    expect(data.location).toEqual({ '@type': 'VirtualLocation', url: `${SITE_BASE_URL}/testing-lab/events/evt-1` });
    expect(data.organizer).toMatchObject({ '@type': 'Organization', name: 'GameGuild' });
  });

  it('uses OfflineEventAttendanceMode and drops VirtualLocation for InPerson events', () => {
    const data = buildEventJsonLd({ name: 'LAN Playtest', url: '/testing-lab/events/lan', mode: 'InPerson' });
    expect(data.eventAttendanceMode).toBe('https://schema.org/OfflineEventAttendanceMode');
    expect(data).not.toHaveProperty('location');
  });
});

describe('buildProductJsonLd', () => {
  it('emits Product with offers when pricing is available', () => {
    const data = buildProductJsonLd({
      name: 'Asset Pack',
      url: '/marketplace/prod-1',
      imageUrl: 'https://example.test/pack.png',
      offer: { price: 9.99, priceCurrency: 'USD' },
    });
    expect(data['@type']).toBe('Product');
    expect(data.image).toBe('https://example.test/pack.png');
    expect(data.offers).toMatchObject({ '@type': 'Offer', price: 9.99, priceCurrency: 'USD' });
  });

  it('omits offers when no price is exposed', () => {
    const data = buildProductJsonLd({ name: 'Free thing', url: '/marketplace/free' });
    expect(data).not.toHaveProperty('offers');
  });
});

describe('buildProfilePageJsonLd', () => {
  it('emits ProfilePage with mainEntity Person', () => {
    const data = buildProfilePageJsonLd('Ana', '/members/ana');
    expect(data['@type']).toBe('ProfilePage');
    expect(data.mainEntity).toEqual({
      '@type': 'Person',
      name: 'Ana',
      url: `${SITE_BASE_URL}/members/ana`,
    });
  });
});

describe('buildCreativeWorkJsonLd', () => {
  it('emits CreativeWork with author Person', () => {
    const data = buildCreativeWorkJsonLd({
      name: 'Roguelike Demo',
      description: 'A tiny roguelike.',
      url: '/members/ana/projects/roguelike-demo',
      authorName: 'Ana',
      authorUrl: '/members/ana',
    });
    expect(data['@type']).toBe('CreativeWork');
    expect(data.name).toBe('Roguelike Demo');
    expect(data.author).toEqual({
      '@type': 'Person',
      name: 'Ana',
      url: `${SITE_BASE_URL}/members/ana`,
    });
  });
});
