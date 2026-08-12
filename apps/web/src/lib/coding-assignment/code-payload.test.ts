import { describe, expect, it } from 'vitest';
import { codePayloadToFiles, filesToCodePayload } from './code-payload';

describe('filesToCodePayload', () => {
  it('serializes files to JSON object keyed by path with v1 {content, encoding} shape', () => {
    const files = [
      { path: 'main.cpp', content: '#include <iostream>' },
      { path: 'utils.h', content: '#pragma once' },
    ];
    const payload = filesToCodePayload(files);
    expect(payload).toBe(
      JSON.stringify({
        'main.cpp': { content: '#include <iostream>', encoding: 'text' },
        'utils.h': { content: '#pragma once', encoding: 'text' },
      }),
    );
  });

  it('returns "{}" for empty array', () => {
    expect(filesToCodePayload([])).toBe('{}');
  });

  it('preserves nested paths', () => {
    const files = [{ path: 'src/deep/nested/file.cpp', content: 'code' }];
    const payload = filesToCodePayload(files);
    expect(JSON.parse(payload)).toEqual({
      'src/deep/nested/file.cpp': { content: 'code', encoding: 'text' },
    });
  });

  it('preserves unicode and special characters exactly', () => {
    const content = '日本語コメント // "quotes" \\backslash\nnewline\ttab';
    const files = [{ path: 'unicode.cpp', content }];
    const payload = filesToCodePayload(files);
    const parsed = JSON.parse(payload) as Record<string, { content: string }>;
    expect(parsed['unicode.cpp'].content).toBe(content);
  });

  it('preserves paths with special characters', () => {
    const files = [{ path: 'src/with space/file (1).cpp', content: 'x' }];
    const payload = filesToCodePayload(files);
    const parsed = JSON.parse(payload) as Record<string, { content: string }>;
    expect(parsed['src/with space/file (1).cpp'].content).toBe('x');
  });
});

describe('codePayloadToFiles', () => {
  it('round-trips files through serialize → deserialize (deep equal)', () => {
    const files = [
      { path: 'main.cpp', content: '#include <iostream>\nint main() { return 0; }' },
      { path: 'test.cpp', content: 'TEST_CASE("basic") { REQUIRE(true); }' },
    ];
    expect(codePayloadToFiles(filesToCodePayload(files))).toEqual(files);
  });

  it('round-trips empty array → "{}" → []', () => {
    expect(codePayloadToFiles(filesToCodePayload([]))).toEqual([]);
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
    expect(restored).toEqual(files);
  });

  it('round-trips paths with special characters', () => {
    const files = [{ path: 'src/with space/file (1).cpp', content: 'x' }];
    expect(codePayloadToFiles(filesToCodePayload(files))).toEqual(files);
  });

  it('accepts legacy v0 shape Record<path, string>', () => {
    const legacy = JSON.stringify({ 'main.cpp': 'int main(){}' });
    expect(codePayloadToFiles(legacy)).toEqual([
      { path: 'main.cpp', content: 'int main(){}' },
    ]);
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
