import { describe, expect, it } from 'vitest';

import { formatJsDocLines } from '../../scripts/utils/jsdoc.js';

describe('formatJsDocLines', () => {
  it('prefixes each line, including blank lines, without trailing whitespace', () => {
    expect(formatJsDocLines('\n  First line\r\n  \r\n  - Item\r\n', '  ')).toEqual([
      '   * First line',
      '   *',
      '   * - Item',
    ]);
  });

  it('keeps comment delimiters inside the documentation', () => {
    expect(formatJsDocLines('Example */ value')).toEqual([' * Example * / value']);
  });
});
