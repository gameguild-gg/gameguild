import { describe, expect, it } from 'vitest';

import { SITE_BASE_URL, buildPageMetadata } from './index';

describe('buildPageMetadata', () => {
  it('builds an absolute unprefixed canonical URL', () => {
    const meta = buildPageMetadata({ path: '/courses', title: 'Courses — GameGuild', description: 'd' });
    expect(meta.alternates?.canonical).toBe(`${SITE_BASE_URL}/courses`);
  });

  it('maps hreflang languages: en-US = canonical, pt-BR = prefixed, x-default = canonical', () => {
    const meta = buildPageMetadata({ path: '/blogs', title: 'Blog — GameGuild', description: 'd' });
    const canonical = meta.alternates?.canonical as string;
    const languages = meta.alternates?.languages as Record<string, string>;
    expect(languages['en-US']).toBe(canonical);
    expect(languages['pt-BR']).toBe(`${SITE_BASE_URL}/pt-BR/blogs`);
    expect(languages['x-default']).toBe(canonical);
  });

  it('keeps root canonical clean for the home path', () => {
    const meta = buildPageMetadata({ path: '/', title: 'GameGuild', description: 'd' });
    expect(meta.alternates?.canonical).toBe(SITE_BASE_URL);
    const languages = meta.alternates?.languages as Record<string, string>;
    expect(languages['pt-BR']).toBe(`${SITE_BASE_URL}/pt-BR`);
    expect(languages['x-default']).toBe(SITE_BASE_URL);
  });

  it('emits a website openGraph card and twitter summary', () => {
    const meta = buildPageMetadata({ path: '/jobs', title: 'Jobs — GameGuild', description: 'desc' });
    const og = meta.openGraph as { type: string; siteName?: string; url?: string; images?: string[] };
    expect(og.type).toBe('website');
    expect(og.siteName).toBe('GameGuild');
    expect(og.url).toBe(`${SITE_BASE_URL}/jobs`);
    expect(og.images).toBeUndefined();
    expect((meta.twitter as { card: string }).card).toBe('summary');
  });

  it('includes og/twitter images when provided', () => {
    const meta = buildPageMetadata({ path: '/', title: 'GameGuild', description: 'd', ogImage: 'https://cdn.example.com/og.png' });
    expect(meta.openGraph?.images).toEqual(['https://cdn.example.com/og.png']);
    expect((meta.twitter as { card: string }).card).toBe('summary_large_image');
    expect(meta.twitter?.images).toEqual(['https://cdn.example.com/og.png']);
  });

  it('sets index/follow robots by default and noindex when requested', () => {
    expect(buildPageMetadata({ path: '/x', title: 't', description: 'd' }).robots).toEqual({ index: true, follow: true });
    expect(buildPageMetadata({ path: '/x', title: 't', description: 'd', noIndex: true }).robots).toEqual({ index: false, follow: false });
  });

  it('normalizes missing leading slash and trailing slash', () => {
    const meta = buildPageMetadata({ path: 'community/', title: 't', description: 'd' });
    expect(meta.alternates?.canonical).toBe(`${SITE_BASE_URL}/community`);
  });
});
