import type { TestCase, TestResult, TestRunner } from '../types';
import { runHppInoutTests } from './inout';
import { runHppPredicateTests } from './predicate';

/**
 * Run C++ header tests
 */
export async function runHppTests(code: string, tests: TestCase[], testType: string, executeCpp: (code: string) => Promise<string>): Promise<TestResult[]> {
  switch (testType) {
    case 'inout':
      return runHppInoutTests(code, tests, executeCpp);
    case 'predicate':
      return runHppPredicateTests(code, tests, executeCpp);
    default:
      throw new Error(`Unsupported test type: ${testType}`);
  }
}

/**
 * C++ header test runners
 */
export const hppTestRunners: Record<string, TestRunner> = {
  inout: async (code: string, tests: TestCase[], options: any) => {
    return runHppInoutTests(code, tests, options.executeCpp);
  },
  predicate: async (code: string, tests: TestCase[], options: any) => {
    return runHppPredicateTests(code, tests, options.executeCpp);
  },
};
