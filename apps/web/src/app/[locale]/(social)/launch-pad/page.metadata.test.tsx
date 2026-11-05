import { describe, expect, it } from 'vitest';

import { SITE_BASE_URL } from '@/lib/seo';

import { metadata } from './page';

describe('launch pad listing metadata', () => {
  it('has a unique title and description', () => {
    expect(String(metadata.title)).not.toBe('GameGuild');
    expect(metadata.description).toBeTruthy();
  });

  it('declares an unprefixed absolute canonical and hreflang alternates', () => {
    expect(metadata.alternates?.canonical).toBe(`${SITE_BASE_URL}/launch-pad`);
    const languages = metadata.alternates?.languages as Record<string, string>;
    expect(languages['en-US']).toBe(`${SITE_BASE_URL}/launch-pad`);
    expect(languages['pt-BR']).toBe(`${SITE_BASE_URL}/pt-BR/launch-pad`);
    expect(languages['x-default']).toBe(`${SITE_BASE_URL}/launch-pad`);
  });
});
