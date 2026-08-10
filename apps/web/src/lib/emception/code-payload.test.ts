import { describe, expect, it } from 'vitest';
import { filesToCodePayload, codePayloadToFiles } from './code-payload';

describe('filesToCodePayload', () => {
  it('serializes files to JSON object with path keys', () => {
    const files = [
      { path: 'main.cpp', content: '#include <iostream>' },
      { path: 'utils.h', content: '#pragma once' },
    ];
    const payload = filesToCodePayload(files);
    expect(payload).toBe(
      JSON.stringify({ 'main.cpp': '#include <iostream>', 'utils.h': '#pragma once' }),
    );
  });

  it('returns "{}" for empty array', () => {
    expect(filesToCodePayload([])).toBe('{}');
  });

  it('preserves nested paths', () => {
    const files = [{ path: 'src/deep/nested/file.cpp', content: 'code' }];
    const payload = filesToCodePayload(files);
    expect(JSON.parse(payload)).toEqual({ 'src/deep/nested/file.cpp': 'code' });
  });

  it('preserves unicode and special characters exactly', () => {
    const content = '日本語コメント // "quotes" \\backslash\nnewline\ttab';
    const files = [{ path: 'unicode.cpp', content }];
    const payload = filesToCodePayload(files);
    const parsed = JSON.parse(payload);
    expect(parsed['unicode.cpp']).toBe(content);
  });
});

describe('codePayloadToFiles', () => {
  it('round-trips files through serialize → deserialize', () => {
    const files = [
      { path: 'main.cpp', content: '#include <iostream>\nint main() { return 0; }' },
      { path: 'test.cpp', content: 'TEST_CASE("basic") { REQUIRE(true); }' },
    ];
    const payload = filesToCodePayload(files);
    const restored = codePayloadToFiles(payload);
    expect(restored).toEqual(files);
  });

  it('round-trips empty array → "{}" → []', () => {
    const payload = filesToCodePayload([]);
    expect(codePayloadToFiles(payload)).toEqual([]);
  });

  it('round-trips nested paths', () => {
    const files = [
      { path: 'src/main.cpp', content: 'a' },
      { path: 'src/lib/utils.cpp', content: 'b' },
      { path: 'include/header.h', content: 'c' },
    ];
    expect(codePayloadToFiles(filesToCodePayload(files))).toEqual(files);
  });

  it('round-trips unicode and special characters losslessly', () => {
    const content = '日本語コメント // "quotes" \\backslash\nnewline\ttab';
    const files = [{ path: 'unicode.cpp', content }];
    const restored = codePayloadToFiles(filesToCodePayload(files));
    expect(restored[0].content).toBe(content);
    expect(restored[0].path).toBe('unicode.cpp');
  });

  it('throws on non-JSON input', () => {
    expect(() => codePayloadToFiles('not json at all')).toThrow('Invalid code payload');
  });

  it('throws when JSON is not a plain object (array)', () => {
    expect(() => codePayloadToFiles('[1,2,3]')).toThrow('Invalid code payload');
  });

  it('throws when JSON is not a plain object (string)', () => {
    expect(() => codePayloadToFiles('"just a string"')).toThrow('Invalid code payload');
  });
});
