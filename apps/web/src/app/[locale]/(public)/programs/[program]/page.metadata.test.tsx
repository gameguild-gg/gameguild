import { describe, expect, it } from 'vitest';

import { SITE_BASE_URL } from '@/lib/seo';
import { PUBLIC_PROGRAM_PACKAGES } from '@/lib/courses/public-programs';

import { generateMetadata } from './page';

describe('program detail metadata', () => {
  it('has unique title, canonical, and hreflang alternates', async () => {
    const program = PUBLIC_PROGRAM_PACKAGES[0];
    expect(program).toBeDefined();
    const slug = typeof program.slug === 'string' ? program.slug : String(program.slug);
    const metadata = await generateMetadata({ params: Promise.resolve({ program: slug }) });

    expect((metadata.title as { absolute: string }).absolute).toContain('— GameGuild');
    expect(String(metadata.title)).not.toBe('GameGuild');
    expect(metadata.alternates?.canonical).toBe(`${SITE_BASE_URL}/programs/${slug}`);
    const languages = metadata.alternates?.languages as Record<string, string>;
    expect(languages['en-US']).toBe(`${SITE_BASE_URL}/programs/${slug}`);
    expect(languages['pt-BR']).toBe(`${SITE_BASE_URL}/pt-BR/programs/${slug}`);
    expect(languages['x-default']).toBe(`${SITE_BASE_URL}/programs/${slug}`);
  });
});
