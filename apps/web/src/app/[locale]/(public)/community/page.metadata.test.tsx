import { describe, expect, it } from 'vitest';

import { SITE_BASE_URL } from '@/lib/seo';

import { metadata } from './page';

describe('community listing metadata', () => {
  it('has a unique title and description', () => {
    expect(String(metadata.title)).not.toBe('GameGuild');
    expect(metadata.description).toBeTruthy();
  });

  it('declares an unprefixed absolute canonical and hreflang alternates', () => {
    expect(metadata.alternates?.canonical).toBe(`${SITE_BASE_URL}/community`);
    const languages = metadata.alternates?.languages as Record<string, string>;
    expect(languages['en-US']).toBe(`${SITE_BASE_URL}/community`);
    expect(languages['pt-BR']).toBe(`${SITE_BASE_URL}/pt-BR/community`);
    expect(languages['x-default']).toBe(`${SITE_BASE_URL}/community`);
  });
});
