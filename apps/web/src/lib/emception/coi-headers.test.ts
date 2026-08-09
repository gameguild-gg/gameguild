import { describe, expect, it } from 'vitest';
import { COI_LEARN_HEADERS, coiHeadersForPath } from './coi-headers';

describe('coiHeadersForPath', () => {
  it('applies credentialless + same-origin to deep learn paths with locale', () => {
    expect(coiHeadersForPath('/en/learn/courses/x/activities/y')).toEqual(
      COI_LEARN_HEADERS,
    );
  });

  it('applies to learn paths without a locale prefix', () => {
    expect(coiHeadersForPath('/learn/courses/x')).toEqual(COI_LEARN_HEADERS);
  });

  it('applies to the bare locale-less /learn root', () => {
    expect(coiHeadersForPath('/learn')).toEqual(COI_LEARN_HEADERS);
  });

  it('applies to the locale root /en/learn', () => {
    expect(coiHeadersForPath('/en/learn')).toEqual(COI_LEARN_HEADERS);
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
