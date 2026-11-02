import { describe, expect, it } from 'vitest';

import { SITE_BASE_URL } from '@/lib/seo';

import { metadata } from './page';

describe('courses listing metadata', () => {
  it('has a unique title and description', () => {
    expect(metadata.title).toContain('Courses');
    expect(metadata.title).not.toBe('GameGuild');
    expect(metadata.description).toBeTruthy();
    expect(metadata.description).not.toBe(metadata.title);
  });

  it('declares an unprefixed absolute canonical', () => {
    expect(metadata.alternates?.canonical).toBe(`${SITE_BASE_URL}/courses`);
  });

  it('declares hreflang en-US, pt-BR, and x-default', () => {
    const languages = metadata.alternates?.languages as Record<string, string>;
    expect(languages['en-US']).toBe(`${SITE_BASE_URL}/courses`);
    expect(languages['pt-BR']).toBe(`${SITE_BASE_URL}/pt-BR/courses`);
    expect(languages['x-default']).toBe(`${SITE_BASE_URL}/courses`);
  });
});
