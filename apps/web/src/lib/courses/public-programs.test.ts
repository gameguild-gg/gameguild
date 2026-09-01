import { describe, expect, it } from 'vitest';

import { PUBLIC_PROGRAM_PACKAGES, SHOWCASE_BY_SLUG } from './public-programs';

describe('public-programs catalog surface', () => {
  it('exports exactly the packages kept for the live course catalog', () => {
    expect(PUBLIC_PROGRAM_PACKAGES.map((program) => ({ slug: program.slug, courseSlugs: program.courseSlugs }))).toEqual([
      { slug: 'game-ai-systems', courseSlugs: ['ai4games'] },
      { slug: 'game-programming-foundations', courseSlugs: ['intro2gpro'] },
    ]);
  });

  it('exports exactly the showcases kept for the live course catalog', () => {
    expect(Object.keys(SHOWCASE_BY_SLUG).sort()).toEqual(['ai4games', 'intro2gpro']);
  });
});
