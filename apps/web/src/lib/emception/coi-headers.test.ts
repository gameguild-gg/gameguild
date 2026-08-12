import { describe, expect, it } from 'vitest';
import { COI_LEARN_HEADERS, coiHeadersForPath } from './coi-headers';

describe('coiHeadersForPath', () => {
  it('applies credentialless + same-origin to deep learn paths with locale', () => {
    expect(coiHeadersForPath('/en/learn/courses/x/activities/y')).toEqual(
      COI_LEARN_HEADERS,
    );
  });

  it('applies to learn activity paths without a locale prefix', () => {
    expect(coiHeadersForPath('/learn/courses/x/activities/y')).toEqual(
      COI_LEARN_HEADERS,
    );
  });

  it('does not apply to lesson paths (allowing YouTube embeds without COOP/COEP restrictions)', () => {
    expect(
      coiHeadersForPath('/learn/courses/intro2gpro/lessons/lesson-1'),
    ).toBeUndefined();
    expect(
      coiHeadersForPath('/en/learn/courses/intro2gpro/lessons/lesson-1'),
    ).toBeUndefined();
  });

  it('does not apply to general learn overview pages', () => {
    expect(coiHeadersForPath('/learn')).toBeUndefined();
    expect(coiHeadersForPath('/en/learn')).toBeUndefined();
    expect(coiHeadersForPath('/learn/courses')).toBeUndefined();
    expect(coiHeadersForPath('/en/learn/courses/intro2gpro')).toBeUndefined();
  });

  it('does not apply to /sign-in', () => {
    expect(coiHeadersForPath('/sign-in')).toBeUndefined();
  });

  it('does not apply to a locale sign-in', () => {
    expect(coiHeadersForPath('/en/sign-in')).toBeUndefined();
  });

  it('does not false-positive on /learn-* siblings', () => {
    expect(coiHeadersForPath('/learn-archive/x')).toBeUndefined();
    expect(coiHeadersForPath('/en/learn-more')).toBeUndefined();
  });
});
