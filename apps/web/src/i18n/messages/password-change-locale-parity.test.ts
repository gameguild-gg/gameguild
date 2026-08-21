import { describe, expect, it } from 'vitest';
import enUs from './en-US.json';
import ptBr from './pt-BR.json';

function keySet(node: unknown, prefix = ''): string[] {
  if (typeof node !== 'object' || node === null) return [prefix];
  return Object.entries(node).flatMap(([key, value]) =>
    keySet(value, prefix ? `${prefix}.${key}` : key),
  );
}

describe('passwordChange locale parity', () => {
  it('en-US and pt-BR expose the same key set under passwordChange', () => {
    const en = keySet(enUs.passwordChange).sort();
    const pt = keySet(ptBr.passwordChange).sort();

    expect(pt).toEqual(en);
    expect(en.length).toBeGreaterThan(0);
  });
});
