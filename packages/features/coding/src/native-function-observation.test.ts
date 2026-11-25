import { describe, expect, it } from 'vitest';
import { composeCodingNativeFunctionObservation } from './native-function-support';
import type { CodingNativeFunctionObservationRequest } from './native-function-support-types';

const recordId = 'a'.repeat(32);
function fixture(): CodingNativeFunctionObservationRequest {
  return {
    language: 'cpp',
    recordId,
    signature: {
      name: 'sum',
      parameters: [
        { name: 'left', type: 'integer' },
        { name: 'right', type: 'integer' },
      ],
      returnType: 'integer',
    },
    inputs: [
      { type: 'integer', value: 2 },
      { type: 'integer', value: 3 },
    ],
    outputLimitBytes: 4096,
  };
}

describe('native scalar observation through the supported console channel', () => {
  it.each(['c', 'cpp'] as const)('calls the %s target once without filesystem or fabricated oracle', (language) => {
    const request = { ...fixture(), language };
    const before = structuredClone(request);
    const file = composeCodingNativeFunctionObservation(request);
    expect(file.path).toBe(`/tmp/gameguild-practice-call.${language === 'cpp' ? 'cpp' : 'c'}`);
    expect(file.recordPrefix).toBe(`\u001eCGP:${recordId}:`);
    expect(file.content).toContain('int sum(int,int) __attribute__((weak));');
    expect(file.content).toContain('if (!sum) __builtin_trap();');
    expect(file.content.match(/sum\(2,3\)/g)).toHaveLength(1);
    expect(file.content).toContain('fprintf(stderr, "%d", gameguild_value)');
    expect(file.content).toContain('fputc(31, stderr)');
    expect(file.content).toContain('__attribute__((constructor))');
    expect(file.content).toContain('__attribute__((weak)) int main(void)');
    expect(file.content).not.toMatch(/doctest|CHECK|passed|expected|fopen|fwrite|readFile/);
    expect(file.content).not.toMatch(/\bprintf\(/);
    expect(request).toEqual(before);
  });

  it.each(['c', 'cpp'] as const)('encodes bounded %s string bytes without mixing raw control/format characters', (language) => {
    const file = composeCodingNativeFunctionObservation({
      language,
      recordId,
      signature: { name: 'echo', parameters: [{ name: 'text', type: 'string' }], returnType: 'string' },
      inputs: [{ type: 'string', value: 'a"%s\\\n' }],
      outputLimitBytes: 12,
    });
    expect(file.content).toContain(language === 'cpp' ? 'std::string echo(std::string)' : 'const char* echo(const char*)');
    expect(file.content).toContain('\\042');
    expect(file.content).toContain('"%02x"');
    expect(file.content).toContain('13');
    expect(file.content).not.toMatch(/fopen|strcmp|expected/);
  });

  it.each([
    ['boolean', { type: 'boolean', value: false }, 'flag(0)', '"%d"'],
    ['float', { type: 'float', value: '-2.5e1' }, 'flag(-2.5e1)', '"%.17g"'],
    ['integer', { type: 'integer', value: -2_147_483_648 }, 'flag((-2147483647 - 1))', '"%d"'],
  ] as const)('preserves the %s ABI in its report, without a correctness expression', (type, value, call, format) => {
    const file = composeCodingNativeFunctionObservation({
      language: 'c',
      recordId,
      signature: { name: 'flag', parameters: [{ name: 'arg', type }], returnType: type },
      inputs: [value],
      outputLimitBytes: 100,
    });
    expect(file.content).toContain(call);
    expect(file.content).toContain(format);
    expect(file.content).not.toContain(' == ');
  });

  it('supports a zero-argument function without requiring a learner main', () => {
    const file = composeCodingNativeFunctionObservation({ ...fixture(), signature: { name: 'answer', parameters: [], returnType: 'integer' }, inputs: [] });
    expect(file.content).toContain('int answer(void)');
    expect(file.content).toContain('answer()');
  });

  it.each(['name', 'arity', 'input type', 'integer range', 'non-finite', 'unsafe string', 'output limit', 'record identity'] as const)(
    'rejects invalid %s before native compilation',
    (defect) => {
      const request = fixture();
      if (defect === 'name') request.signature.name = 'bad();';
      const invalid =
        defect === 'arity'
          ? { ...request, inputs: [] }
          : defect === 'input type'
            ? { ...request, inputs: [{ type: 'boolean' as const, value: true }, request.inputs[1]!] }
            : defect === 'integer range'
              ? { ...request, inputs: [{ type: 'integer' as const, value: 2_147_483_648 }, request.inputs[1]!] }
              : defect === 'non-finite'
                ? {
                    ...request,
                    signature: { name: 'f', parameters: [{ name: 'x', type: 'float' as const }], returnType: 'float' as const },
                    inputs: [{ type: 'float' as const, value: '1e999' }],
                  }
                : defect === 'unsafe string'
                  ? {
                      ...request,
                      signature: { name: 'f', parameters: [{ name: 'x', type: 'string' as const }], returnType: 'string' as const },
                      inputs: [{ type: 'string' as const, value: 'a\u0000b' }],
                    }
                  : defect === 'output limit'
                    ? { ...request, outputLimitBytes: 0 }
                    : defect === 'record identity'
                      ? { ...request, recordId: 'bad);' }
                      : request;
      expect(() => composeCodingNativeFunctionObservation(invalid)).toThrow(TypeError);
    },
  );
});
