import { describe, expect, it } from 'vitest';
import type { CodingFunctionTest, CodingScalarType, CodingScalarValue } from './contracts';
import { composeCodingNativeFunctionSupport } from './native-function-support';

function example(type: CodingScalarType = 'integer', value: CodingScalarValue = { type: 'integer', value: 5 }): Pick<CodingFunctionTest, 'function' | 'cases'> {
  return {
    function: { name: 'identity', parameters: [{ name: 'value', type }], returnType: type },
    cases: [{ id: 'case-one', inputs: [value], expected: value }],
  };
}

describe('owned native function source composition', () => {
  it('puts C call/report/startup support outside the submitted workspace', () => {
    const source = example();
    const before = structuredClone(source);
    const file = composeCodingNativeFunctionSupport({ language: 'c', index: 3, test: source });
    expect(file.path).toBe('/tmp/gameguild-function-3.c');
    expect(file.content).toContain('int identity(int) __attribute__((weak));');
    expect(file.content).toContain('identity &&');
    expect(file.content).toContain('identity(5) == 5');
    expect(file.content).toContain('__attribute__((constructor))');
    expect(file.content).toContain('__attribute__((weak)) int main(void)');
    expect(file.content).toContain('[doctest] Status: %s!');
    expect(file.content).not.toContain('extern "C"');
    expect(source).toEqual(before);
  });

  it('retains the exact public C++ harness and supplies its ordinary-function bridge', () => {
    let selected: string | undefined;
    const harness = '// unchanged public harness\n';
    const file = composeCodingNativeFunctionSupport({ language: 'cpp', index: 4, test: example(),
      createCppHarness: symbol => { selected = symbol; return harness; } });
    expect(selected).toBe('gameguild_function_4');
    expect(file.path).toBe('/tmp/gameguild-function-4.cpp');
    expect(file.content.startsWith(harness)).toBe(true);
    expect(file.content).toContain('int identity(int) __attribute__((weak));');
    expect(file.content).toContain('extern "C" int gameguild_function_4(int gameguild_arg_0)');
    expect(file.content).toContain('if (!identity) __builtin_trap();');
    expect(file.content).toContain('return identity(gameguild_arg_0);');
    expect(file.content).toContain('__attribute__((weak)) int main(void)');
  });

  it.each([
    ['boolean', { type: 'boolean', value: false }, '_Bool identity(_Bool)', 'identity(0) == 0'],
    ['float', { type: 'float', value: '-2.5e1' }, 'double identity(double)', 'identity(-2.5e1) == -2.5e1'],
    ['string', { type: 'string', value: 'a"\\\n\r\t%s' }, 'const char* identity(const char*)', 'strcmp(gameguild_actual_0,'],
  ] as const)('defines native C %s values without changing the compiler language', (type, value, declaration, assertion) => {
    const file = composeCodingNativeFunctionSupport({ language: 'c', index: 0, test: example(type, value) });
    expect(file.content).toContain(declaration);
    expect(file.content).toContain(assertion);
    if (type === 'string') {
      expect(file.content).toContain('gameguild_actual_0 != 0');
      expect(file.content).toContain('\\042');
      expect(file.content).toContain('\\015');
      expect(file.content).not.toContain('%s\n');
    }
  });

  it('counts every case and reports missing targets as failures, including expected zero', () => {
    const source = example();
    source.cases.push({ id: 'case-two', inputs: [{ type: 'integer', value: 0 }], expected: { type: 'integer', value: 0 } });
    const { content } = composeCodingNativeFunctionSupport({ language: 'c', index: 0, test: source });
    expect(content).toContain('identity && identity(0) == 0');
    expect(content).toContain('const int gameguild_assertions = 2;');
    expect(content.match(/gameguild_failures \+=/g)).toHaveLength(2);
  });

  it('uses void for a C function with no parameters', () => {
    const source = example();
    source.function.parameters = [];
    source.cases[0]!.inputs = [];
    const { content } = composeCodingNativeFunctionSupport({ language: 'c', index: 0, test: source });
    expect(content).toContain('int identity(void) __attribute__((weak));');
    expect(content).toContain('identity() == 5');
  });

  it('creates separate source identities for distinct result groups', () => {
    const first = composeCodingNativeFunctionSupport({ language: 'c', index: 0, test: example() });
    const next = composeCodingNativeFunctionSupport({ language: 'c', index: 1, test: example() });
    expect(first.path).not.toBe(next.path);
  });

  it.each([-1, 0.5, NaN, Infinity, 100])('rejects invalid group index %s', index => {
    expect(() => composeCodingNativeFunctionSupport({ language: 'c', index, test: example() })).toThrow(/index/i);
  });

  it.each(['identity); injected(', 'main', 'printf', 'strcmp', 'gameguild_function_0'])('rejects unsafe or colliding target %s', name => {
    const source = example();
    source.function.name = name;
    expect(() => composeCodingNativeFunctionSupport({ language: 'c', index: 0, test: source })).toThrow(TypeError);
    expect(() => composeCodingNativeFunctionSupport({ language: 'c', index: 0, test: source })).toThrow(/function/i);
  });

  it.each([2_147_483_648, -2_147_483_649, NaN, 1.5])('rejects non-representable native integer %s without truncation', value => {
    expect(() => composeCodingNativeFunctionSupport({ language: 'c', index: 0, test: example('integer', { type: 'integer', value }) })).toThrow(/integer/i);
  });

  it.each(['1; injected()', 'NaN', 'Infinity', '1e999'])('rejects unsafe or non-finite floating literal %s', value => {
    expect(() => composeCodingNativeFunctionSupport({ language: 'c', index: 0, test: example('float', { type: 'float', value }) })).toThrow(/float/i);
  });

  it.each(['a\0b', '\ud800'])('rejects strings that native C strings cannot faithfully represent', value => {
    expect(() => composeCodingNativeFunctionSupport({ language: 'c', index: 0, test: example('string', { type: 'string', value }) })).toThrow(/string/i);
  });

  it('rejects C++ literals the unchanged public serializer cannot represent before calling it', () => {
    let calls = 0;
    expect(() => composeCodingNativeFunctionSupport({ language: 'cpp', index: 0,
      test: example('string', { type: 'string', value: 'a\rb' }),
      createCppHarness: () => { calls++; return ''; } })).toThrow(/string/i);
    expect(calls).toBe(0);
  });

  it.each(['empty', 'arity', 'input type', 'return type'] as const)('rejects malformed %s cases', kind => {
    const source = example();
    if (kind === 'empty') source.cases = [];
    if (kind === 'arity') source.cases[0]!.inputs = [];
    if (kind === 'input type') source.cases[0]!.inputs = [{ type: 'boolean', value: true }];
    if (kind === 'return type') source.cases[0]!.expected = { type: 'boolean', value: true };
    expect(() => composeCodingNativeFunctionSupport({ language: 'c', index: 0, test: source })).toThrow(/case|type/i);
  });
});
