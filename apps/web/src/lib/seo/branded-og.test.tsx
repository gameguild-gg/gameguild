import { describe, expect, it } from 'vitest';

import { brandedOgAlt, brandedOgImage, OG_IMAGE_CONTENT_TYPE, OG_IMAGE_SIZE } from './branded-og';

describe('branded OG image template', () => {
  it('exports the 1200x630 PNG contract', () => {
    expect(OG_IMAGE_SIZE).toEqual({ width: 1200, height: 630 });
    expect(OG_IMAGE_CONTENT_TYPE).toBe('image/png');
    expect(brandedOgAlt('Courses')).toBe('Courses — GameGuild');
  });

  it('renders an ImageResponse without throwing', () => {
    expect(() => brandedOgImage({ title: 'Courses', subtitle: 'Course catalog' })).not.toThrow();
  });
});
