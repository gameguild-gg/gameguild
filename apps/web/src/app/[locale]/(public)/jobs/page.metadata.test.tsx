import { describe, expect, it } from 'vitest';

import { SITE_BASE_URL } from '@/lib/seo';

import { metadata } from './page';

describe('jobs listing metadata', () => {
  it('has a unique title and description', () => {
    expect(String(metadata.title)).not.toBe('GameGuild');
    expect(metadata.description).toBeTruthy();
  });

  it('declares an unprefixed absolute canonical and hreflang alternates', () => {
    expect(metadata.alternates?.canonical).toBe(`${SITE_BASE_URL}/jobs`);
    const languages = metadata.alternates?.languages as Record<string, string>;
    expect(languages['en-US']).toBe(`${SITE_BASE_URL}/jobs`);
    expect(languages['pt-BR']).toBe(`${SITE_BASE_URL}/pt-BR/jobs`);
    expect(languages['x-default']).toBe(`${SITE_BASE_URL}/jobs`);
  });
});
