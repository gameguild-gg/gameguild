import type { ProgrammingLanguage } from '../../components/ui/source-code/types';
import type { TestCase, TestResult, TestRunner } from './types';
import { javascriptTestRunners } from './javascript';
import { pythonTestRunners } from './python';
import { typescriptTestRunners } from './typescript';
import { luaTestRunners } from './lua';
import { cTestRunners } from './c';
import { cppTestRunners } from './cpp';
import { hTestRunners } from './h';
import { hppTestRunners } from './hpp';
import { runCustomTests } from './custom';

// Map of all test runners by language and test type
const testRunners: Record<string, Record<string, TestRunner>> = {
  javascript: { ...javascriptTestRunners, custom: runCustomTests, function: runCustomTests },
  typescript: { ...typescriptTestRunners, custom: runCustomTests, function: runCustomTests },
  python: { ...pythonTestRunners, custom: runCustomTests, function: runCustomTests },
  lua: { ...luaTestRunners, custom: runCustomTests, function: runCustomTests },
  c: { ...cTestRunners, custom: runCustomTests, function: runCustomTests },
  cpp: { ...cppTestRunners, custom: runCustomTests, function: runCustomTests },
  h: { ...hTestRunners, custom: runCustomTests, function: runCustomTests },
  hpp: { ...hppTestRunners, custom: runCustomTests, function: runCustomTests },
};

/**
 * Get the appropriate test runner for a language and test type
 */
export function getTestRunner(language: ProgrammingLanguage, testType: string): TestRunner | undefined {
  return testRunners[language]?.[testType];
}

// Export types explicitly
export type { TestRunner, TestCase, TestResult, TestType };
export type TestType = 'simple' | 'inout' | 'predicate' | 'custom' | 'function';
