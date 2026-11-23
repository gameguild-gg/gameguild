import { MAX_CODING_TESTS, type CodingScalarType, type CodingScalarValue } from './contracts';
import type { CodingNativeFunctionSupportComposer } from './native-function-support-types';

export const composeCodingNativeFunctionSupport: CodingNativeFunctionSupportComposer = request => {
  const { index, test, language } = request;
  if (!Number.isSafeInteger(index) || index < 0 || index >= MAX_CODING_TESTS) {
    throw new TypeError('Invalid native function group index.');
  }
  if (language !== 'c' && language !== 'cpp') throw new TypeError('Unsupported native function language.');
  const name = test.function.name;
  if (!/^[A-Za-z_][A-Za-z0-9_]{0,127}$/.test(name) || name.startsWith('gameguild_')
    || name === 'main' || name === 'printf' || name === 'strcmp') {
    throw new TypeError('Invalid or reserved native function name.');
  }
  if (test.cases.length === 0) throw new TypeError('Native function needs at least one case.');
  for (const item of test.cases) {
    if (item.inputs.length !== test.function.parameters.length) throw new TypeError('Native function case arity mismatch.');
    item.inputs.forEach((value, position) => {
      if (value.type !== test.function.parameters[position]?.type) throw new TypeError('Native function input type mismatch.');
      validateValue(value, language);
    });
    if (item.expected.type !== test.function.returnType) throw new TypeError('Native function return type mismatch.');
    validateValue(item.expected, language);
  }
  const parameters = test.function.parameters.map(parameter => nativeType(parameter.type, language));
  const returns = nativeType(test.function.returnType, language);
  const declaration = `${returns} ${name}(${parameters.join(',') || 'void'}) __attribute__((weak));`;
  const startup = '__attribute__((weak)) int main(void){return 0;}';
  const path = `/tmp/gameguild-function-${index}.${language === 'c' ? 'c' : 'cpp'}`;

  if (request.language === 'cpp') {
    const bridge = `gameguild_function_${index}`;
    const harness = request.createCppHarness(bridge);
    if (typeof harness !== 'string' || harness.trim().length === 0) throw new TypeError('Missing public C++ function harness.');
    const args = parameters.map((type, position) => `${type} gameguild_arg_${position}`).join(',');
    const values = parameters.map((_type, position) => `gameguild_arg_${position}`).join(',');
    return { path, content: [harness, declaration,
      `extern "C" ${returns} ${bridge}(${args || 'void'}) {`,
      `  if (!${name}) __builtin_trap();`, `  return ${name}(${values});`, '}', startup, '',
    ].join('\n') };
  }

  const checks = test.cases.flatMap((item, position) => {
    const call = `${name}(${item.inputs.map(cLiteral).join(',')})`;
    const expected = cLiteral(item.expected);
    const actual = `gameguild_actual_${position}`;
    const passed = `gameguild_passed_${position}`;
    const strings = item.expected.type === 'string';
    return [
      ...(strings ? [`  const char* ${actual} = ${name} ? ${call} : 0;`] : []),
      `  const int ${passed} = ${strings ? `${actual} != 0 && strcmp(${actual}, ${expected}) == 0` : `${name} && ${call} == ${expected}`};`,
      `  gameguild_failures += !${passed};`,
      `  if (!${passed}) printf("[doctest] CHECK failed: case ${position}\\n");`,
    ];
  });
  return { path, content: [
    'extern int printf(const char*, ...);', 'extern int strcmp(const char*, const char*);', declaration,
    'static void gameguild_checks(void) __attribute__((constructor));',
    'static void gameguild_checks(void) {',
    `  const int gameguild_assertions = ${test.cases.length};`,
    '  int gameguild_failures = 0;', ...checks,
    '  printf("[doctest] test cases: 1 | %d passed | %d failed | 0 skipped\\n", gameguild_failures == 0, gameguild_failures != 0);',
    '  printf("[doctest] assertions: %d | %d passed | %d failed |\\n", gameguild_assertions, gameguild_assertions - gameguild_failures, gameguild_failures);',
    '  printf("[doctest] Status: %s!\\n", gameguild_failures == 0 ? "SUCCESS" : "FAILURE");',
    '}', startup, '',
  ].join('\n') };
};

function nativeType(type: CodingScalarType, language: 'c' | 'cpp'): string {
  switch (type) {
    case 'integer': return 'int';
    case 'float': return 'double';
    case 'boolean': return language === 'c' ? '_Bool' : 'bool';
    case 'string': return language === 'c' ? 'const char*' : 'std::string';
    default: throw new TypeError('Unsupported native function scalar type.');
  }
}

function validateValue(value: CodingScalarValue, language: 'c' | 'cpp'): void {
  switch (value.type) {
    case 'integer':
      if (!Number.isInteger(value.value) || value.value < -2_147_483_648 || value.value > 2_147_483_647) {
        throw new TypeError('Integer is not representable by the native int ABI.');
      }
      return;
    case 'float':
      if (typeof value.value !== 'string' || !/^-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?$/.test(value.value)
        || !Number.isFinite(Number(value.value))) throw new TypeError('Invalid native floating-point literal.');
      return;
    case 'boolean':
      if (typeof value.value !== 'boolean') throw new TypeError('Invalid native boolean literal.');
      return;
    case 'string':
      if (typeof value.value !== 'string' || value.value.includes('\0') || /[\uD800-\uDFFF]/u.test(value.value)
        || (language === 'cpp' && [...value.value].some(character => {
          const code = character.charCodeAt(0);
          return (code < 32 && code !== 9 && code !== 10) || code === 127;
        }))) {
        throw new TypeError('String cannot be faithfully represented by the native function harness.');
      }
      return;
    default: throw new TypeError('Unsupported native function scalar type.');
  }
}

function cLiteral(value: CodingScalarValue): string {
  switch (value.type) {
    case 'integer': return value.value === -2_147_483_648 ? '(-2147483647 - 1)' : String(value.value);
    case 'float': return value.value;
    case 'boolean': return value.value ? '1' : '0';
    // Fixed-width octal UTF-8 escapes cannot swallow following digits or inject
    // source/format directives. A C string cannot represent embedded NUL.
    case 'string': return `"${Array.from(new TextEncoder().encode(value.value), byte => `\\${byte.toString(8).padStart(3, '0')}`).join('')}"`;
  }
}
